namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents errors that occur during message transport operations.
/// </summary>
/// <remarks>This exception is typically thrown when a failure occurs while sending or receiving messages over a
/// transport layer, such as a network or inter-process communication channel. Use this exception to distinguish
/// transport-related errors from other types of exceptions in messaging scenarios.</remarks>
public class MessageTransportException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessageTransportException"/> class.
	/// </summary>
	/// <param name="message">The message.</param>
	public MessageTransportException(string message)
		: base(message)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="MessageTransportException"/> class.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="innerException">The inner exception.</param>
	public MessageTransportException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
