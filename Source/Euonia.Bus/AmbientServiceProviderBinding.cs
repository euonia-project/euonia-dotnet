using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 将 <see cref="IServiceAccessor"/> 绑定到消息处理或后台重试专用的服务作用域，并在释放时恢复先前的绑定。
/// </summary>
/// <remarks>
/// 异步派生流会继承发布方的 <see cref="IServiceAccessor"/> 值；当发布方作用域已释放时（如 HTTP 请求结束后
/// 才执行的内存总线后台泵），处理程序内经由访问器的服务解析会抛出 <see cref="ObjectDisposedException"/>。
/// 绑定仅在当前异步执行流内生效；释放后恢复原先显式设置的值，未显式设置时恢复为未设置状态。
/// </remarks>
internal sealed class AmbientServiceProviderBinding : IDisposable
{
	private readonly IServiceAccessor _accessor;
	private readonly IServiceProvider _inherited;
	private bool _disposed;

	private AmbientServiceProviderBinding(IServiceAccessor accessor, IServiceProvider inherited)
	{
		_accessor = accessor;
		_inherited = inherited;
	}

	/// <summary>
	/// 将访问器绑定到 <paramref name="scope"/> 的服务提供程序，访问器经 <paramref name="provider"/> 解析。
	/// </summary>
	/// <param name="provider">用于解析 <see cref="IServiceAccessor"/> 的服务提供程序；未注册访问器时绑定为空操作。</param>
	/// <param name="scope">本次处理专用的作用域。</param>
	/// <returns>释放时恢复先前绑定的对象。</returns>
	public static AmbientServiceProviderBinding Bind(IServiceProvider provider, IServiceScope scope)
	{
		ArgumentNullException.ThrowIfNull(provider);
		ArgumentNullException.ThrowIfNull(scope);

		return Bind(provider.GetService<IServiceAccessor>(), scope.ServiceProvider);
	}

	/// <summary>
	/// 将访问器绑定到已有的服务提供程序（如进程内消息复用的发布方作用域）。
	/// </summary>
	/// <param name="provider">用于解析 <see cref="IServiceAccessor"/> 的服务提供程序。</param>
	/// <param name="scopedProvider">本次处理复用的服务提供程序。</param>
	/// <returns>释放时恢复先前绑定的对象。</returns>
	public static AmbientServiceProviderBinding Bind(IServiceProvider provider, IServiceProvider scopedProvider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		ArgumentNullException.ThrowIfNull(scopedProvider);

		return Bind(provider.GetService<IServiceAccessor>(), scopedProvider);
	}

	/// <summary>
	/// 将访问器绑定到 <paramref name="scopedProvider"/>；访问器为 <c>null</c> 时绑定为空操作。
	/// </summary>
	/// <param name="accessor">环境服务访问器。</param>
	/// <param name="scopedProvider">本次处理专用作用域的服务提供程序。</param>
	/// <returns>释放时恢复先前绑定的对象。</returns>
	public static AmbientServiceProviderBinding Bind(IServiceAccessor accessor, IServiceProvider scopedProvider)
	{
		ArgumentNullException.ThrowIfNull(scopedProvider);

		if (accessor == null)
		{
			return new AmbientServiceProviderBinding(null, null);
		}

		// 只读取显式绑定：ServiceProvider 的 getter 在未设置时会创建兜底作用域，
		// 而该作用域属于本次处理的执行流，随处理结束不可达且无人释放。
		var inherited = accessor.TryGetServiceProvider(out var current) ? current : null;
		accessor.ServiceProvider = scopedProvider;
		return new AmbientServiceProviderBinding(accessor, inherited);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed || _accessor == null)
		{
			return;
		}

		_disposed = true;
		_accessor.ServiceProvider = _inherited;
	}
}
