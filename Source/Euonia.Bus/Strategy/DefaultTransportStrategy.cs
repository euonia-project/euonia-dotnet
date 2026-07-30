namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents the default implementation of the <see cref="ITransportStrategy"/> interface.
/// This strategy does not allow any message types for outgoing or incoming operations.
/// </summary>
internal class DefaultTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// Gets the name of the transport strategy.
	/// </summary>
	public string Name { get; } = "Default Transport Strategy";

	/// <summary>
	/// Determines whether the specified message type is allowed for outgoing operations.
	/// This implementation always returns <c>false</c>.
	/// </summary>
	/// <param name="messageType">The type of the message to check.</param>
	/// <returns><c>false</c>, indicating that outgoing operations are not allowed.</returns>
	public bool Outgoing(Type messageType)
	{
		return false;
	}

	/// <summary>
	/// Determines whether the specified message type is allowed for incoming operations.
	/// This implementation always returns <c>false</c>.
	/// </summary>
	/// <param name="messageType">The type of the message to check.</param>
	/// <returns><c>false</c>, indicating that incoming operations are not allowed.</returns>
	public bool Incoming(Type messageType)
	{
		return false;
	}
}