namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// 基于 ActiveMQ 的消息总线选项定义。
/// </summary>
public class ActiveMqBusOptions
{
	/// <summary>
	/// 获取或设置一个值，指示该功能是否启用。
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// 获取或设置传输器名称。
	/// </summary>
	public string Name { get; set; } = Constants.DefaultTransportName;

	/// <summary>
	/// 获取或设置 ActiveMQ 连接字符串。
	/// </summary>
	/// <example>activemq:tcp://activemqhost:61616</example>
	public string Connection { get; set; }

	/// <summary>
	/// 获取或设置失败重试的最大次数。
	/// </summary>
	public int MaxFailureRetries { get; set; } = 3;

	/// <summary>
	/// 获取或设置订阅标识符。
	/// </summary>
	public string SubscriptionId { get; set; }
	
	/// <summary>
	/// 获取或设置序列化器提供程序名称。
	/// 默认使用 System.Text.Json：相较 Newtonsoft.Json，反序列化更快、分配更低。
	/// 需要 Newtonsoft 专有特性时可显式设为 <c>"NewtonsoftJson"</c>。
	/// </summary>
	public string SerializerProvider { get; set; } = "SystemTestJson";
}