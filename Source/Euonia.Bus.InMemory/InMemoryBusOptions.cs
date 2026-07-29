namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// Options for configuring the In-Memory Bus transport.
/// </summary>
public class InMemoryBusOptions
{
	/// <summary>
	/// Gets or sets the transport name.
	/// </summary>
	public string Name { get; set; } = Constants.DefaultTransportName;

	/// <summary>
	/// 
	/// </summary>
	public bool LazyInitialize { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum concurrent calls.
	/// </summary>
	public int MaxConcurrentCalls { get; set; } = 1;

	/// <summary>
	/// Gets or sets a value indicating whether the subscriber should create for each message channel.
	/// </summary>
	/// <value>
	/// <c>true</c> if the subscriber should create for each message channel; otherwise, <c>false</c>. default is <c>false</c>.
	/// </value>
	public bool MultipleSubscriberInstance { get; set; }
}