using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 基于 RabbitMQ 的 <see cref="ITransporter"/> 实现。
/// </summary>
internal class RabbitMqTransporter : ITransporter
{
	/// <summary>
	/// 当消息成功投递到 RabbitMQ 时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly RabbitMqBusOptions _options;
	private readonly IPersistentConnection _connection;
	private readonly ILogger<RabbitMqTransporter> _logger;
	private readonly IMessageSerializer _serializer;

	/// <summary>
	/// 获取传输器名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="RabbitMqTransporter"/> 的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供者。</param>
	/// <param name="connection">用于与 RabbitMQ 建立和管理持久连接的工厂。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 RabbitMQ 总线配置选项。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public RabbitMqTransporter(IServiceProvider provider, IPersistentConnection connection, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
	{
		_serializer = provider.GetKeyedService<IMessageSerializer>(options.Value.SerializerProvider);
		_logger = logger.CreateLogger<RabbitMqTransporter>();
		_connection = connection;
		_options = options.Value;
		Name = _options.Name ?? Constants.DefaultTransportName;
	}

	/// <summary>
	/// 以多播（Fanout）方式向 RabbitMQ 发布消息。
	/// 声明一个 Fanout 类型的交换机，将消息序列化后通过交换机发布到所有绑定的队列。
	/// 支持自动重试，容忍 <see cref="SocketException"/> 和 <see cref="BrokerUnreachableException"/>。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <param name="message">要发布的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步发布操作的任务。</returns>
	public async Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		await using var channel = await _connection.CreateChannelAsync();

		var props = BuildProperties(message);

		await Policy.Handle<SocketException>()
		            .Or<TimeoutException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            _logger.LogDebug("Publishing message to channel '{Channel}' with routing key '{RoutingKey}'", message.Channel, $"{message.Channel}@{_options.RoutingKey}");
			            var messageBody = await _serializer.SerializeAsync(message, cancellationToken);
			            await channel.ExchangeDeclareAsync(message.Channel, ExchangeType.Fanout, cancellationToken: cancellationToken);
			            await channel.BasicPublishAsync(message.Channel, $"{message.Channel}@{_options.RoutingKey}", true, props, messageBody, cancellationToken: cancellationToken);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
		            });
	}

	/// <summary>
	/// 以单播方式向 RabbitMQ 发送消息并等待指定类型的响应。
	/// 当 <typeparamref name="TResponse"/> 为 <see cref="Unit"/>、<see cref="Task"/>、<see cref="ValueTask"/> 或 <c>void</c> 时返回默认值。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <typeparam name="TResponse">期望的响应类型。</typeparam>
	/// <param name="message">要发送的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步发送操作并返回强类型响应的任务。</returns>
	public async Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		var task = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

		CancellationTokenRegistration cancellationRegistration = default;
		if (cancellationToken != CancellationToken.None)
		{
			cancellationRegistration = cancellationToken.Register(() => task.TrySetCanceled());
		}

		var requestQueueName = RabbitMqDelivery.ResolveQueueName(_options, message.Channel, message.GetQueue());

		await using var channel = await _connection.CreateChannelAsync();

		await CheckQueueAsync(channel, requestQueueName);

		// 回复队列必须声明为独占 + 自动删除：默认值会创建一个服务器命名的永久队列，
		// 且全项目从不调用 QueueDeleteAsync，导致每次调用都在 broker 上永久留下一个队列，
		// 迟到的回复还会在其中无界堆积。
		var responseQueueName = (await channel.QueueDeclareAsync(exclusive: true, autoDelete: true, cancellationToken: cancellationToken)).QueueName;
		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += OnReceivedAsync;

		var props = BuildProperties(message, responseQueueName);

		try
		{
			await Policy.Handle<SocketException>()
			            .Or<TimeoutException>()
			            .Or<BrokerUnreachableException>()
			            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(1), (exception, _, retryCount, _) =>
			            {
				            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
			            })
			            .ExecuteAsync(async () =>
			            {
				            _logger.LogDebug("Sending message to queue '{QueueName}' with correlation ID '{CorrelationId}'", requestQueueName, message.CorrelationId);
				            var messageBody = await _serializer.SerializeAsync(message, cancellationToken);
				            await channel.BasicPublishAsync("", requestQueueName, true, props, messageBody, cancellationToken);
				            await channel.BasicConsumeAsync(responseQueueName, true, consumer, cancellationToken: cancellationToken);

				            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
			            });

			return await task.Task;
		}
		finally
		{
			cancellationRegistration.Dispose();
			consumer.ReceivedAsync -= OnReceivedAsync;

			// 显式删除作为兜底：独占队列在连接断开时会被 broker 回收，但显式删除更及时也更明确。
			try
			{
				await channel.QueueDeleteAsync(responseQueueName, cancellationToken: CancellationToken.None);
			}
			catch (Exception exception)
			{
				_logger.LogDebug(exception, "Failed to delete reply queue '{QueueName}'.", responseQueueName);
			}
		}

		async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
		{
			if (args.BasicProperties.CorrelationId != message.CorrelationId)
			{
				return;
			}

			var body = args.Body.ToArray();

			if (typeof(TResponse).IsIn(typeof(Unit), typeof(Task), typeof(ValueTask), typeof(void)))
			{
				var response = _serializer.Deserialize<RabbitMqReply<object>>(Encoding.UTF8.GetString(body));
				if (response.IsSuccess)
				{
					task.TrySetResult(default);
				}
				else
				{
					task.TrySetException(response.Error);
				}
			}
			else
			{
				var response = _serializer.Deserialize<RabbitMqReply<TResponse>>(Encoding.UTF8.GetString(body));
				if (response.IsSuccess)
				{
					task.TrySetResult(response.Result);
				}
				else
				{
					task.TrySetException(response.Error);
				}
			}

			await Task.CompletedTask;
		}
	}

	/// <summary>
	/// 以请求-响应模式调用远程处理程序。
	/// </summary>
	/// <typeparam name="TRequest">请求消息的类型。</typeparam>
	/// <typeparam name="TResponse">响应消息的类型。</typeparam>
	/// <param name="message">请求消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步调用操作并返回响应的任务。</returns>
	/// <exception cref="MessageDeliverException">当传输层未能完成调用时抛出。</exception>
	public async Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		var task = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

		CancellationTokenRegistration cancellationRegistration = default;
		if (cancellationToken != CancellationToken.None)
		{
			cancellationRegistration = cancellationToken.Register(() => task.TrySetCanceled());
		}

		var requestQueueName = RabbitMqDelivery.ResolveQueueName(_options, message.Channel, message.GetQueue());

		await using var channel = await _connection.CreateChannelAsync();

		await CheckQueueAsync(channel, requestQueueName);

		// 回复队列必须声明为独占 + 自动删除，否则每次调用都会在 broker 上永久留下一个服务器命名的队列。
		var responseQueueName = (await channel.QueueDeclareAsync(exclusive: true, autoDelete: true, cancellationToken: cancellationToken)).QueueName;
		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += OnReceivedAsync;

		var props = BuildProperties(message, responseQueueName);

		try
		{
			await Policy.Handle<SocketException>()
			            .Or<TimeoutException>()
			            .Or<BrokerUnreachableException>()
			            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(1), (exception, _, retryCount, _) =>
			            {
				            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
			            })
			            .ExecuteAsync(async () =>
			            {
				            _logger.LogDebug("Sending message to queue '{QueueName}' with correlation ID '{CorrelationId}'", requestQueueName, message.CorrelationId);
				            var messageBody = await _serializer.SerializeAsync(message, cancellationToken);
				            await channel.BasicPublishAsync("", requestQueueName, true, props, messageBody, cancellationToken);
				            await channel.BasicConsumeAsync(responseQueueName, true, consumer, cancellationToken: cancellationToken);

				            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
			            });

			return await task.Task;
		}
		finally
		{
			cancellationRegistration.Dispose();
			consumer.ReceivedAsync -= OnReceivedAsync;

			try
			{
				await channel.QueueDeleteAsync(responseQueueName, cancellationToken: CancellationToken.None);
			}
			catch (Exception exception)
			{
				_logger.LogDebug(exception, "Failed to delete reply queue '{QueueName}'.", responseQueueName);
			}
		}

		async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
		{
			if (args.BasicProperties.CorrelationId != message.CorrelationId)
			{
				return;
			}

			var body = args.Body.ToArray();

			var response = _serializer.Deserialize<RabbitMqReply<TResponse>>(Encoding.UTF8.GetString(body));
			if (response.IsSuccess)
			{
				task.TrySetResult(response.Result);
			}
			else
			{
				task.TrySetException(response.Error);
			}

			await Task.CompletedTask;
		}
	}

	private BasicProperties BuildProperties(IMessageEnvelope message, string replyTo = null)
	{
		var props = new BasicProperties
		{
			CorrelationId = message.CorrelationId,
			ContentEncoding = "utf-8",
			ContentType = "application/json",
			Type = message.TypeName,
			ReplyTo = replyTo,
			MessageId = message.MessageId,
			UserId = message.User?.Identity?.Name,
			// 未启用优先级（MaxPriority <= 0）时返回 null，表示不设置该属性。
			Priority = RabbitMqDelivery.ResolvePriority(message.GetPriority(), _options.MaxPriority) ?? 0,
		};
		props.Headers ??= new Dictionary<string, object>();
		props.Headers[MessageHeaders.ConversationId] = message.ConversationId;
		props.Headers[MessageHeaders.RequestTraceId] = message.RequestTraceId;
		props.Headers[MessageHeaders.Authorization] = message.Authorization;
		props.Headers[MessageHeaders.Channel] = message.Channel;
		return props;
	}

	/// <summary>
	/// 根据通道名称构建 RabbitMQ 队列名称。
	/// 队列名称格式为：<c>{channel}@{subscriptionId}</c>，
	/// 具体规则与消费端共用 <see cref="RabbitMqDelivery.ResolveQueueName"/>。
	/// 注意 <see cref="RabbitMqBusOptions.QueueNamePrefix"/> 目前**未被使用**，队列名不含该前缀。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <returns>生成的队列名称。</returns>
	private string GetQueueName(string channel)
	{
		return RabbitMqDelivery.ResolveQueueName(_options, channel);
	}

	/// <summary>
	/// 检查指定队列是否存在且有消费者。
	/// 如果队列不存在（404 错误）或消费者数为零，则抛出 <see cref="MessageDeliverException"/>。
	/// </summary>
	/// <param name="channel">RabbitMQ 通道（IChannel）。</param>
	/// <param name="requestQueueName">要检查的队列名称。</param>
	/// <exception cref="MessageDeliverException">当队列不存在或没有消费者时抛出。</exception>
	private static async Task CheckQueueAsync(IChannel channel, string requestQueueName)
	{
		try
		{
			var queueDeclare = await channel.QueueDeclarePassiveAsync(requestQueueName);

			if (queueDeclare == null)
			{
				throw new MessageDeliverException("Channel not found in vhost '/'.");
			}

			if (queueDeclare.ConsumerCount < 1)
			{
				throw new MessageDeliverException("No consumer found for the channel.");
			}
		}
		catch (OperationInterruptedException exception) when (exception.ShutdownReason?.ReplyCode == 404)
		{
			throw new MessageDeliverException("No consumer found for the channel.");
		}
	}
}