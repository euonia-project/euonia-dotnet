using System.Reflection;

namespace Nerosoft.Euonia.Bus.RabbitMq;

/// <summary>
/// RabbitMQ 投递期计算的纯逻辑：目标队列名、订阅标识与消息优先级。
/// </summary>
/// <remarks>
/// 抽成独立的静态类型，一是让发布端与消费端共用**同一份**队列命名规则
/// （此前两端各自推导，回退值不同，导致未配置 <c>SubscriptionId</c> 时队列名不一致、
/// 发布端的队列存在性检查直接 404），二是让这些计算可以被单元测试覆盖，
/// 而不必依赖真实 broker。
/// </remarks>
internal static class RabbitMqDelivery
{
	/// <summary>
	/// RabbitMQ 支持的优先级上限。
	/// </summary>
	/// <remarks>
	/// 代理允许 1-255，但官方建议使用小范围（习惯上 1-10）；
	/// 优先级越高，broker 为每个优先级维护独立队列的额外开销越大，因此这里限定为 0-9。
	/// </remarks>
	public const int MaxSupportedPriority = 9;

	/// <summary>
	/// 依据配置与通道推导订阅标识。
	/// </summary>
	/// <param name="options">RabbitMQ 总线选项。</param>
	/// <param name="channel">通道名称。</param>
	/// <returns>订阅标识。</returns>
	/// <remarks>
	/// 回退链：显式配置的 <see cref="RabbitMqBusOptions.SubscriptionId"/> →
	/// 入口程序集的**简单名称** → 通道名称。
	/// <para>
	/// 必须使用简单名称（<c>GetName().Name</c>）而不是 <c>FullName</c>：
	/// 后者形如 <c>MyApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</c>，
	/// 与消费端推导出的队列名不一致。此前发布端使用 <c>FullName</c>，两端因此对不上。
	/// </para>
	/// </remarks>
	public static string ResolveSubscriptionId(RabbitMqBusOptions options, string channel)
	{
		return string.Collapse(options.SubscriptionId, Assembly.GetEntryAssembly()?.GetName().Name, channel);
	}

	/// <summary>
	/// 解析消息的目标队列名称。
	/// </summary>
	/// <param name="options">RabbitMQ 总线选项。</param>
	/// <param name="channel">通道名称。</param>
	/// <param name="queueOverride">消息级的目标队列覆盖值；为空时按通道推导。</param>
	/// <returns>目标队列名称。</returns>
	public static string ResolveQueueName(RabbitMqBusOptions options, string channel, string queueOverride = null)
	{
		if (!string.IsNullOrWhiteSpace(queueOverride))
		{
			return queueOverride;
		}

		return $"{channel}@{ResolveSubscriptionId(options, channel)}";
	}

	/// <summary>
	/// 把消息级优先级收敛为 RabbitMQ 可用的字节优先级。
	/// </summary>
	/// <param name="priority">消息声明的优先级；为 <c>null</c> 表示未声明。</param>
	/// <param name="maxPriority">队列上配置的最大优先级；小于等于 0 表示未启用优先级。</param>
	/// <returns>
	/// 应写入消息属性的优先级；未启用优先级或未声明优先级时返回 <c>null</c>（表示不设置该属性）。
	/// </returns>
	/// <remarks>
	/// 未启用优先级的队列会静默忽略该属性，因此这里在未启用时直接返回 <c>null</c>，
	/// 避免发出会被代理丢弃的信息。声明值会被收敛到 <c>[0, min(maxPriority, 9)]</c>。
	/// </remarks>
	public static byte? ResolvePriority(int? priority, int maxPriority)
	{
		if (priority is not > 0 || maxPriority <= 0)
		{
			return null;
		}

		var upperBound = Math.Min(maxPriority, MaxSupportedPriority);
		return (byte)Math.Clamp(priority.Value, 0, upperBound);
	}

	/// <summary>
	/// 构建队列声明参数，在启用优先级时附加 <c>x-max-priority</c>。
	/// </summary>
	/// <param name="maxPriority">队列上配置的最大优先级；小于等于 0 表示不启用。</param>
	/// <param name="additional">需要一并附加的其他参数（例如死信配置）；可为 <c>null</c>。</param>
	/// <returns>队列声明参数；无任何参数时返回 <c>null</c>。</returns>
	public static IDictionary<string, object> BuildQueueArguments(int maxPriority, IDictionary<string, object> additional = null)
	{
		Dictionary<string, object> arguments = additional == null ? [] : new Dictionary<string, object>(additional);

		if (maxPriority > 0)
		{
			arguments["x-max-priority"] = Math.Min(maxPriority, MaxSupportedPriority);
		}

		return arguments.Count == 0 ? null : arguments;
	}
}
