using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace System;

/// <summary>
/// Implementation of <see cref="ILazyServiceProvider"/>.
/// </summary>
public partial class LazyServiceProvider : ILazyServiceProvider
{
	/// <summary>
	/// Gets the cached services.
	/// </summary>
	private ConcurrentDictionary<ServiceIdentifier, Lazy<object>> CachedServices { get; } = new();

	/// <summary>
	/// Gets the service provider instance.
	/// </summary>
	private IServiceProvider ServiceProvider { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="LazyServiceProvider"/> class.
	/// </summary>
	/// <param name="serviceProvider"></param>
	public LazyServiceProvider(IServiceProvider serviceProvider)
	{
		ServiceProvider = serviceProvider;
	}

	/// <inheritdoc />
	public virtual T GetRequiredService<T>()
	{
		return (T)GetRequiredService(typeof(T));
	}

	/// <inheritdoc />
	public virtual object GetRequiredService(Type serviceType)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceType), _ => new Lazy<object>(() =>
		{
			ArgumentAssert.ThrowIfNull(ServiceProvider, nameof(ServiceProvider));
			ArgumentAssert.ThrowIfNull(serviceType, nameof(serviceType));
			var service = ServiceProvider.GetRequiredService(serviceType);
			return service;
		})).Value;
	}

	/// <inheritdoc />
	public virtual T GetService<T>()
	{
		return (T)GetService(typeof(T));
	}

	/// <inheritdoc />
	public virtual object GetService(Type serviceType)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceType), _ => new Lazy<object>(() => ServiceProvider.GetService(serviceType)!)).Value;
	}

	/// <inheritdoc />
	public virtual T GetService<T>(T defaultValue)
	{
		return (T)GetService(typeof(T), defaultValue!);
	}

	/// <inheritdoc />
	public virtual object GetService(Type serviceType, object defaultValue)
	{
		return GetService(serviceType) ?? defaultValue;
	}

	/// <inheritdoc />
	public virtual T GetService<T>(Func<IServiceProvider, object> factory)
	{
		return (T)GetService(typeof(T), factory);
	}

	/// <inheritdoc />
	public virtual object GetService(Type serviceType, Func<IServiceProvider, object> factory)
	{
		return CachedServices.GetOrAdd(new ServiceIdentifier(serviceType), _ => new Lazy<object>(() => factory(ServiceProvider))).Value;
	}
}