using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息上下文。
/// </summary>
public sealed class MessageContext : IMessageContext
{
	private readonly WeakEventManager _events = new();

	private readonly Dictionary<string, string> _headers = new();

	private bool _disposedValue;

	/// <summary>
	/// 初始化 <see cref="MessageContext"/> 类的新实例。
	/// </summary>
	public MessageContext()
	{
	}

	/// <summary>
	/// 使用指定的消息信封初始化 <see cref="MessageContext"/> 类的新实例。
	/// </summary>
	/// <param name="envelope">消息信封实例。</param>
	public MessageContext(IMessageEnvelope envelope)
	{
		MessageId = envelope.MessageId;
		CorrelationId = envelope.CorrelationId;
		ConversationId = envelope.ConversationId;
		RequestTraceId = envelope.RequestTraceId;
		Authorization = envelope.Authorization;
		User = envelope.User;
		Metadata = envelope.Metadata;
	}

	/// <summary>
	/// 当消息处理完成并向分发器回复时触发。
	/// </summary>
	public event EventHandler<MessageRepliedEventArgs> Responded
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <summary>
	/// 当消息上下文释放时触发。
	/// </summary>
	public event EventHandler<MessageHandledEventArgs> Completed
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <summary>
	/// 当消息处理失败时触发。
	/// </summary>
	public event EventHandler<Exception> Failed
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public string MessageId
	{
		get => _headers.GetValueOrDefault(MessageHeaders.MESSAGE_ID);
		set => _headers[MessageHeaders.MESSAGE_ID] = value;
	}

	/// <inheritdoc />
	public string CorrelationId
	{
		get => _headers.GetValueOrDefault(MessageHeaders.CORRELATION_ID);
		set => _headers[MessageHeaders.CORRELATION_ID] = value;
	}

	/// <inheritdoc />
	public string ConversationId
	{
		get => _headers.GetValueOrDefault(MessageHeaders.CONVERSATION_ID);
		set => _headers[MessageHeaders.CONVERSATION_ID] = value;
	}

	/// <inheritdoc />
	public string RequestTraceId
	{
		get => _headers.GetValueOrDefault(MessageHeaders.REQUEST_TRACE_ID);
		set => _headers[MessageHeaders.REQUEST_TRACE_ID] = value;
	}

	/// <inheritdoc />
	public string Authorization
	{
		get => _headers.GetValueOrDefault(MessageHeaders.AUTHORIZATION);
		set => _headers[MessageHeaders.AUTHORIZATION] = value;
	}

	/// <inheritdoc/>
	public IPrincipal User { get; }

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> Headers => _headers;

	/// <summary>
	/// 获取或设置包含消息元数据信息的 <see cref="MessageMetadata"/> 实例。
	/// </summary>
	public MessageMetadata Metadata { get; set; }

	/// <summary>
	/// 向消息分发器回复消息处理结果。
	/// </summary>
	/// <param name="message">要回复的消息。</param>
	public void Response(object message)
	{
		_events.HandleEvent(this, new MessageRepliedEventArgs(message), nameof(Responded));
	}

	/// <summary>
	/// 向消息分发器回复消息处理结果。
	/// </summary>
	/// <typeparam name="TMessage">消息的类型。</typeparam>
	/// <param name="message">要回复的消息。</param>
	public void Response<TMessage>(TMessage message)
	{
		Response((object)message);
	}

	/// <summary>
	/// 在消息处理失败后调用。
	/// </summary>
	/// <param name="exception">处理过程中发生的异常。</param>
	public void Failure(Exception exception)
	{
		_events.HandleEvent(this, exception, nameof(Failed));
	}

	/// <summary>
	/// 在消息处理完成后调用。此操作将触发 <see cref="Completed"/> 事件。
	/// </summary>
	/// <param name="messageId">已完成处理的消息标识符。</param>
	public void Complete(string messageId)
	{
		_events.HandleEvent(this, new MessageHandledEventArgs(messageId), nameof(Completed));
	}

	/// <summary>
	/// 在消息处理完成后调用。此操作将触发 <see cref="Completed"/> 事件。
	/// </summary>
	/// <param name="messageId">已完成处理的消息标识符。</param>
	/// <param name="handlerType">处理该消息的处理程序类型。</param>
	public void Complete(string messageId, Type handlerType)
	{
		_events.HandleEvent(this, new MessageHandledEventArgs(messageId) { HandlerType = handlerType }, nameof(Completed));
	}

	/// <summary>
	/// 释放当前实例所使用的资源。
	/// </summary>
	/// <param name="disposing">指示是否正在主动释放托管资源。</param>
	private void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}

		if (disposing)
		{
			Complete(MessageId);
		}

		_events.RemoveEventHandlers();
		_disposedValue = true;
	}

	/// <summary>
	/// 终止 <see cref="MessageContext"/> 类的当前实例。
	/// </summary>
	~MessageContext()
	{
		Dispose(disposing: false);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}