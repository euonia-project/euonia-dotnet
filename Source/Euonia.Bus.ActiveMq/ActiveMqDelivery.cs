using Apache.NMS;

namespace Nerosoft.Euonia.Bus.ActiveMq;

/// <summary>
/// ActiveMQ 投递期计算的纯逻辑：目标队列名与消息优先级。
/// </summary>
/// <remarks>
/// 抽成独立的静态类型以便单元测试覆盖，无需依赖真实 broker。
/// </remarks>
internal static class ActiveMqDelivery
{
	/// <summary>
	/// 解析消息的目标队列名称。
	/// </summary>
	/// <param name="channel">通道名称。</param>
	/// <param name="queueOverride">消息级的目标队列覆盖值；为空时使用通道名称。</param>
	/// <returns>目标队列名称。</returns>
	/// <remarks>
	/// ActiveMQ 的队列由通道名称直接决定，因此覆盖值可以完整替换它。
	/// 消费端按通道注册，只有当目标队列上确实有消费者时消息才会被处理。
	/// </remarks>
	public static string ResolveQueueName(string channel, string queueOverride = null)
	{
		return string.IsNullOrWhiteSpace(queueOverride) ? channel : queueOverride;
	}

	/// <summary>
	/// 把消息级优先级转换为 NMS 优先级。
	/// </summary>
	/// <param name="priority">消息声明的优先级；为 <c>null</c> 或不大于 0 时使用 <see cref="MsgPriority.Normal"/>。</param>
	/// <returns>NMS 优先级（0-9）。</returns>
	/// <remarks>
	/// <see cref="MsgPriority"/> 的取值即为 0-9（<c>Lowest</c> → <c>Highest</c>），
	/// 因此直接按数值转换并把越界值收敛到该区间。
	/// <para>
	/// 与 RabbitMQ 不同，ActiveMQ 客户端无需在目标上声明优先级上限；
	/// 是否真正生效由 broker 的按目的地策略（per-destination policy）决定，
	/// 未启用时该值会被忽略。
	/// </para>
	/// </remarks>
	public static MsgPriority ResolvePriority(int? priority)
	{
		if (priority is not > 0)
		{
			return MsgPriority.Normal;
		}

		return (MsgPriority)Math.Clamp(priority.Value, 0, 9);
	}
}
