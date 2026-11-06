using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace System;

/// <summary>
/// 延迟解析的服务提供程序；此分部类提供按服务键解析服务的成员。
/// </summary>
public partial class LazyServiceProvider
{
	/// <inheritdoc />
	public virtual T GetKeyedService<T>(object serviceKey)
	{
		return (T)GetKeyedService(typeof(T), serviceKey);
	}

	/// <inheritdoc />
	public virtual object GetKeyedService(Type serviceType, object serviceKey)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceKey, serviceType), _ => new Lazy<object>(() => ServiceProvider.GetKeyedService(serviceType, serviceKey)!)).Value;
	}

	/// <inheritdoc />
	public virtual T GetRequiredKeyedService<T>(object serviceKey)
	{
		return (T)GetRequiredKeyedService(typeof(T), serviceKey);
	}

	/// <inheritdoc />
	public virtual object GetRequiredKeyedService(Type serviceType, object serviceKey)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceKey, serviceType), _ => new Lazy<object>(() => ServiceProvider.GetRequiredKeyedService(serviceType, serviceKey))).Value!;
	}

	/// <inheritdoc />
	public virtual T GetKeyedService<T>(object serviceKey, T defaultValue)
	{
		return (T)GetKeyedService(typeof(T), serviceKey, defaultValue!);
	}

	/// <inheritdoc />
	public virtual T GetKeyedService<T>(object serviceKey, Func<IServiceProvider, T> factory)
	{
		return (T)GetKeyedService(typeof(T), serviceKey, provider => factory(provider)!);
	}

	/// <inheritdoc />
	public virtual object GetKeyedService(Type serviceType, object serviceKey, Func<IServiceProvider, object> factory)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceKey, serviceType), _ => new Lazy<object>(() => factory(ServiceProvider))).Value;
	}

	/// <inheritdoc />
	public virtual object GetKeyedService(Type serviceType, object serviceKey, object defaultValue)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceKey, serviceType), _ => new Lazy<object>(() => defaultValue)).Value;
	}
}
