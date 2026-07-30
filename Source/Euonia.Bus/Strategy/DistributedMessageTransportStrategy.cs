using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Represents a transport strategy that evaluates whether a type is marked as a distributed message.
/// </summary>
public class DistributedMessageTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// Gets the name of the transport strategy.
	/// </summary>
	public string Name => "Distributed message transport strategy";

	/// <summary>
	/// Determines whether the specified message type is allowed for outgoing operations.
	/// Checks if the type is marked with the <see cref="DistributedMessageAttribute"/>.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if the message type is marked with <see cref="DistributedMessageAttribute"/>; otherwise, <c>false</c>.</returns>
	public bool Outgoing(Type messageType)
	{
		return messageType.GetCustomAttribute<DistributedMessageAttribute>() != null;
	}

	/// <summary>
	/// Determines whether the specified message type is allowed for incoming operations.
	/// Checks if the type is marked with the <see cref="DistributedMessageAttribute"/>.
	/// </summary>
	/// <param name="messageType">The type of the message to evaluate.</param>
	/// <returns><c>true</c> if the message type is marked with <see cref="DistributedMessageAttribute"/>; otherwise, <c>false</c>.</returns>
	public bool Incoming(Type messageType)
	{
		return messageType.GetCustomAttribute<DistributedMessageAttribute>() != null;
	}
}