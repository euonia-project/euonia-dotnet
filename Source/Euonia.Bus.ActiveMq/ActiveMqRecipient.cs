using System.Reflection;
using Apache.NMS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 表示 ActiveMQ 消息接收端的抽象基类。
/// 封装了连接、会话、消息反序列化、消息处理分发以及资源释放等通用逻辑。
/// </summary>
internal abstract class ActiveMqRecipient : DisposableObject
{
	/// <summary>
	/// 当消息被接收时触发。
	/// </summary>
	public event EventHandler<MessageReceivedEventArgs> MessageReceived;

	/// <summary>
	/// 当消息被确认后触发。
	/// </summary>
	public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;

	/// <summary>
	/// 消息序列化器，用于消息信封的序列化与反序列化。
	/// </summary>
	private readonly IMessageSerializer _serializer;

	/// <summary>
	/// 初始化 <see cref="ActiveMqRecipient"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析依赖项的服务提供程序。</param>
	/// <param name="channelName">当前接收器要监听的消息通道名称。</param>
	/// <param name="messageType">当前接收器要处理的消息类型。</param>
	protected ActiveMqRecipient(IServiceProvider provider, string channelName, Type messageType)
	{
		Handler = provider.GetRequiredService<IHandlerContext>();
		Options = provider.GetRequiredService<IOptions<ActiveMqBusOptions>>().Value;
		Connection = provider.GetRequiredService<IPersistentConnection>();
		ChannelName = channelName;
		MessageType = messageType;
		_serializer = provider.GetKeyedService<IMessageSerializer>(Options.SerializerProvider);
	}

	/// <summary>
	/// 获取消息处理器上下文，用于执行具体的消息处理逻辑。
	/// </summary>
	protected virtual IHandlerContext Handler { get; }

	/// <summary>
	/// 获取或设置当前接收器处理的消息类型。
	/// </summary>
	protected Type MessageType { get; set; }

	/// <summary>
	/// 获取当前接收器监听的消息通道名称。
	/// </summary>
	protected string ChannelName { get; }

	/// <summary>
	/// 获取用于与 ActiveMQ 进行通信的持久连接。
	/// </summary>
	protected IPersistentConnection Connection { get; }

	/// <summary>
	/// 获取 ActiveMQ 消息总线的配置选项。
	/// </summary>
	protected virtual ActiveMqBusOptions Options { get; }

	/// <summary>
	/// 获取或设置当前接收器使用的消息会话。
	/// </summary>
	protected virtual ISession Session { get; set; }

	/// <summary>
	/// 获取或设置当前接收器关联的消息消费者。
	/// </summary>
	protected IMessageConsumer Consumer { get; set; }
	// {
	// 	get
	// 	{
	// 		if (Session == null)
	// 		{
	// 			throw new InvalidOperationException("Session is not initialized.");
	// 		}
	//
	// 		var consumer = _consumer;
	// 		if (consumer != null)
	// 		{
	// 			return consumer;
	// 		}
	//
	// 		lock (_consumerLock)
	// 		{
	// 			consumer = _consumer;
	// 			if (consumer == null)
	// 			{
	// 				var destination = Session.GetQueue($"queue://Consumer.{SubscriptionId}.VirtualTopic.{ChannelName}");
	// 				consumer = Session.CreateConsumer(destination);
	// 				consumer.Listener += HandleMessageReceived;
	// 				_consumer = consumer;
	// 			}
	// 		}
	//
	// 		return _consumer;
	// 	}
	// }

	/// <summary>
	/// 处理接收到的消息。
	/// 该方法负责验证消息格式、反序列化消息内容、构造消息上下文、
	/// 调用具体处理逻辑，并在需要时发送回复消息。
	/// </summary>
	/// <param name="message">接收到的 ActiveMQ 消息。</param>
	/// <param name="cancellationToken">用于取消消息处理操作的取消令牌。</param>
	/// <returns>表示异步消息处理操作的任务。</returns>
	protected virtual async Task HandleMessageReceivedAsync(IMessage message, CancellationToken cancellationToken)
	{
		if (message is not ITextMessage textMessage || string.IsNullOrEmpty(textMessage.Text))
		{
			return;
		}

		var envelope = _serializer.DeserializeEnvelope(textMessage.Text, MessageType);

		var context = new MessageContext(envelope);

		MessageReceived?.Invoke(this, new MessageReceivedEventArgs(envelope.Payload, context));

		var taskCompletion = new TaskCompletionSource<object>();
		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => taskCompletion.TrySetCanceled(), false);
		}

		context.Responded += OnResponded;
		context.Failed += OnFailed;
		context.Completed += OnCompleted;

		ActiveMqReply<object> reply;

		try
		{
			await HandleAsync(ChannelName, envelope.Payload, context, cancellationToken);

			var result = await taskCompletion.Task;
			reply = ActiveMqReply<object>.Success(result);
		}
		catch (Exception exception)
		{
			reply = ActiveMqReply<object>.Failure(exception);
		}

		if (message.NMSReplyTo != null)
		{
			var response = _serializer.Serialize(reply);

			using var responder = await Session.CreateProducerAsync(message.NMSReplyTo);
			var responseMessage = await Session.CreateTextMessageAsync(response);

			// 建立关联 ID，使请求方能够将回复与原始请求消息进行对应。
			responseMessage.NMSCorrelationID = message.NMSCorrelationID;

			// 发送回复消息。
			await responder.SendAsync(responseMessage);
		}

		MessageAcknowledged?.Invoke(this, new MessageAcknowledgedEventArgs(envelope.Payload, context));

		void OnResponded(object s, MessageRepliedEventArgs e)
		{
			if (message.NMSReplyTo == null || string.IsNullOrWhiteSpace(message.NMSCorrelationID))
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

		// 取消订阅上下文事件，避免在长生命周期场景中产生重复调用或额外的引用保留。
		context.Responded -= OnResponded;
		context.Failed -= OnFailed;
		context.Completed -= OnCompleted;
	}

	/// <summary>
	/// 获取当前订阅的标识符。
	/// 优先使用配置中的订阅标识；若未配置，则回退到入口程序集名称；
	/// 若仍不可用，则使用默认值 <c>DefaultSubscription</c>。
	/// </summary>
	protected virtual string SubscriptionId => string.Collapse(Options?.SubscriptionId, Assembly.GetEntryAssembly()?.GetName().Name, "DefaultSubscription");

	/// <summary>
	/// 启动消息接收器并开始监听消息。
	/// 具体启动方式由派生类实现。
	/// </summary>
	/// <param name="cancellationToken">用于取消启动操作的取消令牌。</param>
	/// <returns>表示异步启动操作的任务。</returns>
	internal abstract Task StartAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行具体的消息处理逻辑。
	/// 该方法通过消息处理器上下文分发消息，并将处理结果或异常写入消息上下文。
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

	/// <summary>
	/// 释放消费者占用的资源。
	/// 取消订阅消息监听器，并在可用时释放消费者与会话对象。
	/// </summary>
	/// <param name="disposing">指示是否正在释放托管资源。</param>
	protected override void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		// 这里不要通过属性访问消费者，以避免在会话未初始化时触发额外异常。
		if (Consumer != null)
		{
			Consumer.AsyncListener -= HandleMessageReceivedAsync;
			Consumer.Dispose();
		}

		// 如果会话已创建，则释放它。
		try
		{
			Session?.Dispose();
		}
		catch
		{
			// 释放阶段吞掉异常，避免在终结或清理路径中再次抛出异常。
		}
	}
}