using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <inheritdoc />
public abstract class AutomaticRegistrationBase : IAutomaticRegistration
{
    /// <inheritdoc />
    public virtual void AddAssembly(IServiceCollection services, Assembly assembly)
    {
        Check.EnsureNotNull(services, nameof(services));
        Check.EnsureNotNull(assembly, nameof(assembly));

        var types = AssemblyHelper.LoadTypes(assembly)
                                  .Where(IsRegistrableType)
                                  .ToArray();

        AddTypes(services, types);
    }

    /// <inheritdoc />
    public virtual void AddTypes(IServiceCollection services, params Type[] types)
    {
        Check.EnsureNotNull(services, nameof(services));

        if (types == null)
        {
            return;
        }

        foreach (var type in types)
        {
            if (type == null)
            {
                continue;
            }

            AddType(services, type);
        }
    }

    /// <inheritdoc />
    public abstract void AddType(IServiceCollection services, Type type);

    /// <summary>
    /// 判断指定类型是否适合参与约定式注册。
    /// </summary>
    /// <param name="type">要判断的类型。</param>
    /// <returns>类型为可实例化的普通类时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    protected virtual bool IsRegistrableType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               && !typeof(IModuleContext).IsAssignableFrom(type)
               && !type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
    }

    /// <summary>
    /// 调用已注册的服务公开操作，处理指定实现类型的服务类型列表。
    /// </summary>
    /// <param name="services">从中读取服务公开操作的服务集合。</param>
    /// <param name="implementationType">被注册的实现类型。</param>
    /// <param name="serviceTypes">该实现类型要公开的服务类型列表，将作为上下文传给每个公开操作。</param>
    /// <remarks>当服务集合中未注册任何公开操作时直接返回，不做任何处理。</remarks>
    protected virtual void TriggerServiceExposing(IServiceCollection services, Type implementationType, List<Type> serviceTypes)
    {
        var exposeActions = services.GetSingletonInstanceOrNull<IObjectAccessor<ServiceExposingAction>>()?.Value;
        if (exposeActions is not { Count: > 0 })
        {
            return;
        }

        var context = new ServiceExposingContext(implementationType, serviceTypes);
        foreach (var action in exposeActions)
        {
            action(context);
        }
    }

    /// <summary>
    /// 调用已注册的服务注册操作。
    /// </summary>
    /// <param name="services">从中读取服务注册操作的服务集合。</param>
    /// <param name="implementationType">被注册的实现类型。</param>
    /// <param name="lifetime">服务的生命周期。</param>
    /// <param name="serviceTypes">该实现类型要公开的服务类型列表。</param>
    /// <remarks>当服务集合中未注册任何注册操作时直接返回，不做任何处理。</remarks>
    protected virtual void TriggerServiceRegistration(IServiceCollection services, Type implementationType, ServiceLifetime lifetime, List<Type> serviceTypes)
    {
        var registrationActions = services.GetSingletonInstanceOrNull<IObjectAccessor<ServiceRegistrationAction>>()?.Value;
        if (registrationActions is not { Count: > 0 })
        {
            return;
        }

        var context = new ServiceRegistrationContext(services, implementationType, lifetime, serviceTypes);
        foreach (var action in registrationActions)
        {
            action(context);
        }
    }

    /// <summary>
    /// 获取指定类型上标记的所有 <see cref="ExportServiceAttribute"/>。
    /// </summary>
    /// <param name="type">要检查的特性宿主类型。</param>
    /// <returns>类型或其基类上标记的导出特性数组；未标记时返回空数组。</returns>
    protected virtual IReadOnlyList<ExportServiceAttribute> GetDependencyAttributes(Type type)
    {
        return type.GetCustomAttributes<ExportServiceAttribute>(true).ToArray();
    }

    /// <summary>
    /// 获取指定类型上标记的第一个 <see cref="ExportServiceAttribute"/>。
    /// </summary>
    /// <param name="type">要检查的特性宿主类型。</param>
    /// <returns>类型或其基类上标记的 <see cref="ExportServiceAttribute"/>；未标记时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 当类型标记了多个导出特性时，建议使用 <see cref="GetDependencyAttributes"/> 获取完整列表。
    /// </remarks>
    protected virtual ExportServiceAttribute? GetDependencyAttribute(Type type)
    {
        return GetDependencyAttributes(type).FirstOrDefault();
    }

    /// <summary>
    /// 确定指定类型的服务生命周期。
    /// </summary>
    /// <param name="type">要确定生命周期的类型。</param>
    /// <param name="attribute">类型上标记的导出特性，可为 <see langword="null"/>。</param>
    /// <returns>确定的服务生命周期；当特性未指定且无法从类型推断时返回 <see langword="null"/>。</returns>
    /// <remarks>优先使用 <paramref name="attribute"/> 的 <see cref="ExportServiceAttribute.Lifetime"/>，否则依次从类型实现的依赖标记接口和 <see cref="GetDefaultLifeTimeOfType"/> 推断。</remarks>
    protected virtual ServiceLifetime? GetLifeTime(Type type, ExportServiceAttribute? attribute)
    {
        if (attribute != null)
        {
            return attribute.Lifetime;
        }

        return GetServiceLifetimeFromHierarchy(type) ?? GetDefaultLifeTimeOfType(type);
    }

    /// <summary>
    /// 根据类型实现的依赖标记接口推断服务生命周期。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>匹配到的服务生命周期；类型未实现任何依赖标记接口时返回 <see langword="null"/>。</returns>
    /// <remarks>当类型同时实现多个标记接口时，按 <see cref="ITransientDependency"/>、<see cref="ISingletonDependency"/>、<see cref="IScopedDependency"/> 的顺序返回首个匹配项。</remarks>
    protected virtual ServiceLifetime? GetServiceLifetimeFromHierarchy(Type type)
    {
        if (typeof(ITransientDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Transient;
        }

        if (typeof(ISingletonDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Singleton;
        }

        if (typeof(IScopedDependency).IsAssignableFrom(type))
        {
            return ServiceLifetime.Scoped;
        }

        return null;
    }

    /// <summary>
    /// 获取指定类型的默认服务生命周期。
    /// </summary>
    /// <param name="type">要确定默认生命周期的类型。</param>
    /// <returns>默认实现始终返回 <see langword="null"/>，表示不提供默认生命周期。</returns>
    /// <remarks>派生类可重写此方法，在类型既未标记导出特性、也未实现依赖标记接口时提供兜底生命周期。</remarks>
    protected virtual ServiceLifetime? GetDefaultLifeTimeOfType(Type type)
    {
        return null;
    }

    /// <summary>
    /// 获取指定实现类型要公开的服务类型列表。
    /// </summary>
    /// <param name="type">要探索的实现类型。</param>
    /// <returns>类型声明的公开服务类型；未声明时按约定匹配与类型名对应的接口，并包含类型自身。</returns>
    protected virtual List<Type> GetExposedServiceTypes(Type type)
    {
        return ExposedServiceExplorer.GetExposedServices(type);
    }

    /// <summary>
    /// 为指定服务类型创建服务描述符。
    /// </summary>
    /// <param name="serviceType">要注册的服务类型。</param>
    /// <param name="implementationType">服务的实现类型。</param>
    /// <param name="exposingServiceTypes">实现类型公开的全部服务类型，用于判断是否可复用已有注册。</param>
    /// <param name="lifeTime">服务的生命周期。</param>
    /// <returns>使用指定服务类型、实现类型和生命周期创建的服务描述符。</returns>
    /// <remarks>当生命周期为单例或作用域，且能找到可重定向的服务类型时，描述符通过服务提供程序解析该类型，以便在同一生命周期范围内共享同一实例。</remarks>
    protected virtual ServiceDescriptor CreateServiceDescriptor(Type serviceType, Type implementationType, IReadOnlyList<Type> exposingServiceTypes, ServiceLifetime lifeTime)
    {
        if (lifeTime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
        {
            var redirectedType = GetRedirectedTypeOrNull(serviceType, implementationType, exposingServiceTypes);

            if (redirectedType != null)
            {
                return ServiceDescriptor.Describe(serviceType, provider => provider.GetService(redirectedType)!, lifeTime);
            }
        }

        return ServiceDescriptor.Describe(serviceType, implementationType, lifeTime);
    }

    /// <summary>
    /// 获取可用于重定向解析的服务类型。
    /// </summary>
    /// <param name="serviceType">当前正在注册的服务类型。</param>
    /// <param name="implementationType">服务的实现类型。</param>
    /// <param name="exposingServiceTypes">实现类型公开的全部服务类型。</param>
    /// <returns>可重定向到的服务类型；无需或无法重定向时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>公开类型少于两个，或 <paramref name="serviceType"/> 与实现类型相同时，返回 <see langword="null"/>。</para>
    /// <para>实现类型本身也在公开列表中时返回实现类型；否则返回首个不等于 <paramref name="serviceType"/> 且可赋值给 <paramref name="serviceType"/> 的公开类型。</para>
    /// </remarks>
    protected virtual Type? GetRedirectedTypeOrNull(Type serviceType, Type implementationType, IReadOnlyList<Type> exposingServiceTypes)
    {
        if (exposingServiceTypes.Count < 2 || serviceType == implementationType)
        {
            return null;
        }

        if (exposingServiceTypes.Contains(implementationType))
        {
            return implementationType;
        }

        return exposingServiceTypes.FirstOrDefault(type => type != serviceType && serviceType.IsAssignableFrom(type));
    }
}
