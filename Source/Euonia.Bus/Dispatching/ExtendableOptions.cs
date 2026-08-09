namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 可扩展的消息选项基类，提供发送消息时常用的配置属性。
/// </summary>
public abstract class ExtendableOptions
{
	/// <summary>
	/// 获取或设置用户自定义的消息标识符。
	/// </summary>
	/// <remarks>
	/// 设置后将替换原始消息标识符。
	/// </remarks>
	public virtual string MessageId { get; set; } = ObjectId.NewGuid(GuidType.SequentialAsString).ToString("N");

	/// <summary>
	/// 获取或设置指定的消息通道。
	/// </summary>
	public virtual string Channel { get; set; }

	/// <summary>
	/// 获取或设置队列名称。
	/// </summary>
	/// <remarks>
	/// 队列名称用于标识消息将发送到的目标队列。
	/// 设置后消息将被放入该队列。
	/// </remarks>
	public virtual string Queue { get; set; }

	/// <summary>
	/// 获取或设置队列优先级。
	/// </summary>
	public virtual int Priority { get; set; }

	/// <summary>
	/// 获取或设置请求追踪标识符。
	/// </summary>
	public virtual string RequestTraceId { get; set; }

	/// <summary>
	/// 获取或设置消息处理的延迟时间（毫秒）。
	/// </summary>
	public virtual long Delay { get; set; }

	/// <summary>
	/// 获取或设置消息处理的超时时间（毫秒）。
	/// </summary>
	public virtual long Timeout { get; set; }

	/// <summary>
	/// 获取或设置用于自定义消息元数据的委托。
	/// </summary>
	public virtual Action<MessageMetadata> MetadataSetter { get; set; }
}