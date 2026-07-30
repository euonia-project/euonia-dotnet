namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// Defines a message pack to transport.
/// </summary>
public sealed class MessagePack
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	/// <param name="context"></param>
	public MessagePack(IMessageEnvelope message, MessageContext context)
	{
		Message = message;
		Context = context;
	}

	/// <summary>
	/// Get the message.
	/// </summary>
	public IMessageEnvelope Message { get; }

	/// <summary>
	/// Get the message context.
	/// </summary>
	public MessageContext Context { get; }

	/// <summary>
	/// Gets or sets the cancellation token.
	/// </summary>
	public CancellationToken Aborted { get; set; }
}