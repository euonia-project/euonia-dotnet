using Microsoft.Extensions.DependencyInjection;

namespace System;

/// <summary>
/// <see cref="IServiceProvider"/> 接口的扩展方法。
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// 获取指定名称的服务对象。
    /// </summary>
    /// <param name="provider">服务提供程序实例。</param>
    /// <param name="name">已注册的服务名称。</param>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>名称对应的服务实例；未注册或其名称为 <see langword="null"/> 的注册项时为 <see langword="null"/>。</returns>
    public static TService? GetNamedService<TService>(this IServiceProvider provider, string name)
        where TService : class
    {
        var @delegate = provider.GetService(typeof(NamedService<TService>)) as NamedService<TService>;
        return @delegate?.Invoke(name);
    }

    /// <summary>
    /// 获取指定名称的服务对象。
    /// </summary>
    /// <param name="provider">服务提供程序实例。</param>
    /// <param name="name">已注册的服务名称。</param>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>名称对应的服务实例。</returns>
    /// <exception cref="InvalidOperationException">如果具有指定名称的服务未能解析，则抛出异常。</exception>
    public static TService GetNamedRequiredService<TService>(this IServiceProvider provider, string name)
        where TService : class
    {
        var @delegate = provider.GetService(typeof(NamedService<TService>)) as NamedService<TService>;
        return @delegate?.Invoke(name) ?? throw new InvalidOperationException(string.Format(Nerosoft.Euonia.Modularity.Properties.Resources.IDS_NAMED_SERVICE_NOT_FOUND, typeof(TService).FullName, name));
    }

	/// <summary>
	/// 获取指定类型的服务对象。
	/// </summary>
	/// <param name="provider">服务提供程序实例。</param>
	/// <param name="serviceType">一个对象，指定要获取的服务对象的类型。</param>
	/// <param name="serviceKey">一个对象，指定要获取的服务对象的键。</param>
	/// <returns>键对应的服务实例；未找到时为 <see langword="null"/>。</returns>
	/// <exception cref="InvalidOperationException">服务提供程序不支持键控服务时抛出。</exception>
	public static object? GetKeyedService(this IServiceProvider provider, Type serviceType, object serviceKey)
	{
		if (provider is IKeyedServiceProvider keyedServiceProvider)
		{
			return keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
		}

		throw new InvalidOperationException(Nerosoft.Euonia.Modularity.Properties.Resources.IDS_KEYED_SERVICE_NOT_SUPPORTED);
	}
}
