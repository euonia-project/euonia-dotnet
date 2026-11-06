namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 指定消息信使跟踪已注册接收方时使用的引用类型。
/// </summary>
public enum MessengerReferenceType
{
	/// <summary>
	/// 使用强引用跟踪接收方，接收方不会被自动回收，需要显式注销。
	/// </summary>
	StrongReference,

	/// <summary>
	/// 使用弱引用跟踪接收方，接收方可被垃圾回收自动清理。
	/// </summary>
	WeakReference
}