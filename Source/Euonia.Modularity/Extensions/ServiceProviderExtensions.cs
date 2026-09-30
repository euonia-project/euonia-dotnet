using Microsoft.Extensions.DependencyInjection;

namespace System;

/// <summary>
/// The extension methods for <see cref="IServiceProvider"/> interface.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the service object with specified name.
    /// </summary>
    /// <param name="provider">The service provider instance.</param>
    /// <param name="name">The registered service name.</param>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns></returns>
    public static TService GetNamedService<TService>(this IServiceProvider provider, string name)
        where TService : class
    {
        var @delegate = (NamedService<TService>)provider.GetService(typeof(NamedService<TService>));
        return @delegate?.Invoke(name);
    }

    /// <summary>
    /// Gets the service object with specified name.
    /// </summary>
    /// <param name="provider">The service provider instance.</param>
    /// <param name="name">The registered service name.</param>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Throws if service with specified name was not resolved.</exception>
    public static TService GetNamedRequiredService<TService>(this IServiceProvider provider, string name)
        where TService : class
    {
        var @delegate = (NamedService<TService>)provider.GetService(typeof(NamedService<TService>));
        return @delegate?.Invoke(name) ?? throw new InvalidOperationException(string.Format(Nerosoft.Euonia.Modularity.Properties.Resources.IDS_NAMED_SERVICE_NOT_FOUND, typeof(TService).FullName, name));
    }

	/// <summary>
	/// Gets the service object of the specified type.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="serviceType">An object that specifies the type of service object to get.</param>
	/// <param name="serviceKey">An object that specifies the key of service object to get.</param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static object GetKeyedService(this IServiceProvider provider, Type serviceType, object serviceKey)
	{
		if (provider is IKeyedServiceProvider keyedServiceProvider)
		{
			return keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
		}

		throw new InvalidOperationException(Nerosoft.Euonia.Modularity.Properties.Resources.IDS_KEYED_SERVICE_NOT_SUPPORTED);
	}
}
