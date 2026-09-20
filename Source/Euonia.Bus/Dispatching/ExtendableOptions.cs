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
	/// <remarks>
	/// 请求-响应调用（<see cref="IBus"/> 的 <c>CallAsync</c> 系列方法）会应用该值：
	/// 限时内未完成则抛出 <see cref="TimeoutException"/>。小于等于 0 表示不启用超时。
	/// 发送与发布目前不消费此值。
	/// </remarks>
	public virtual long Timeout { get; set; }

	/// <summary>
	/// 获取或设置用于自定义消息元数据的委托。
	/// </summary>
	public virtual Action<MessageMetadata> MetadataSetter { get; set; }

	/// <summary>
	/// 获取或设置是否为当前消息启用发件箱（Outbox）记录。
	/// </summary>
	/// <remarks>
	/// 单条消息级别的开关，优先级高于全局开关 <see cref="OutboxOptions.Enabled"/>：
	/// <list type="bullet">
	/// <item><description><c>true</c>：强制为当前消息启用发件箱；</description></item>
	/// <item><description><c>false</c>：强制跳过发件箱；</description></item>
	/// <item><description><c>null</c>（默认）：遵循全局开关。</description></item>
	/// </list>
	/// </remarks>
	public virtual bool? UseOutbox { get; set; }

	/// <summary>
	/// 获取或设置是否为当前消息启用收件箱（Inbox）去重记录。
	/// </summary>
	/// <remarks>
	/// 收件箱去重发生在消费端（接收方），由全局开关 <see cref="InboxOptions.Enabled"/> 与
	/// 收件箱存储的注册情况决定。该属性仅为发送侧的单条消息开关，供能够将选项
	/// 透传至消费端的传输器使用；内置传输器目前不消费该值。
	/// </remarks>
	public virtual bool? UseInbox { get; set; }
}