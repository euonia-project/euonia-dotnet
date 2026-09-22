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
	/// 获取或设置需要自动扫描并注册消息处理器的程序集名称列表。
	/// </summary>
	/// <remarks>
	/// 由 <see cref="ServiceActivator"/> 在应用启动时（用户配置委托之前）执行：
	/// 逐个加载程序集并扫描其中的处理器类型注册到对应通道，
	/// 因此配置后无需再手工调用 <c>RegisterChannel(assembly)</c>。
	/// <para>程序集按**简单名称**加载（例如 <c>MyApp.Handlers</c>）；无法加载时启动即失败并指出具体名称。</para>
	/// <para>通过 <c>Euonia:Bus:AutoLoadAssemblies</c> 节点配置。</para>
	/// </remarks>
	public string[] AutoLoadAssemblies { get; set; }

	/// <summary>
	/// 获取或设置发件箱（Outbox）模式的配置选项。
	/// </summary>
	/// <remarks>
	/// 通过 <c>Euonia:Bus:Outbox</c> 配置节点进行绑定。
	/// </remarks>
	public OutboxOptions Outbox { get; set; } = new();

	/// <summary>
	/// 获取或设置收件箱（Inbox）模式的配置选项。
	/// </summary>
	/// <remarks>
	/// 通过 <c>Euonia:Bus:Inbox</c> 配置节点进行绑定。
	/// </remarks>
	public InboxOptions Inbox { get; set; } = new();
}