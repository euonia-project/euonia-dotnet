namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represent the exception was thrown by message bus.
/// Implements the <see cref="Exception" />
/// </summary>
/// <seealso cref="Exception" />
[Serializable]
public class MessageBusException : Exception
{
	private readonly object _message;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageBusException"/> class.
	/// </summary>
	/// <param name="messageContext">Type of the message.</param>
	public MessageBusException(object messageContext)
	{
		_message = messageContext;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageBusException"/> class.
	/// </summary>
	/// <param name="messageContext">Type of the message.</param>
	/// <param name="message">The message.</param>
	public MessageBusException(object messageContext, string message)
		: base(message)
	{
		_message = messageContext;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageBusException"/> class.
	/// </summary>
	/// <param name="messageContext">Type of the message.</param>
	/// <param name="message">The message.</param>
	/// <param name="innerException">The inner exception.</param>
	public MessageBusException(object messageContext, string message, Exception innerException)
		: base(message, innerException)
	{
		_message = messageContext;
	}

	/// <summary>
	/// The type of the handled message.
	/// </summary>
	/// <value>The type of the message.</value>
	public virtual object MessageContext => _message;
}