using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 基于 RabbitMQ 的 <see cref="ITransporter"/> 实现。
/// </summary>
public class RabbitMqTransporter : ITransporter
{
	/// <summary>
	/// 当消息成功投递到 RabbitMQ 时触发。
	/// </summary>
	public event EventHandler<MessageDeliveredEventArgs> Delivered;

	private readonly RabbitMqBusOptions _options;
	private readonly IPersistentConnection _connection;
	private readonly ILogger<RabbitMqTransporter> _logger;

	/// <summary>
	/// 获取传输器名称。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化 <see cref="RabbitMqTransporter"/> 的新实例。
	/// </summary>
	/// <param name="connection">用于与 RabbitMQ 建立和管理持久连接的工厂。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 RabbitMQ 总线配置选项。</param>
	/// <param name="logger">用于创建类型化日志记录器的日志工厂。</param>
	public RabbitMqTransporter(IPersistentConnection connection, IOptions<RabbitMqBusOptions> options, ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<RabbitMqTransporter>();
		_connection = connection;
		_options = options.Value;
		Name = _options.Name ?? nameof(RabbitMqTransporter);
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

		var props = BuildProperties(message.TypeName);

		await Policy.Handle<SocketException>()
		            .Or<TimeoutException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(3), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            var messageBody = await SerializeAsync(message, cancellationToken);

			            var exchangePrefix = string.Collapse(_options.ExchangeNamePrefix, Constants.DefaultExchangeNamePrefix);
			            var exchangeName = $"{exchangePrefix}:{message.Channel}";

			            await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, cancellationToken: cancellationToken);
			            await channel.BasicPublishAsync(exchangeName, $"{exchangeName}@{_options.RoutingKey}", true, props, messageBody, cancellationToken: cancellationToken);

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
		var task = new TaskCompletionSource<TResponse>();

		var requestQueueName = GetQueueName(message.Channel);

		await using var channel = await _connection.CreateChannelAsync();

		await CheckQueueAsync(channel, requestQueueName);

		var responseQueueName = (await channel.QueueDeclareAsync(cancellationToken: cancellationToken)).QueueName;
		var consumer = new AsyncEventingBasicConsumer(channel);

		consumer.ReceivedAsync += OnReceivedAsync;

		var props = BuildProperties(message.TypeName, message.CorrelationId, responseQueueName);

		await Policy.Handle<SocketException>()
		            .Or<TimeoutException>()
		            .Or<BrokerUnreachableException>()
		            .WaitAndRetryAsync(_options.MaxFailureRetries, _ => TimeSpan.FromSeconds(1), (exception, _, retryCount, _) =>
		            {
			            _logger.LogError(exception, "Retry:{RetryCount}, {Message}", retryCount, exception.Message);
		            })
		            .ExecuteAsync(async () =>
		            {
			            var messageBody = await SerializeAsync(message, cancellationToken);
			            await channel.BasicPublishAsync("", requestQueueName, true, props, messageBody, cancellationToken);
			            await channel.BasicConsumeAsync(responseQueueName, true, consumer, cancellationToken: cancellationToken);

			            Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload, null));
		            });

		var result = await task.Task;
		consumer.ReceivedAsync -= OnReceivedAsync;
		return result;

		async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
		{
			if (args.BasicProperties.CorrelationId != message.CorrelationId)
			{
				return;
			}

			var body = args.Body.ToArray();

			if (typeof(TResponse).IsIn(typeof(Unit), typeof(Task), typeof(ValueTask), typeof(void)))
			{
				var response = JsonConvert.DeserializeObject<RabbitMqReply<object>>(Encoding.UTF8.GetString(body), Constants.SerializerSettings);
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
				var response = JsonConvert.DeserializeObject<RabbitMqReply<TResponse>>(Encoding.UTF8.GetString(body), Constants.SerializerSettings);
				if (response.IsSuccess)
				{
					task.SetResult(response.Result);
				}
				else
				{
					task.SetException(response.Error);
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
	/// <exception cref="NotImplementedException">始终抛出，此方法当前未实现。</exception>
	public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	private static BasicProperties BuildProperties(string messageType, string correlationId = null, string replyTo = null)
	{
		var props = new BasicProperties
		{
			CorrelationId = correlationId,
			ContentEncoding = "utf-8",
			ContentType = "application/json",
			Type = messageType,
			ReplyTo = replyTo
		};
		props.Headers ??= new Dictionary<string, object>();
		props.Headers[MessageHeaders.MessageType] = messageType;
		return props;
	}

	/// <summary>
	/// 根据通道名称构建 RabbitMQ 队列名称。
	/// 队列名称格式为：<c>{QueueNamePrefix}:{channel}@{subscriptionId}</c>。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <returns>生成的队列名称。</returns>
	private string GetQueueName(string channel)
	{
		var subscriptionId = string.Collapse(_options.SubscriptionId, Assembly.GetEntryAssembly()?.FullName, channel);
		var requestQueueName = $"{string.Collapse(_options.QueueNamePrefix, Constants.DefaultQueueNamePrefix)}:{channel}@{subscriptionId}";
		return requestQueueName;
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

	/// <summary>
	/// 将消息信封序列化为 UTF-8 字节数组。
	/// 使用不带 BOM 的 UTF-8 编码，避免 RabbitMQ 客户端反序列化失败。
	/// </summary>
	/// <typeparam name="TMessage">消息负载的类型。</typeparam>
	/// <param name="message">要序列化的消息信封。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>序列化后的 UTF-8 字节数组；如果 <paramref name="message"/> 为 null，则返回空数组。</returns>
	private static async Task<byte[]> SerializeAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
	{
		if (message == null)
		{
			return [];
		}

		await using var stream = new MemoryStream();
		// 默认的 UTF8Encoding 会输出 BOM，将导致 RabbitMQ 客户端反序列化消息失败。
		await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
		{
			await using var jsonWriter = new JsonTextWriter(writer);

			JsonSerializer.CreateDefault(Constants.SerializerSettings)
			              .Serialize(jsonWriter, message);

			await jsonWriter.FlushAsync(cancellationToken);
			await writer.FlushAsync(cancellationToken);
		}

		return stream.ToArray();
	}
}