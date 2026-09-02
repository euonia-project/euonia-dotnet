namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 常量定义。
/// </summary>
internal class Constants
{
	/// <summary>
	/// 获取默认的传输器名称。
	/// </summary>
	public const string DefaultTransportName = "ActiveMq";
	
	/// <summary>
	/// ActiveMQ 消息总线的配置节点路径。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Bus:ActiveMq";
}