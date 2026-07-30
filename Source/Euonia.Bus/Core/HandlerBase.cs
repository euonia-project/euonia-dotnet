namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The abstract implement of <see cref="IHandler{TMessage}" />.
/// Implements the <see cref="IHandler{TMessage}" />
/// </summary>
/// <typeparam name="TMessage">The type of the message to be handled.</typeparam>
/// <seealso cref="IHandler{TMessage}" />
public abstract class HandlerBase<TMessage> : IHandler<TMessage>
	where TMessage : class
{
	/// <summary>
	/// Determines whether this instance can handle the specified message type.
	/// </summary>
	/// <param name="messageType">Type of the message.</param>
	/// <returns><c>true</c> if this instance can handle the specified message type; otherwise, <c>false</c>.</returns>
	public virtual bool CanHandle(Type messageType)
	{
		return typeof(TMessage) == messageType;
	}

	Task IHandler<TMessage>.HandleAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken)
	{
		return HandleAsync(message, context, cancellationToken);
	}

	/// <summary>
	/// Handles the asynchronous.
	/// </summary>
	/// <param name="message">The message.</param>
	/// <param name="messageContext">The message context.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	public abstract Task<Unit> HandleAsync(TMessage message, IMessageContext messageContext, CancellationToken cancellationToken = default);
}