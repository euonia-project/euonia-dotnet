namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 命令执行状态的定义。
/// </summary>
public enum CommandStatus
{
	/// <summary>
	/// 表示命令已成功执行。
	/// </summary>
	Succeed,

	/// <summary>
	/// 表示命令执行失败。
	/// </summary>
	Failure,

	/// <summary>
	/// 表示命令执行被取消。
	/// </summary>
	Canceled,
}