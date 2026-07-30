using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents a transport strategy that evaluates whether a type is marked as a local message.
/// </summary>
public class LocalMessageTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// Gets the name of the transport strategy.
	/// </summary>
	public string Name { get; } = "Local message transport strategy";

	/// <summary>
	/// Determines whether the specified message type is allowed for outgoing operations.
	/// Checks if the type is marked with the <see cref="LocalMessageAttribute"/>.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if the message type is marked with <see cref="LocalMessageAttribute"/>; otherwise, <c>false</c>.</returns>
	public bool Outgoing(Type messageType)
	{
		return messageType.GetCustomAttribute<LocalMessageAttribute>() != null;
	}

	/// <summary>
	/// Determines whether the specified message type is allowed for incoming operations.
	/// Checks if the type is marked with the <see cref="LocalMessageAttribute"/>.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if the message type is marked with <see cref="LocalMessageAttribute"/>; otherwise, <c>false</c>.</returns>
	public bool Incoming(Type messageType)
	{
		return messageType.GetCustomAttribute<LocalMessageAttribute>() != null;
	}
}