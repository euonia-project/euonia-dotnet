namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息总线的配置选项。
/// </summary>
public class MessageBusOptions
{
	/// <summary>
	/// 获取或设置默认传输器的名称，可用于消息路由和分类。
	/// </summary>
	/// <remarks>
	/// 当消息类型未通过传输策略分配特定传输器时，将使用此默认传输器。
	/// <para>此选项通过<c>Euonia:Bus:DefaultTransporter</c>节点进行配置。</para>
	/// </remarks>
	/// <value>默认传输器的名称。</value>
	public string DefaultTransporter { get; set; }

	/// <summary>
	/// 获取或设置需要自动加载的程序集名称列表。
	/// </summary>
	/// <remarks>
	///	自动加载程序集的名称列表，用于在应用程序启动时扫描并注册消息处理器和传输器。
	/// </remarks>
	public string[] AutoLoadAssemblies { get; set; }
}