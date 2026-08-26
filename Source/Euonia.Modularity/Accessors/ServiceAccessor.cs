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

	/// <inheritdoc/>
	public IServiceProvider ServiceProvider
	{
		get => _provider.Value;
		set => _provider.Value = value;
	}

	/// <inheritdoc/>
	public T GetService<T>()
	{
		return (ServiceProvider ?? factory.CreateScope().ServiceProvider).GetService<T>();
	}

	/// <inheritdoc />
	public T GetRequiredService<T>()
	{
		return (ServiceProvider ?? factory.CreateScope().ServiceProvider).GetRequiredService<T>();
	}

	/// <inheritdoc />
	public T GetKeyedService<T>(object key)
	{
		return (ServiceProvider ?? factory.CreateScope().ServiceProvider).GetKeyedService<T>(key);
	}

	/// <inheritdoc />
	public T GetRequiredKeyedService<T>(object name)
	{
		return (ServiceProvider ?? factory.CreateScope().ServiceProvider).GetRequiredKeyedService<T>(name);
	}

	/// <inheritdoc/>
	public object GetService(Type type)
	{
		return (ServiceProvider ?? factory.CreateScope().ServiceProvider).GetService(type);
	}
}