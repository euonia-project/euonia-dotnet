namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息总线相关的常量定义。
/// </summary>
internal static class Constants
{
	/// <summary>
	/// 最小序列值。
	/// </summary>
	public const long MinimalSequence = -1L;

	/// <summary>
	/// 最大序列值。
	/// </summary>
	public const long MaximumSequence = long.MaxValue;

	/// <summary>
	/// 消息总线的配置节点路径。
	/// </summary>
	public const string ConfigurationSection = "Euonia:Bus";

	/// <summary>
	/// 死信队列的传输器名称。
	/// </summary>
	public const string DeadLetterTransport = "DeadLetter";

	/// <summary>
	/// 默认传输器的配置节点路径。
	/// </summary>
	public const string DefaultTransporterSection = $"{ConfigurationSection}:DefaultTransporter";

	/// <summary>
	/// 自动加载程序集的配置节点路径。
	/// </summary>
	public const string AutoLoadAssembliesSection = $"{ConfigurationSection}:AutoLoadAssemblies";
}