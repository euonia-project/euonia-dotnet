namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 死信（Dead Letter）记录，表示一条重试次数耗尽、需要人工介入的消息。
/// </summary>
/// <remarks>
/// 当发件箱的某个传输通道或收件箱的某个处理程序在 <c>MaxRetryAttempts</c> 次重试后仍然失败时，
/// 该消息会被移入死信存储：它不再被后台调度器扫描重试，可通过
/// <see cref="IDeadLetterService"/> 查询、重放或丢弃。
/// </remarks>
public class DeadLetterEntry
{
	/// <summary>
	/// 获取或设置消息的标识符。
	/// </summary>
	public string MessageId { get; set; }

	/// <summary>
	/// 获取或设置消息的通道名称。
	/// </summary>
	public string Channel { get; set; }

	/// <summary>
	/// 获取或设置消息负载的完整类型名称。
	/// </summary>
	public string MessageType { get; set; }

	/// <summary>
	/// 获取或设置消息内容（信封），重放时需要用到。
	/// </summary>
	public IMessageEnvelope Content { get; set; }

	/// <summary>
	/// 获取或设置死信的来源（发件箱或收件箱）。
	/// </summary>
	public DeadLetterSource Source { get; set; }

	/// <summary>
	/// 获取或设置死信的目标名称。
	/// </summary>
	/// <remarks>
	/// 发件箱来源时为目标传输器名称；收件箱来源时为目标处理程序类型名称。
	/// </remarks>
	public string Target { get; set; }

	/// <summary>
	/// 获取或设置导致转入死信的最后一次错误信息。
	/// </summary>
	public string Error { get; set; }

	/// <summary>
	/// 获取或设置转入死信时已累计的重试次数。
	/// </summary>
	public int RetryAttempts { get; set; }

	/// <summary>
	/// 获取或设置转入死信的时间（本地时间，与 <see cref="OutboxEntry.CreatedAt"/> 保持一致的时区约定）。
	/// </summary>
	public DateTime DeadLetteredAt { get; set; } = DateTime.Now;

	/// <summary>
	/// 返回当前实例的字符串表示形式。
	/// </summary>
	/// <returns>包含消息标识符、来源与目标的字符串。</returns>
	public override string ToString()
	{
		return $"DeadLetterEntry{{messageId={MessageId}, source={Source}, target={Target}, channel={Channel}}}";
	}
}

/// <summary>
/// 死信来源枚举。
/// </summary>
public enum DeadLetterSource
{
	/// <summary>
	/// 来自发件箱：某条发布消息在某个传输通道上投递失败且重试次数耗尽。
	/// </summary>
	Outbox = 0,

	/// <summary>
	/// 来自收件箱：某条已接收消息在某个处理程序上执行失败且重试次数耗尽。
	/// </summary>
	Inbox = 1,
}
