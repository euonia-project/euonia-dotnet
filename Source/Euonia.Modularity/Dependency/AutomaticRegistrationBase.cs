using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Reflection;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <inheritdoc />
public abstract class AutomaticRegistrationBase : IAutomaticRegistration
{
    /// <inheritdoc />
    public virtual void AddAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = AssemblyHelper.LoadTypes(assembly)
                                  .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericType: false })
                                  .ToArray();
        AddTypes(services, types);
    }

    /// <inheritdoc />
    public virtual void AddTypes(IServiceCollection services, params Type[] types)
    {
        foreach (var type in types)
        {
            AddType(services, type);
        }
    }

    /// <inheritdoc />
    public abstract void AddType(IServiceCollection services, Type type);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="implementationType"></param>
    /// <param name="serviceTypes"></param>
    protected virtual void TriggerServiceExposing(IServiceCollection services, Type implementationType, List<Type> serviceTypes)
    {
        var exposeActions = services.GetExposingActions();
        if (!exposeActions.Any())
        {
            return;
        }

        var args = new ServiceExposingContext(implementationType, serviceTypes);
        foreach (var action in exposeActions)
        {
            action(args);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected virtual ExportServiceAttribute GetDependencyAttribute(Type type)
    {
        return type.GetCustomAttribute<ExportServiceAttribute>(true);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    protected virtual ServiceLifetime? GetLifeTime(Type type, ExportServiceAttribute attribute)
    {
        if (attribute != null)
        {
            return attribute.Lifetime;
        }

        return GetServiceLifetimeFromHierarchy(type) ?? GetDefaultLifeTimeOfType(type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected virtual ServiceLifetime? GetServiceLifetimeFromHierarchy(Type type)
    {
        if (typeof(ITransientDependency).GetTypeInfo().IsAssignableFrom(type))
        {
            return ServiceLifetime.Transient;
        }

        if (typeof(ISingletonDependency).GetTypeInfo().IsAssignableFrom(type))
        {
            return ServiceLifetime.Singleton;
        }

        if (typeof(IScopedDependency).GetTypeInfo().IsAssignableFrom(type))
        {
            return ServiceLifetime.Scoped;
        }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected virtual ServiceLifetime? GetDefaultLifeTimeOfType(Type type)
    {
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    protected virtual List<Type> GetExposedServiceTypes(Type type)
    {
        return ExposedServiceExplorer.GetExposedServices(type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <param name="exposingServiceTypes"></param>
    /// <param name="lifeTime"></param>
    /// <returns></returns>
    protected virtual ServiceDescriptor CreateServiceDescriptor(Type serviceType, Type implementationType, List<Type> exposingServiceTypes, ServiceLifetime lifeTime)
    {
        if (lifeTime.IsIn(ServiceLifetime.Singleton, ServiceLifetime.Scoped))
        {
            var redirectedType = GetRedirectedTypeOrNull(serviceType, implementationType, exposingServiceTypes);

            if (redirectedType != null)
            {
                return ServiceDescriptor.Describe(serviceType, provider => provider.GetService(redirectedType), lifeTime);
            }
        }

        {
            //Empty block to prevent IDE suggestion.
        }

        return ServiceDescriptor.Describe(serviceType, implementationType, lifeTime);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <param name="exposingServiceTypes"></param>
    /// <returns></returns>
    protected virtual Type GetRedirectedTypeOrNull(Type serviceType, Type implementationType, List<Type> exposingServiceTypes)
    {
        if (exposingServiceTypes.Count < 2)
        {
            return null;
        }

        if (serviceType == implementationType)
        {
            return null;
        }

        if (exposingServiceTypes.Contains(implementationType))
        {
            return implementationType;
        }

        {
            //Empty block to prevent IDE suggestion.
        }

        return exposingServiceTypes.FirstOrDefault(type => type != serviceType && serviceType.IsAssignableFrom(type));
    }
}