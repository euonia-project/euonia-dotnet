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
	/// 获取或设置目标队列名称，覆盖传输器依据通道推导出的默认队列。
	/// </summary>
	/// <remarks>
	/// 仅对以「队列」为投递目标的传输器生效：
	/// <list type="bullet">
	/// <item><description>RabbitMQ：<c>SendAsync</c>/<c>CallAsync</c> 的目标队列（<c>PublishAsync</c> 走交换机，忽略此值）；</description></item>
	/// <item><description>ActiveMQ：<c>SendAsync</c>/<c>CallAsync</c> 的目标队列；</description></item>
	/// <item><description>InMemory / HTTP / gRPC：无队列概念，忽略此值。</description></item>
	/// </list>
	/// <para>
	/// 该值会随消息写入元数据（见 <see cref="MessageProperties.QueueKey"/>），
	/// 因此可被转发/桥接场景保留。由于消费端是按通道注册的，
	/// 只有当目标队列上确实有消费者时消息才会被处理。
	/// </para>
	/// </remarks>
	/// <value>目标队列名称；为 <c>null</c> 或空白时使用传输器推导的默认队列。</value>
	public virtual string Queue { get; set; }

	/// <summary>
	/// 获取或设置消息优先级。
	/// </summary>
	/// <remarks>
	/// 取值语义由传输器与代理共同决定，超出范围的值会被传输器收敛到其支持的区间：
	/// <list type="bullet">
	/// <item><description>RabbitMQ：0-9 的字节优先级。**目标队列必须以 <c>x-max-priority</c> 声明**该优先级才会被代理采纳，否则静默忽略；</description></item>
	/// <item><description>ActiveMQ：0-9 的优先级；</description></item>
	/// <item><description>InMemory / HTTP / gRPC：无优先级概念，忽略此值。</description></item>
	/// </list>
	/// <para>
	/// 该值会随消息写入元数据（见 <see cref="MessageProperties.PriorityKey"/>）。
	/// 小于等于 0 表示不设置优先级。
	/// </para>
	/// </remarks>
	/// <value>消息优先级；小于等于 0 时不设置。</value>
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
	/// 收件箱去重发生在消费端（接收方），实际行为只由全局开关 <see cref="InboxOptions.Enabled"/>
	/// 与收件箱存储的注册情况决定。该属性仅为发送侧的单条消息标记，供能够把选项透传至
	/// 消费端的自定义传输器使用；内置传输器不消费该值，设置它对收件箱行为没有影响。
	/// </remarks>
	public virtual bool? UseInbox { get; set; }
}