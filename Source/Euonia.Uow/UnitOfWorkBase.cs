using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Base class providing common unit-of-work helpers such as scoped service resolution
/// and disposable behavior for concrete unit-of-work implementations.
/// </summary>
public abstract class UnitOfWorkBase : DisposableObject
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> scoped to the current unit of work.
    /// Implementations must provide the service provider used for resolving services
    /// within the unit of work's lifetime.
    /// </summary>
    public abstract IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Resolves a service of type <typeparamref name="TService"/> from the unit of work's scope.
    /// Returns null if the service is not registered.
    /// </summary>
    /// <typeparam name="TService">The type of the service to resolve.</typeparam>
    /// <returns>An instance of <typeparamref name="TService"/> or <c>null</c> if not registered.</returns>
    public TService GetService<TService>()
        where TService : class
    {
        return ServiceProvider.GetService<TService>();
    }

    /// <summary>
    /// Resolves all registered services of type <typeparamref name="TService"/> from the unit of work's scope.
    /// </summary>
    /// <typeparam name="TService">The type of the services to resolve.</typeparam>
    /// <returns>An <see cref="IEnumerable{TService}"/> containing all resolved services; empty if none registered.</returns>
    public IEnumerable<TService> GetServices<TService>()
        where TService : class
    {
        return ServiceProvider.GetServices<TService>();
    }

    /// <summary>
    /// Resolves a service of the specified <paramref name="serviceType"/> from the unit of work's scope.
    /// Returns null if the service is not registered.
    /// </summary>
    /// <param name="serviceType">The type of the service to resolve.</param>
    /// <returns>An instance of the requested service or <c>null</c> if not registered.</returns>
    public object GetService(Type serviceType)
    {
        return ServiceProvider.GetService(serviceType);
    }

    /// <summary>
    /// Resolves all registered services of the specified <paramref name="serviceType"/> from the unit of work's scope.
    /// </summary>
    /// <param name="serviceType">The type of the services to resolve.</param>
    /// <returns>An <see cref="IEnumerable{Object}"/> containing all resolved services; empty if none registered.</returns>
    public IEnumerable<object> GetServices(Type serviceType)
    {
        return ServiceProvider.GetServices(serviceType);
    }
}