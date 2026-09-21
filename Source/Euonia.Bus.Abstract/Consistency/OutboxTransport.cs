namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 发件箱（Outbox）消息在单个传输通道上的状态记录。
/// </summary>
/// <remarks>
/// 每个由 <see cref="OutboxEntry"/> 记录的消息都会为涉及的每个传输器维护一个 <see cref="OutboxTransport"/> 实例，
/// 用于追踪其在对应通道上的发送状态与重试次数。
/// </remarks>
public class OutboxTransport
{
	/// <summary>
	/// 获取或设置关联消息的标识符。
	/// </summary>
	public string MessageId { get; set; }

	/// <summary>
	/// 获取或设置传输器的名称。
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// 获取或设置当前传输状态。
	/// </summary>
	public OutboxTransportStatus Status { get; set; } = OutboxTransportStatus.Pending;

	/// <summary>
	/// 获取或设置已重试的次数。
	/// </summary>
	public int RetryAttempts { get; set; }

	/// <summary>
	/// 获取或设置最后一次发送失败时的错误信息。
	/// </summary>
	public string Error { get; set; }

	/// <summary>
	/// 将当前状态标记为发送成功，并清除失败信息。
	/// </summary>
	public void MarkAsSuccess()
	{
		Status = OutboxTransportStatus.Success;
		Error = null;
	}

	/// <summary>
	/// 将当前状态标记为发送失败。
	/// </summary>
	/// <param name="error">失败原因的描述信息。</param>
	public void MarkAsFailed(string error)
	{
		Status = OutboxTransportStatus.Failed;
		Error = error;
		RetryAttempts++;
	}

	/// <summary>
	/// 将当前状态标记为已转入死信队列。
	/// </summary>
	/// <remarks>
	/// 转入死信后该记录不再被后台调度器扫描重试；<see cref="RetryAttempts"/> 保留转入时的次数，
	/// 便于运维判断死信产生的原因。重放时需重置状态与重试次数。
	/// </remarks>
	/// <param name="error">导致转入死信的最后一次错误信息。</param>
	public void MarkAsDeadLettered(string error)
	{
		Status = OutboxTransportStatus.DeadLettered;
		Error = error;
	}

	/// <summary>
	/// 返回当前状态的字符串表示形式。
	/// </summary>
	/// <returns>包含消息标识符、传输器名称与状态的字符串。</returns>
	public override string ToString()
	{
		return $"OutboxTransport{{messageId={MessageId}, transport={Name}, status={Status}}}";
	}
}

/// <summary>
/// 发件箱（Outbox）传输状态枚举。
/// </summary>
public enum OutboxTransportStatus
{
	/// <summary>
	/// 待发送。
	/// </summary>
	Pending = 0,

	/// <summary>
	/// 发送成功。
	/// </summary>
	Success = 1,

	/// <summary>
	/// 发送失败。
	/// </summary>
	Failed = 2,

	/// <summary>
	/// 重试次数耗尽，已转入死信队列。
	/// </summary>
	DeadLettered = 3,
}