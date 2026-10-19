namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 为一个内存总线容器提供独立的消息信使实例。
/// </summary>
/// <remarks>
/// 不能使用进程级的 <see cref="WeakReferenceMessenger.Default"/> / <see cref="StrongReferenceMessenger.Default"/>：
/// 同一进程内多个服务容器或命名传输器会把同一条消息投递给彼此不相关的接收者，
/// 单播/请求场景还会重复执行处理程序。每个容器通过依赖注入共享一对实例即可保持隔离。
/// </remarks>
public sealed class InMemoryMessengerProvider
{
	/// <summary>
	/// 获取用于多播（发布）的弱引用信使。
	/// </summary>
	public WeakReferenceMessenger Weak { get; } = new();

	/// <summary>
	/// 获取用于单播与请求-响应的强引用信使。
	/// </summary>
	public StrongReferenceMessenger Strong { get; } = new();
}
