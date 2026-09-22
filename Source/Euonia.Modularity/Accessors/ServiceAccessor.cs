using Microsoft.Extensions.DependencyInjection;

namespace System;

/// <summary>
/// <see cref="IServiceAccessor"/> 的默认实现，使用 <see cref="AsyncLocal{T}"/> 持有 <see cref="IServiceProvider"/>。
/// </summary>
/// <remarks>
/// <para>
/// 内部通过 <see cref="AsyncLocal{T}"/> 存储 <see cref="IServiceProvider"/>，
/// 使得不同的异步执行流可以各自维护独立的服务提供程序引用，
/// 避免在并发场景下发生服务解析冲突。
/// </para>
/// <para>
/// 作为 <see cref="ISingletonDependency"/> 注册，在整个应用生命周期内共享同一实例。
/// 调用方可在请求开始时设置 <see cref="ServiceProvider"/>，
/// 随后在同一条异步调用链中通过 <c>GetService</c> / <c>GetRequiredService</c> 解析服务。
/// </para>
/// </remarks>
/// <seealso cref="IServiceAccessor"/>
public class ServiceAccessor(IServiceScopeFactory factory) : IServiceAccessor
{
	/// <summary>
	/// 使用 <see cref="AsyncLocal{T}"/> 存储当前异步执行流中的 <see cref="IServiceProvider"/> 实例。
	/// </summary>
	private readonly AsyncLocal<IServiceProvider> _provider = new();

	/// <summary>
	/// 未被显式设置 <see cref="ServiceProvider"/> 时使用的兜底作用域，按异步执行流缓存。
	/// </summary>
	/// <remarks>
	/// 此前该兜底路径写作 <c>factory.CreateScope().ServiceProvider</c>，即**每次读取属性都新建一个作用域且从不释放**。
	/// 由于 HTTP 请求之外的路径（消息消费、后台任务、发件箱/收件箱轮询）都不会显式设置该属性，
	/// 每一次服务解析都会泄漏一个作用域及其中的 <see cref="IDisposable"/> 服务（如 DbContext）。
	/// <para>
	/// 现在按异步执行流缓存：同一条执行流内最多创建一个兜底作用域。
	/// 仍需注意：该作用域随执行流存活，框架尚未提供执行流结束时的释放钩子。
	/// 因此宿主应在流程开始时显式设置 <see cref="ServiceProvider"/>（HTTP 路径由 HostingModule 的中间件完成），
	/// 长驻的后台流程应自行创建并释放作用域。
	/// </para>
	/// </remarks>
	private readonly AsyncLocal<IServiceScope> _fallbackScope = new();

	/// <inheritdoc/>
	public IServiceProvider ServiceProvider
	{
		get => _provider.Value ?? GetFallbackScope().ServiceProvider;
		set => _provider.Value = value;
	}

	/// <summary>
	/// 获取当前异步执行流的兜底作用域；不存在时创建一次并缓存。
	/// </summary>
	/// <returns>当前执行流的兜底作用域。</returns>
	private IServiceScope GetFallbackScope()
	{
		var scope = _fallbackScope.Value;
		if (scope == null)
		{
			scope = factory.CreateScope();
			_fallbackScope.Value = scope;
		}

		return scope;
	}

	/// <inheritdoc/>
	public T GetService<T>()
	{
		return ServiceProvider.GetService<T>();
	}

	/// <inheritdoc />
	public T GetRequiredService<T>()
	{
		return ServiceProvider.GetRequiredService<T>();
	}

	/// <inheritdoc />
	public T GetKeyedService<T>(object key)
	{
		return ServiceProvider.GetKeyedService<T>(key);
	}

	/// <inheritdoc />
	public T GetRequiredKeyedService<T>(object name)
	{
		return ServiceProvider.GetRequiredKeyedService<T>(name);
	}

	/// <inheritdoc/>
	public object GetService(Type type)
	{
		return ServiceProvider.GetService(type);
	}
}