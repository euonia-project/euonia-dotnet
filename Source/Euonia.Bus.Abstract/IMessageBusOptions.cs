namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Configuration options for the message bus.
/// </summary>
public interface IMessageBusOptions
{
	/// <summary>
	/// Gets the name of the default transport that will be used when no specific transport is assigned to a message type by strategy.
	/// </summary>
	/// <value>
	/// The default transport name.
	/// </value>
	string DefaultTransport { get; }

	/// <summary>
	/// Gets a value indicates whether to enable pipeline behaviors for message processing.
	/// </summary>
	bool EnablePipelineBehaviors { get; }

	/// <summary>
	/// Gets the message convention used for naming and discovering messages and handlers.
	/// </summary>
	/// <value>
	///	Convention provider that defines message type classifications (multicast, unicast, request).
	/// </value>
	IMessageConvention Convention { get; }

	/// <summary>
	/// Gets the list of types for which a transport strategy has been assigned.
	/// </summary>
	IReadOnlyList<string> StrategyAssignedTypes { get; }

	/// <summary>
	/// Gets the transport strategy associated with the specified transport name.
	/// </summary>
	/// <param name="transport">The name of the transport for which the strategy is to be retrieved.</param>
	/// <returns>
	/// An instance of <see cref="ITransportStrategy"/> representing the strategy associated with the specified transport name.
	/// </returns>
	ITransportStrategy GetStrategy(string transport);
}