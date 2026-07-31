namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Configuration options for the message bus.
/// </summary>
public class MessageBusOptions
{
	/// <summary>
	/// Gets the name of the default transport that will be used when no specific transport is assigned to a message type by strategy.
	/// </summary>
	/// <value>
	/// The default transport name.
	/// </value>
	public string DefaultTransporter { get; set; }
}