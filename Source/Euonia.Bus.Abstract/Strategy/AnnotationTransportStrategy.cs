using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Evaluate whether a type can be handled by attribute decoration on the type.
/// </summary>
public class AnnotationTransportStrategy : ITransportStrategy
{
	/// <summary>
	/// The name of the strategy.
	/// </summary>
	public string Name { get; } = "Attribute decoration handle strategy";

	/// <summary>
	/// The required transport names for a type to be handled by this strategy.
	/// </summary>
	private IEnumerable<string> Required { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AnnotationTransportStrategy"/> class.
	/// </summary>
	/// <param name="requiredTransports"></param>
	public AnnotationTransportStrategy(IEnumerable<string> requiredTransports)
	{
		Required = requiredTransports;
	}

	/// <summary>
	/// Determines whether the specified message type can be dispatched by the transport.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	public bool Outgoing(Type messageType)
	{
		var attribute = messageType.GetCustomAttribute<DispatchInAttribute>();

		return attribute != null && Required.Intersect(attribute.Transports).Any();
	}

	/// <summary>
	/// Determines whether the specified message type can be received by the transport.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	public bool Incoming(Type messageType)
	{
		var attribute = messageType.GetCustomAttribute<ReceiveInAttribute>();

		return attribute != null && Required.Intersect(attribute.Transports).Any();
	}
}