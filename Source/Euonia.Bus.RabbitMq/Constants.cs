namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 消息总线相关的常量定义。
/// </summary>
internal static class Constants
{
	/// <summary>
	/// 默认传输器名称。
	/// </summary>
	public const string DefaultTransportName = "RabbitMq";

	/// <summary>
	/// RabbitMQ 消息总线的配置节点路径。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Bus:RabbitMq";

	/// <summary>
	/// 默认交换机名称前缀。
	/// </summary>
	public const string DefaultExchangeNamePrefix = "$nerosoft.euonia.exchange";

	/// <summary>
	/// 默认队列名称前缀。
	/// </summary>
	public const string DefaultQueueNamePrefix = "$nerosoft.euonia.queue";

	/// <summary>
	/// 默认主题名称。
	/// </summary>
	public const string DefaultTopicName = "$nerosoft.euonia.topic";

	/// <summary>
	/// 默认的死信队列（DLX）路由键。
	/// </summary>
	public const string DefaultDlxRoutingKey = "$nerosoft.euonia.dead-letter";
}