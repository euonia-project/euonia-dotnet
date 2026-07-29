namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Configuration options for the message bus.
/// </summary>
public class MessageBusOptions : IMessageBusOptions
{
	private readonly BusConfigurator _configurator = Singleton<BusConfigurator>.Instance;

	/// <summary>
	/// Gets the name of the default transport that will be used when no specific transport is assigned to a message type by strategy.
	/// </summary>
	/// <value>
	/// The default transport name.
	/// </value>
	public string DefaultTransport { get; set; }

	/// <summary>
	/// Indicates whether to enable pipeline behaviors for message processing.
	/// </summary>
	public bool EnablePipelineBehaviors { get; set; } = true;
	
	/// <summary>
	/// Gets the message convention used for naming and discovering messages and handlers.
	/// </summary>
	public IMessageConvention Convention => _configurator.ConventionBuilder.Convention;

	/// <summary>
	/// Gets the list of types for which a transport strategy has been assigned.
	/// </summary>
	public IReadOnlyList<string> StrategyAssignedTypes => _configurator.StrategyBuilders.Keys.ToList();

	/// <summary>
	/// Gets the transport strategy associated with the specified transport name.
	/// </summary>
	/// <param name="transport">The name of the transport for which the strategy is to be retrieved.</param>
	/// <returns>
	/// An instance of <see cref="ITransportStrategy"/> representing the strategy associated with the specified transport name.
	/// </returns>
	public ITransportStrategy GetStrategy(string transport)
	{
		return _configurator.StrategyBuilders.GetOrDefault(transport)?.Strategy;
	}
}