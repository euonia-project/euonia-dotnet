using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 消息接收器的基类。
/// </summary>
public abstract class RabbitMqRecipient : DisposableObject
{
	/// <summary>
	/// 当消息被接收时触发。
	/// </summary>
	public event EventHandler<MessageReceivedEventArgs> MessageReceived;

	/// <summary>
	/// 当消息被确认（ACK）时触发。
	/// </summary>
	public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;

	/// <summary>
	/// 消息序列化器，用于消息的序列化与反序列化。
	/// </summary>
	private readonly IMessageSerializer _serializer;

	/// <summary>
	/// 初始化 <see cref="RabbitMqRecipient"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析服务（如消息序列化器）的服务提供程序。</param>
	/// <param name="factory">用于建立和管理 RabbitMQ 连接的持久连接工厂。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="options">包装在 <see cref="IOptions{T}"/> 中的 <see cref="RabbitMqBusOptions"/> 配置。</param>
	protected RabbitMqRecipient(IServiceProvider provider, IPersistentConnection factory, IHandlerContext handler, IOptions<RabbitMqBusOptions> options)
	{
		Handler = handler;
		Options = options.Value;
		Connection = factory;
		_serializer = provider.GetKeyedService<IMessageSerializer>(Options.SerializerProvider);
	}

	/// <summary>
	/// 消息处理器上下文，用于执行具体的消息处理逻辑。
	/// </summary>
	protected virtual IHandlerContext Handler { get; }

	/// <summary>
	/// 获取或设置此接收器处理的消息类型。
	/// </summary>
	internal Type MessageType { get; set; }

	/// <summary>
	/// 获取用于与 RabbitMQ 进行通信的持久连接。
	/// </summary>
	protected IPersistentConnection Connection { get; }

	/// <summary>
	/// 获取 RabbitMQ 消息总线的配置选项。
	/// </summary>
	protected virtual RabbitMqBusOptions Options { get; }

	/// <summary>
	/// 获取或设置 RabbitMQ 消息通道。
	/// </summary>
	protected virtual IChannel Channel { get; set; }

	/// <summary>
	/// 获取 RabbitMQ 消费者实例。
	/// </summary>
	/// <exception cref="InvalidOperationException">当 RabbitMQ 通道尚未初始化时抛出。</exception>
	protected virtual AsyncEventingBasicConsumer Consumer
	{
		get
		{
			if (Channel == null)
			{
				throw new InvalidOperationException("The RabbitMQ channel is not initialized.");
			}

			field ??= new AsyncEventingBasicConsumer(Channel);
			field.ReceivedAsync += HandleMessageReceivedAsync;
			return field;
		}
	}

	/// <summary>
	/// 执行具体的消息处理逻辑。调用处理器上下文处理消息。
	/// </summary>
	/// <param name="channel">消息所在的通道名称。</param>
	/// <param name="message">接收到的消息对象。</param>
	/// <param name="context">消息上下文，用于跟踪处理状态与结果。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步消息处理操作的任务。</returns>
	protected virtual async Task HandleAsync(string channel, object message, MessageContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await Handler.HandleAsync(channel, message, context, cancellationToken);
			context.Response(result);
		}
		catch (Exception exception)
		{
			context.Failure(exception);
		}
		finally
		{
			context.Complete(null);
		}
	}

	// protected virtual void AcknowledgeMessage(ulong deliveryTag)
	// {
	// 	Channel.BasicAck(deliveryTag, false);
	// }

	/// <summary>
	/// 启动接收器，开始监听指定通道的消息。
	/// </summary>
	/// <param name="channel">要监听的通道名称。</param>
	internal abstract Task StartAsync(string channel);

	/// <summary>
	/// 处理来自 RabbitMQ 的原始消息投递事件。
	/// </summary>
	/// <remarks>
	///	负责将 <see cref="BasicDeliverEventArgs"/> 转换为内部消息格式并触发处理流程。反序列化消息信封，构建消息上下文，执行处理器并通过 TaskCompletionSource 等待处理结果，然后根据请求属性决定是否发送响应，最后确认（ACK）消息。
	/// </remarks>
	/// <param name="sender">事件发送方。</param>
	/// <param name="args">包含投递消息数据的 RabbitMQ 事件参数。</param>
	protected virtual async Task HandleMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
	{
		var message = DeserializeMessage(args.Body.ToArray(), MessageType);

		var props = args.BasicProperties;

		var context = new MessageContext(message);

		OnMessageReceived(new MessageReceivedEventArgs(message.Payload, context));

		var taskCompletion = new TaskCompletionSource<object>();
		if (args.CancellationToken != CancellationToken.None)
		{
			args.CancellationToken.Register(() => taskCompletion.TrySetCanceled(), false);
		}

		RabbitMqReply<object> reply;

		try
		{
			context.Responded += OnResponded;
			context.Failed += OnFailed;
			context.Completed += OnCompleted;

			await HandleAsync(message.Channel, message.Payload, context);

			var result = await taskCompletion.Task;
			reply = RabbitMqReply<object>.Success(result);
		}
		catch (Exception exception)
		{
			reply = RabbitMqReply<object>.Failure(exception);
		}

		if (!string.IsNullOrEmpty(props.CorrelationId) || !string.IsNullOrWhiteSpace(props.ReplyTo))
		{
			var replyProps = new BasicProperties();
			replyProps.Headers ??= new Dictionary<string, object>();
			replyProps.CorrelationId = props.CorrelationId;
			replyProps.Type = reply.Result?.GetType().Name;

			var response = SerializeMessage(reply);
			await Channel.BasicPublishAsync(string.Empty, props.ReplyTo!, true, replyProps, response);
		}

		await Channel.BasicAckAsync(args.DeliveryTag, false);

		OnMessageAcknowledged(new MessageAcknowledgedEventArgs(message.Payload, context));

		void OnResponded(object s, MessageRepliedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(props.ReplyTo) && string.IsNullOrWhiteSpace(props.CorrelationId))
			{
				return;
			}

			taskCompletion.TrySetResult(e.Result);
		}

		void OnFailed(object s, Exception exception)
		{
			taskCompletion.TrySetException(exception);
		}

		void OnCompleted(object s, MessageHandledEventArgs e)
		{
			taskCompletion.TryCompleteFromCompletedTask(Task.FromResult(default(object)));
		}
	}

	/// <summary>
	/// 触发 <see cref="MessageAcknowledged"/> 事件。
	/// </summary>
	/// <param name="args">包含确认信息的消息确认事件参数。</param>
	protected virtual void OnMessageAcknowledged(MessageAcknowledgedEventArgs args)
	{
		MessageAcknowledged?.Invoke(this, args);
	}

	/// <summary>
	/// 触发 <see cref="MessageReceived"/> 事件。
	/// </summary>
	/// <param name="args">包含接收信息的消息接收事件参数。</param>
	protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
	{
		MessageReceived?.Invoke(this, args);
	}

	/// <summary>
	/// 将消息对象序列化为字节数组。
	/// 使用 JSON 格式进行序列化，null 消息返回空字节数组。
	/// </summary>
	/// <param name="message">要序列化的消息对象。</param>
	/// <returns>序列化后的 UTF-8 字节数组；如果 <paramref name="message"/> 为 null，则返回空数组。</returns>
	protected virtual byte[] SerializeMessage(object message)
	{
		if (message == null)
		{
			return Array.Empty<byte>();
		}

		var json = _serializer.Serialize(message);
		return Encoding.UTF8.GetBytes(json);
	}

	/// <summary>
	/// 将字节数组反序列化为指定类型的消息信封。
	/// </summary>
	/// <param name="message">包含序列化消息的字节数组。</param>
	/// <param name="messageType">消息的运行时类型。</param>
	/// <returns>反序列化后的 <see cref="IMessageEnvelope"/> 实例。</returns>
	protected virtual IMessageEnvelope DeserializeMessage(byte[] message, Type messageType)
	{
		var json = Encoding.UTF8.GetString(message);
		return _serializer.DeserializeEnvelope(json, messageType);
	}

	/// <summary>
	/// 从消息头字典中获取指定键的值。
	/// 支持字符串和字节数组类型的值。
	/// </summary>
	/// <param name="header">消息头键值对字典，可能为 null。</param>
	/// <param name="key">要查找的键名。</param>
	/// <returns>找到的字符串值；如果头字典为 null 或未找到指定键，则返回 <see cref="string.Empty"/>。</returns>
	protected virtual string GetHeaderValue(IDictionary<string, object> header, string key)
	{
		if (header == null)
		{
			return string.Empty;
		}

		if (header.TryGetValue(key, out var value))
		{
			return value switch
			{
				null => string.Empty,
				string @string => @string,
				byte[] bytes => Encoding.UTF8.GetString(bytes),
				_ => value.ToString()
			};
		}

		return string.Empty;
	}

	/// <summary>
	/// 声明死信交换机（DLX）、死信队列（DLQ）及二者的绑定关系。
	/// 返回包含死信配置的队列参数；若未启用死信功能（<see cref="RabbitMqBusOptions.IsDeadLetterEnabled"/>），则返回 <c>null</c>。
	/// </summary>
	/// <param name="channel">用于声明交换机、队列及绑定的 RabbitMQ 通道。</param>
	/// <param name="channelName">当前消息通道的名称，用于构建死信交换机与死信队列的名称。</param>
	/// <returns>包含 <c>x-dead-letter-exchange</c> 和 <c>x-dead-letter-routing-key</c> 的队列参数字典；未启用死信功能时返回 <c>null</c>。</returns>
	protected async Task<IDictionary<string, object>> DeclareDeadLetterAsync(IChannel channel, string channelName)
	{
		if (!Options.IsDeadLetterEnabled)
		{
			return null;
		}

		string dlxName = $"{channelName}.dlx", dlqName = $"{channelName}.dlq";

		await channel.ExchangeDeclareAsync(dlxName, ExchangeType.Fanout, true, false);
		await channel.QueueDeclareAsync(dlqName, true, false, false);
		await channel.QueueBindAsync(dlqName, dlxName, Constants.DefaultDlxRoutingKey);

		return new Dictionary<string, object>
		{
			{ "x-dead-letter-exchange", dlxName },
			{ "x-dead-letter-routing-key", Constants.DefaultDlxRoutingKey },
		};
	}

	/// <summary>
	/// 释放消费者占用的资源。
	/// 取消订阅消息接收事件，并释放通道与连接。
	/// </summary>
	/// <param name="disposing">指示是否正在释放托管资源。</param>
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		if (Consumer != null)
		{
			Consumer.ReceivedAsync -= HandleMessageReceivedAsync;
		}

		Channel?.Dispose();
	}
}