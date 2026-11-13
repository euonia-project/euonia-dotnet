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
        return (T)GetKeyedService(typeof(T), serviceKey)!;
    }

    /// <inheritdoc />
    public virtual object GetKeyedService(Type serviceType, object serviceKey)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceKey, serviceType),
            () => ServiceProvider.GetKeyedService(serviceType, serviceKey))!;
    }

    /// <inheritdoc />
    public virtual T GetRequiredKeyedService<T>(object serviceKey)
    {
        return (T)GetRequiredKeyedService(typeof(T), serviceKey)!;
    }

    /// <inheritdoc />
    public virtual object GetRequiredKeyedService(Type serviceType, object serviceKey)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceKey, serviceType),
            () => ServiceProvider.GetRequiredKeyedService(serviceType, serviceKey))!;
    }

    /// <inheritdoc />
    public virtual T GetKeyedService<T>(object serviceKey, T defaultValue)
    {
        return (T)GetKeyedService(typeof(T), serviceKey, defaultValue!)!;
    }

    /// <inheritdoc />
    public virtual T GetKeyedService<T>(object serviceKey, Func<IServiceProvider, T> factory)
    {
        Check.EnsureNotNull(factory, nameof(factory));

        return (T)GetKeyedService(typeof(T), serviceKey, provider => factory(provider)!)!;
    }

    /// <inheritdoc />
    public virtual object GetKeyedService(Type serviceType, object serviceKey, Func<IServiceProvider, object> factory)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));
        Check.EnsureNotNull(factory, nameof(factory));

        return Resolve(
            new ServiceIdentifier(serviceKey, serviceType),
            () => factory(ServiceProvider))!;
    }

    /// <inheritdoc />
    public virtual object GetKeyedService(Type serviceType, object serviceKey, object defaultValue)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceKey, serviceType),
            () => ServiceProvider.GetKeyedService(serviceType, serviceKey) ?? defaultValue)!;
    }
}
