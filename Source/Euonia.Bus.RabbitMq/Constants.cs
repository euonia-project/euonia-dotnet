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
	/// 默认的死信队列（DLX）路由键。
	/// </summary>
	public const string DefaultDlxRoutingKey = "$nerosoft.euonia.dead-letter";
}