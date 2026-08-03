namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息总线的配置选项。
/// </summary>
public class MessageBusOptions
{
	/// <summary>
	/// 获取或设置默认传输器的名称。
	/// </summary>
	/// <remarks>
	/// 当消息类型未通过传输策略分配特定传输器时，将使用此默认传输器。
	/// </remarks>
	/// <value>默认传输器的名称。</value>
	public string DefaultTransporter { get; set; }
}