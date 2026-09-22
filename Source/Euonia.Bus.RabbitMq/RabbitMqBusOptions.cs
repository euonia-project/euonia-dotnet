namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// 基于 RabbitMQ 的消息总线选项定义。
/// </summary>
public class RabbitMqBusOptions
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
	/// 获取或设置 RabbitMQ 连接字符串。
	/// <example>amqp://user:password@host:port</example>
	/// </summary>
	public string Connection { get; set; }

	/// <summary>
	/// 获取或设置交换机名称前缀。
	/// </summary>
	public string ExchangeNamePrefix { get; set; } = Constants.DefaultExchangeNamePrefix;

	/// <summary>
	/// 获取或设置用于构建队列名称的前缀字符串。
	/// </summary>
	/// <remarks>
	/// 对于单播消息，队列名称将构建为 {QueueNamePrefix}:{MessageChannelName}@{SubscriptionId}。
	/// </remarks>
	public string QueueNamePrefix { get; set; } = Constants.DefaultQueueNamePrefix;

	/// <summary>
	/// 获取或设置路由键。
	/// </summary>
	public string RoutingKey { get; set; } = "*";

	/// <summary>
	/// 获取或设置一个值，指示消息是否应持久化。
	/// </summary>
	public bool Persistent { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示消息是否应自动确认。
	/// </summary>
	public bool AutoAck { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示消息是否应为强制性的。
	/// </summary>
	public bool Mandatory { get; set; } = true;

	/// <summary>
	/// 获取或设置失败重试的最大次数。
	/// </summary>
	public int MaxFailureRetries { get; set; } = 3;

	/// <summary>
	/// 获取或设置订阅标识符。
	/// </summary>
	public string SubscriptionId { get; set; }

	/// <summary>
	/// 获取或设置预取大小（单条消息的最大字节数，0 表示不限制）。
	/// </summary>
	public int PrefetchSize { get; set; } = 0;

	/// <summary>
	/// 获取或设置预取数量（RabbitMQ 服务器在确认前可发送的消息条数）。
	/// </summary>
	public int PrefetchCount { get; set; } = 1;

	/// <summary>
	/// 获取或设置一个值，指示是否启用死信队列功能。
	/// </summary>
	public bool IsDeadLetterEnabled { get; set; } = true;

	/// <summary>
	/// 获取或设置队列支持的最大消息优先级。
	/// </summary>
	/// <remarks>
	/// 小于等于 0 表示不启用优先级（默认）。启用后：
	/// <list type="bullet">
	/// <item><description>消费端声明队列时会附加 <c>x-max-priority</c> 参数；</description></item>
	/// <item><description>发送端会把 <c>ExtendableOptions.Priority</c> 写入消息属性，并收敛到 <c>[0, min(该值, 9)]</c>。</description></item>
	/// </list>
	/// <para>
	/// <b>注意</b>：RabbitMQ 只在队列以 <c>x-max-priority</c> 声明时才会采纳消息优先级；
	/// 若同名队列已以其他参数存在，该声明会被忽略（参数不一致不会报错）。
	/// 提高优先级会让 broker 为每个优先级维护独立队列，带来额外开销，建议保持较小取值范围。
	/// </para>
	/// </remarks>
	/// <value>最大优先级；默认 <c>0</c>（不启用）。</value>
	public int MaxPriority { get; set; }

	/// <summary>
	/// 获取或设置序列化器提供程序名称。
	/// </summary>
	public string SerializerProvider { get; set; } = "NewtonsoftJson";
}