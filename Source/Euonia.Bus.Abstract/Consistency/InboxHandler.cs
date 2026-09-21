namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 收件箱（Inbox）消息在单个处理程序上的执行状态记录。
/// </summary>
/// <remarks>
/// 每个由 <see cref="InboxEntry"/> 记录的已接收消息都会为涉及的每个处理程序维护一个 <see cref="InboxHandler"/> 实例，
/// 用于追踪其执行状态与重试次数。
/// </remarks>
public class InboxHandler
{
	/// <summary>
	/// 获取或设置关联消息的标识符。
	/// </summary>
	public string MessageId { get; set; }

	/// <summary>
	/// 获取或设置处理程序的类型名称。
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// 获取或设置当前执行状态。
	/// </summary>
	public InboxHandlerStatus Status { get; set; } = InboxHandlerStatus.Pending;

	/// <summary>
	/// 获取或设置已重试的次数。
	/// </summary>
	public int RetryAttempts { get; set; }

	/// <summary>
	/// 获取或设置最后一次执行失败时的错误信息。
	/// </summary>
	public string Error { get; set; }

	/// <summary>
	/// 将当前状态标记为处理成功，并清除错误信息。
	/// </summary>
	public void MarkAsSuccess()
	{
		Status = InboxHandlerStatus.Success;
		Error = null;
	}

	/// <summary>
	/// 将当前状态标记为处理失败。
	/// </summary>
	/// <param name="error">失败原因的描述信息。</param>
	public void MarkAsFailed(string error)
	{
		Status = InboxHandlerStatus.Failed;
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
		Status = InboxHandlerStatus.DeadLettered;
		Error = error;
	}

	/// <summary>
	/// 返回当前状态的字符串表示形式。
	/// </summary>
	/// <returns>包含消息标识符、处理程序名称与状态的字符串。</returns>
	public override string ToString()
	{
		return $"InboxHandler{{messageId={MessageId}, handler={Name}, status={Status}}}";
	}
}

/// <summary>
/// 收件箱（Inbox）处理状态枚举。
/// </summary>
public enum InboxHandlerStatus
{
	/// <summary>
	/// 待处理。
	/// </summary>
	Pending = 0,

	/// <summary>
	/// 处理成功。
	/// </summary>
	Success = 1,

	/// <summary>
	/// 处理失败。
	/// </summary>
	Failed = 2,

	/// <summary>
	/// 重试次数耗尽，已转入死信队列。
	/// </summary>
	DeadLettered = 3,
}