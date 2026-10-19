using Microsoft.Extensions.DependencyInjection;

namespace System;

/// <summary>
/// <see cref="IServiceAccessor"/> 的默认实现，绑定到创建它的服务作用域。
/// </summary>
/// <remarks>
/// 实例持有注入的 <see cref="IServiceProvider"/>，不做异步流缓存，也不创建兜底作用域。
/// 作用域的创建、复用与释放由宿主或消息执行作用域显式管理。
/// </remarks>
/// <seealso cref="IServiceAccessor"/>
public class ServiceAccessor(IServiceProvider provider) : IServiceAccessor
{
	/// <inheritdoc/>
	public IServiceProvider ServiceProvider { get; } = provider;

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
