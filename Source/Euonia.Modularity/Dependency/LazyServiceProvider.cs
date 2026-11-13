using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace System;

/// <summary>
/// <see cref="ILazyServiceProvider"/> 的实现。
/// </summary>
/// <remarks>
/// 服务实例在首次访问时解析，并按服务标识（服务类型及可选服务键）缓存，后续请求返回同一实例。
/// 缓存使用 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>，保证并发访问只创建一次实例。
/// </remarks>
public partial class LazyServiceProvider : ILazyServiceProvider
{
    private readonly ConcurrentDictionary<ServiceIdentifier, Lazy<object?>> _cachedServices = new();

    /// <summary>
    /// 获取服务提供程序实例。
    /// </summary>
    private IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 初始化 <see cref="LazyServiceProvider"/> 类的新实例。
    /// </summary>
    /// <param name="serviceProvider">用于解析服务的服务提供程序。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="serviceProvider"/> 为 <see langword="null"/> 时抛出。</exception>
    public LazyServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = Check.EnsureNotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <inheritdoc />
    public virtual T GetRequiredService<T>()
    {
        return (T)GetRequiredService(typeof(T))!;
    }

    /// <inheritdoc />
    public virtual object GetRequiredService(Type serviceType)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceType),
            () => ServiceProvider.GetRequiredService(serviceType))!;
    }

    /// <inheritdoc />
    public virtual T GetService<T>()
    {
        return (T)GetService(typeof(T))!;
    }

    /// <inheritdoc />
    public virtual object GetService(Type serviceType)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceType),
            () => ServiceProvider.GetService(serviceType))!;
    }

    /// <inheritdoc />
    public virtual T GetService<T>(T defaultValue)
    {
        return (T)GetService(typeof(T), defaultValue!)!;
    }

    /// <inheritdoc />
    public virtual object GetService(Type serviceType, object defaultValue)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));

        return Resolve(
            new ServiceIdentifier(serviceType),
            () => ServiceProvider.GetService(serviceType) ?? defaultValue)!;
    }

    /// <inheritdoc />
    public virtual T GetService<T>(Func<IServiceProvider, object> factory)
    {
        return (T)GetService(typeof(T), factory)!;
    }

    /// <inheritdoc />
    public virtual object GetService(Type serviceType, Func<IServiceProvider, object> factory)
    {
        Check.EnsureNotNull(serviceType, nameof(serviceType));
        Check.EnsureNotNull(factory, nameof(factory));

        return Resolve(
            new ServiceIdentifier(serviceType),
            () => factory(ServiceProvider))!;
    }

    private object? Resolve(ServiceIdentifier identifier, Func<object?> factory)
    {
        return _cachedServices
               .GetOrAdd(
                   identifier,
                   _ => new Lazy<object?>(factory, LazyThreadSafetyMode.ExecutionAndPublication))
               .Value;
    }
}
