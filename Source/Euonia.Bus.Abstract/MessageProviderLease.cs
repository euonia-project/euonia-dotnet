namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息处理时服务提供程序的获取方式。
/// </summary>
public enum MessageProviderLease
{
	/// <summary>
	/// 为本次消息处理创建独立作用域。
	/// </summary>
	OwnScope = 0,

	/// <summary>
	/// 复用发布方显式绑定的作用域（仅限调用方等待处理完成的进程内单播/请求）。
	/// </summary>
	Borrow = 1,
}
