using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The message context.
/// </summary>
public sealed class MessageContext : IMessageContext
{
	private readonly WeakEventManager _events = new();

	private readonly Dictionary<string, string> _headers = new();

	private bool _disposedValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageContext"/> class.
	/// </summary>
	public MessageContext()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageContext"/> class.
	/// </summary>
	/// <param name="message"></param>
	public MessageContext(object message)
		: this()
	{
		Message = message;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageContext"/> class.
	/// </summary>
	/// <param name="pack"></param>
	public MessageContext(IRoutedMessage pack)
		: this(pack.Data)
	{
		MessageId = pack.MessageId;
		CorrelationId = pack.CorrelationId;
		ConversationId = pack.ConversationId;
		RequestTraceId = pack.RequestTraceId;
		Authorization = pack.Authorization;
		User = pack.User;
		Metadata = pack.Metadata;
	}

	/// <summary>
	/// Invoked while message was handled and replied to dispatcher.
	/// </summary>
	public event EventHandler<MessageRepliedEventArgs> Responded
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <summary>
	/// Invoke while message context disposed.
	/// </summary>
	public event EventHandler<MessageHandledEventArgs> Completed
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <summary>
	/// Invoked while message handling was failed.
	/// </summary>
	public event EventHandler<Exception> Failed
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <inheritdoc />
	public object Message { get; }

	/// <inheritdoc />
	public string MessageId
	{
		get => _headers.GetValueOrDefault(nameof(MessageId));
		set => _headers[nameof(MessageId)] = value;
	}

	/// <inheritdoc />
	public string CorrelationId
	{
		get => _headers.GetValueOrDefault(nameof(CorrelationId));
		set => _headers[nameof(CorrelationId)] = value;
	}

	/// <inheritdoc />
	public string ConversationId
	{
		get => _headers.GetValueOrDefault(nameof(ConversationId));
		set => _headers[nameof(ConversationId)] = value;
	}

	/// <inheritdoc />
	public string RequestTraceId
	{
		get => _headers.GetValueOrDefault(nameof(RequestTraceId));
		set => _headers[nameof(RequestTraceId)] = value;
	}

	/// <inheritdoc />
	public string Authorization
	{
		get => _headers.GetValueOrDefault(nameof(Authorization));
		set => _headers[nameof(Authorization)] = value;
	}

	/// <inheritdoc/>
	public IPrincipal User { get; }

	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> Headers => _headers;

	/// <summary>
	/// Gets or sets a <see cref="MessageMetadata"/> instance that contains the metadata information of the message.
	/// </summary>
	public MessageMetadata Metadata { get; set; }

	/// <summary>
	/// Replies message handling result to message dispatcher.
	/// </summary>
	/// <param name="message">The message to reply.</param>
	public void Response(object message)
	{
		_events.HandleEvent(this, new MessageRepliedEventArgs(message), nameof(Responded));
	}

	/// <summary>
	/// Replies message handling result to message dispatcher.
	/// </summary>
	/// <typeparam name="TMessage">The type of the message.</typeparam>
	/// <param name="message">The message to reply.</param>
	public void Response<TMessage>(TMessage message)
	{
		Response((object)message);
	}

	/// <summary>
	/// Called after the message handling was failed.
	/// </summary>
	/// <param name="exception"></param>
	public void Failure(Exception exception)
	{
		_events.HandleEvent(this, exception, nameof(Failed));
	}

	/// <summary>
	/// Called after the message has been handled.
	/// This operate will raised up the <see cref="Completed"/> event.
	/// </summary>
	/// <param name="message"></param>
	public void Complete(object message)
	{
		_events.HandleEvent(this, new MessageHandledEventArgs(message), nameof(Completed));
	}

	/// <summary>
	/// Called after the message has been handled.
	/// This operate will raised up the <see cref="Completed"/> event.
	/// </summary>
	/// <param name="message"></param>
	/// <param name="handlerType"></param>
	public void Complete(object message, Type handlerType)
	{
		_events.HandleEvent(this, new MessageHandledEventArgs(message) { HandlerType = handlerType }, nameof(Completed));
	}

	/// <summary>
	/// Called after the message has been handled.
	/// </summary>
	/// <param name="disposing"></param>
	private void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}

		if (disposing)
		{
			Complete(Message);
		}

		_events.RemoveEventHandlers();
		_disposedValue = true;
	}

	/// <summary>
	/// Finalizes the current instance of the <see cref="MessageContext"/> class.
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