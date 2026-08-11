namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务上下文访问器。
/// </summary>
/// <remarks>
/// 使用 <see cref="AsyncLocal{T}"/> 在当前异步控制流中传播服务提供程序：
/// 派生任务（<see cref="System.Threading.Tasks.Task"/>.Run、线程池线程、async/await 边界）通过 ExecutionContext
/// 自动继承当前作用域的服务提供程序，实现跨异步边界的上下文流动。不同异步流之间相互隔离，
/// 不会发生跨请求/跨任务的上下文泄漏。
/// <para>
/// 为避免在隔离 ExecutionContext 的宿主（如某些测试框架）中丢失上下文，实例方法
/// <see cref="ServiceProvider"/> 在异步流未建立时回退到本作用域解析时捕获的实例 provider。
/// </para>
/// </remarks>
public class BusinessContextAccessor
{
	/// <summary>
	/// 存储当前异步流作用域的服务提供程序。
	/// </summary>
	private static readonly AsyncLocal<IServiceProvider> _currentServiceProvider = new();

	/// <summary>
	/// 本作用域解析时捕获的服务提供程序，作为异步流未建立时的兜底。
	/// </summary>
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 初始化 <see cref="BusinessContextAccessor"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于提供服务解析的服务提供程序。</param>
	/// <remarks>
	/// 以 Scoped 注册时，本作用域首次解析访问器即把该作用域的 provider 写入当前异步流，
	/// 使同一调用链上的派生任务/线程共享当前上下文。
	/// </remarks>
	public BusinessContextAccessor(IServiceProvider provider)
	{
		_provider = provider;
		_currentServiceProvider.Value = provider;
	}

	/// <summary>
	/// 获取当前异步流中的服务提供程序；未建立上下文时为 <c>null</c>。
	/// </summary>
	public static IServiceProvider Current => _currentServiceProvider.Value;

	/// <summary>
	/// 获取当前异步流中的服务提供程序；异步流未建立时回退到本作用域的实例 provider。
	/// </summary>
	internal IServiceProvider ServiceProvider => _currentServiceProvider.Value ?? _provider;

	/// <summary>
	/// 显式建立/替换当前异步流的服务提供程序。
	/// 适用于后台任务等无法通过 DI 解析作用域的场景（手动创建作用域后调用）。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	public static void SetCurrent(IServiceProvider provider)
	{
		_currentServiceProvider.Value = provider;
	}

	/// <summary>
	/// 清除当前异步流的服务提供程序。
	/// </summary>
	public static void Clear()
	{
		_currentServiceProvider.Value = null;
	}
}