namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// The RabbitMQ based message bus options definition.
/// </summary>
public class RabbitMqBusOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether the feature is enabled.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets the transport name.
	/// </summary>
	public string Name { get; set; } = "rabbitmq";

	/// <summary>
	/// Gets or sets the RabbitMQ connection string.
	/// <example>amqp://user:password@host:port</example>
	/// </summary>
	public string Connection { get; set; }

	/// <summary>
	/// Gets or sets the exchange name.
	/// </summary>
	public string ExchangeNamePrefix { get; set; } = Constants.DefaultExchangeNamePrefix;

	/// <summary>
	/// Gets or sets the prefix string to build queue names.
	/// </summary>
	/// <remarks>
	/// In case of unicast messages, the queue name will be built as {QueueNamePrefix}:{MessageChannelName}@{SubscriptionId}.
	/// </remarks>
	public string QueueNamePrefix { get; set; } = Constants.DefaultQueueNamePrefix;

	/// <summary>
	/// Gets or sets the routing key.
	/// </summary>
	public string RoutingKey { get; set; } = "*";

	/// <summary>
	/// Gets or sets a value indicating whether the message should be persistent.
	/// </summary>
	public bool Persistent { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether the message should be auto-acknowledged.
	/// </summary>
	public bool AutoAck { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether the message should be mandatory.
	/// </summary>
	public bool Mandatory { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of failure retries.
	/// </summary>
	public int MaxFailureRetries { get; set; } = 3;

	/// <summary>
	/// Gets or sets the subscription identifier.
	/// </summary>
	public string SubscriptionId { get; set; }
}