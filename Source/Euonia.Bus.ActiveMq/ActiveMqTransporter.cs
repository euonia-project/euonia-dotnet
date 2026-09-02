using Apache.NMS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 基于 ActiveMQ 的 <see cref="ITransporter"/> 实现。
/// </summary>
internal class ActiveMqTransporter : ITransporter
{
	/// <summary>
	/// 当消息成功投递到 ActiveMQ 时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly ActiveMqBusOptions _options;
	private readonly IPersistentConnection _connection;
	private readonly ILogger<ActiveMqTransporter> _logger;
	private readonly IMessageSerializer _serializer;

	/// <summary>
	/// 获取传输器名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="ActiveMqTransporter"/> 的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供者。</param>
	/// <param name="connection">用于与 ActiveMQ 建立持久连接的连接实例。</param>
	/// <param name="options">包含 ActiveMQ 总线配置的选项。</param>
	/// <param name="logger">用于记录日志的日志工厂。</param>
	public ActiveMqTransporter(IServiceProvider provider, IPersistentConnection connection, IOptions<ActiveMqBusOptions> options, ILoggerFactory logger)
	{
		_serializer = provider.GetKeyedService<IMessageSerializer>(options.Value.SerializerProvider);
		_logger = logger.CreateLogger<ActiveMqTransporter>();
		_options = options.Value;
		_connection = connection;
		Name = _options.Name ?? Constants.DefaultTransportName;
	}

	public async Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		using var session = await _connection.CreateSessionAsync();
		var destination = await session.GetTopicAsync(message.Channel);
		using var producer = await session.CreateProducerAsync(destination);
		producer.DeliveryMode = MsgDeliveryMode.Persistent;
		producer.RequestTimeout = TimeSpan.FromSeconds(30);
		var request = await BuildRequestAsync(session, message);

		await Policy.Handle<Exception>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            }).ExecuteAsync(async () =>
		            {
			            await producer.SendAsync(request, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
		            });
	}

	/// <summary>
	/// 发送消息并等待回复。
	/// </summary>
	/// <typeparam name="TMessage">表示发送的消息类型。</typeparam>
	/// <typeparam name="TResponse">表示回复的消息类型。</typeparam>
	/// <param name="message">表示要发送的消息封装。</param>
	/// <param name="cancellationToken">表示取消操作的令牌。</param>
	/// <returns>表示异步操作的任务，任务结果为回复的消息。</returns>
	public async Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		var task = new TaskCompletionSource<TResponse>();

		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => task.TrySetCanceled());
		}

		using var session = await _connection.CreateSessionAsync();

		// 1. 创建用于接收回复的临时队列（生命周期由当前 Connection 管理）
		var replyQueue = await session.CreateTemporaryQueueAsync();

		// 2. 创建一个消费者，专门用来监听这个临时队列（等待消费回复消息）
		var replyConsumer = await session.CreateConsumerAsync(replyQueue);
		replyConsumer.Listener += OnReceived;

		var destination = await session.GetQueueAsync(message.Channel);
		using var producer = await session.CreateProducerAsync(destination);
		producer.DeliveryMode = MsgDeliveryMode.Persistent;
		producer.RequestTimeout = TimeSpan.FromSeconds(30);
		var request = await BuildRequestAsync(session, message);

		await Policy.Handle<Exception>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            }).ExecuteAsync(async () =>
		            {
			            await producer.SendAsync(request, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
		            });

		var result = await task.Task;
		replyConsumer.Listener -= OnReceived;
		return result;

		void OnReceived(IMessage replyMessage)
		{
			if (replyMessage is not ITextMessage reply)
			{
				task.SetException(new InvalidOperationException("Received message is not a text message."));
				return;
			}

			if (typeof(TResponse).IsIn(typeof(Unit), typeof(Task), typeof(ValueTask), typeof(void)))
			{
				var response = _serializer.Deserialize<ActiveMqReply<object>>(reply.Text);
				if (response.IsSuccess)
				{
					task.SetResult(default);
				}
				else
				{
					task.SetException(response.Error);
				}
			}
			else
			{
				var response = _serializer.Deserialize<ActiveMqReply<TResponse>>(reply.Text);
				if (response.IsSuccess)
				{
					task.SetResult(response.Result);
				}
				else
				{
					task.SetException(response.Error);
				}
			}
		}
	}

	/// <summary>
	/// 发送消息并等待回复。
	/// </summary>
	/// <typeparam name="TRequest">表示发送的消息类型。</typeparam>
	/// <typeparam name="TResponse">表示回复的消息类型。</typeparam>
	/// <param name="message">表示要发送的消息封装。</param>
	/// <param name="cancellationToken">表示取消操作的令牌。</param>
	/// <returns>表示异步操作的任务，任务结果为回复的消息。</returns>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		var task = new TaskCompletionSource<TResponse>();

		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => task.TrySetCanceled());
		}

		using var session = await _connection.CreateSessionAsync();

		// 1. 创建用于接收回复的临时队列（生命周期由当前 Connection 管理）
		var replyQueue = await session.CreateTemporaryQueueAsync();

		// 2. 创建一个消费者，专门用来监听这个临时队列（等待消费回复消息）
		var replyConsumer = await session.CreateConsumerAsync(replyQueue);
		replyConsumer.Listener += OnReceived;

		var destination = await session.GetQueueAsync(message.Channel);
		using var producer = await session.CreateProducerAsync(destination);
		producer.DeliveryMode = MsgDeliveryMode.Persistent;
		producer.RequestTimeout = TimeSpan.FromSeconds(30);
		var request = await BuildRequestAsync(session, message);

		await Policy.Handle<Exception>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            }).ExecuteAsync(async () =>
		            {
			            await producer.SendAsync(request, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.MaxValue);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
		            });

		var result = await task.Task;
		replyConsumer.Listener -= OnReceived;
		return result;

		void OnReceived(IMessage replyMessage)
		{
			if (replyMessage is not ITextMessage reply)
			{
				task.SetException(new InvalidOperationException("Received message is not a text message."));
				return;
			}

			var response = _serializer.Deserialize<ActiveMqReply<TResponse>>(reply.Text);
			if (response.IsSuccess)
			{
				task.SetResult(response.Result);
			}
			else
			{
				task.SetException(response.Error);
			}
		}
	}

	private async Task<ITextMessage> BuildRequestAsync<TMessage>(ISession session, IMessageEnvelope<TMessage> message, IDestination replyTo = null)
	{
		var messageBody = _serializer.Serialize(message);
		var request = await session.CreateTextMessageAsync(messageBody);
		request.NMSCorrelationID = message.CorrelationId;
		request.NMSReplyTo = replyTo;
		request.Properties[MessageHeaders.MessageId] = message.MessageId;
		request.Properties[MessageHeaders.ConversationId] = message.ConversationId;
		request.Properties[MessageHeaders.RequestTraceId] = message.RequestTraceId;
		request.Properties[MessageHeaders.Authorization] = message.Authorization;
		request.Properties[MessageHeaders.Channel] = message.Channel;
		request.Properties[MessageHeaders.UserId] = message.User.Identity?.Name;
		request.Properties[MessageHeaders.MessageType] = message.TypeName;
		return request;
	}
}