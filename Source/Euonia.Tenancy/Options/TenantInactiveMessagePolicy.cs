namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 停用/归档租户的存量消息处理策略。
/// </summary>
public enum TenantInactiveMessagePolicy
{
	/// <summary>
	/// 转入死信，等待运维处理。
	/// </summary>
	DeadLetter = 0,

	/// <summary>
	/// 跳过执行并正常确认，不产生死信。
	/// </summary>
	Skip = 1,

	/// <summary>
	/// 继续执行；默认只读，写入需显式开启。
	/// </summary>
	Process = 2,
}
