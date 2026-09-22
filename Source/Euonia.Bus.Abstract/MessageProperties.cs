namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息投递属性在 <see cref="IMessageEnvelope.Metadata"/> 中的键，以及对应的读写辅助方法。
/// </summary>
/// <remarks>
/// <c>ExtendableOptions.Queue</c> 与 <c>ExtendableOptions.Priority</c>
/// 是**投递期**属性（由发送端的传输器消费），与消息负载本身无关。
/// 把它们放在 <see cref="IMessageEnvelope.Metadata"/> 中而不是扩展 <see cref="IMessageEnvelope"/> 接口，
/// 一方面不破坏既有实现者，另一方面它们会随消息序列化一并传递，
/// 使转发/桥接场景也能保留投递意图。
/// <para>
/// 并非所有传输器都消费全部属性：不理解某个属性的传输器应当忽略它，而不是报错。
/// </para>
/// </remarks>
public static class MessageProperties
{
	/// <summary>
	/// 目标队列名称的元数据键。
	/// </summary>
	/// <remarks>
	/// 仅对以「队列」为投递目标的传输器（如 RabbitMQ 的 Send/Call、ActiveMQ）有意义；
	/// 以交换机/主题为目标的发布（如 RabbitMQ 的 Publish）会忽略它。
	/// </remarks>
	public const string QueueKey = "$nerosoft.euonia:message.queue";

	/// <summary>
	/// 消息优先级的元数据键。
	/// </summary>
	/// <remarks>
	/// 取值语义由传输器决定：RabbitMQ 使用 0-9 的字节优先级，ActiveMQ 使用 0-9 的优先级。
	/// 目标队列/代理未启用优先级支持时，该值会被静默忽略。
	/// </remarks>
	public const string PriorityKey = "$nerosoft.euonia:message.priority";

	/// <summary>
	/// 读取消息的目标队列名称。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <returns>目标队列名称；未设置时返回 <c>null</c>。</returns>
	public static string GetQueue(this IMessageEnvelope message)
	{
		return message?.Metadata?[QueueKey] as string;
	}

	/// <summary>
	/// 读取消息的优先级。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <returns>优先级；未设置或无法解析时返回 <c>null</c>。</returns>
	public static int? GetPriority(this IMessageEnvelope message)
	{
		var value = message?.Metadata?[PriorityKey];

		return value switch
		{
			null => null,
			int priority => priority,
			long priority => (int)priority,
			_ => int.TryParse(value.ToString(), out var parsed) ? parsed : null,
		};
	}

	/// <summary>
	/// 设置消息的目标队列名称。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <param name="queue">目标队列名称。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="message"/> 为 <c>null</c> 时抛出。</exception>
	public static void SetQueue(this IMessageEnvelope message, string queue)
	{
		ArgumentNullException.ThrowIfNull(message);
		message.Metadata[QueueKey] = queue;
	}

	/// <summary>
	/// 设置消息的优先级。
	/// </summary>
	/// <param name="message">消息信封。</param>
	/// <param name="priority">优先级。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="message"/> 为 <c>null</c> 时抛出。</exception>
	public static void SetPriority(this IMessageEnvelope message, int priority)
	{
		ArgumentNullException.ThrowIfNull(message);
		message.Metadata[PriorityKey] = priority;
	}
}
