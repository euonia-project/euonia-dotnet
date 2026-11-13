namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 用于配置内存总线传输的选项。
/// </summary>
public class InMemoryBusOptions
{
	/// <summary>
	/// 获取或设置传输名称。
	/// </summary>
	public string Name { get; set; } = Constants.DefaultTransportName;

	/// <summary>
	/// 获取或设置一个值，指示是否延迟初始化内存消息信使。默认值为 <see langword="true"/>。
	/// </summary>
	/// <remarks>当前实现未读取该选项。</remarks>
	public bool LazyInitialize { get; set; } = true;

	/// <summary>
	/// 获取或设置最大并发调用数。
	/// </summary>
	public int MaxConcurrentCalls { get; set; } = 1;

	/// <summary>
	/// 获取或设置一个值，指示是否为每个消息通道创建独立的订阅者。
	/// </summary>
	/// <value>
	/// 如果为每个消息通道创建独立的订阅者，则为 <c>true</c>；否则为 <c>false</c>。默认值为 <c>false</c>。
	/// </value>
	public bool MultipleSubscriberInstance { get; set; }
}