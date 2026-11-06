using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 将实现类按其服务类型导出到依赖注入容器的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExportServiceAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="ExportServiceAttribute"/> 的新实例。
    /// </summary>
    /// <param name="lifetime">服务的生命周期。</param>
    public ExportServiceAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// 初始化 <see cref="ExportServiceAttribute"/> 的新实例。
    /// </summary>
    /// <param name="lifetime">服务的生命周期。</param>
    /// <param name="serviceTypes">要导出的服务类型；为空时不指定具体服务类型。</param>
    /// <exception cref="InvalidOperationException"><paramref name="serviceTypes"/> 中包含既非类也非接口的类型。</exception>
    public ExportServiceAttribute(ServiceLifetime lifetime, params Type[] serviceTypes)
        : this(lifetime)
    {
        Lifetime = lifetime;

        if (serviceTypes is not { Length: > 0 })
        {
            return;
        }

        foreach (var serviceType in serviceTypes)
        {
            if (serviceType == null)
            {
                continue;
            }

            if (!serviceType.IsClass && !serviceType.IsInterface)
            {
                throw new InvalidOperationException(Resources.IDS_ONLY_INTERFACE_OR_CLASS);
            }
        }

        ServiceTypes = new List<Type>(serviceTypes);
    }

    /// <summary>
    /// 获取服务生命周期。
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// 获取已实现的服务类型。
    /// </summary>
    public List<Type>? ServiceTypes { get; }

    /// <summary>
    /// 获取或设置一个值，指示是否仅在服务尚未注册时注册。
    /// </summary>
    public virtual bool TryRegister { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示是否替换已存在的服务注册。
    /// </summary>
    public virtual bool ReplaceServices { get; set; }
}

/// <summary>
/// 将服务以瞬态生命周期导出的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExportServiceAsTransientAttribute : ExportServiceAttribute
{
    /// <summary>
    /// 初始化 <see cref="ExportServiceAsTransientAttribute"/> 的新实例。
    /// </summary>
    public ExportServiceAsTransientAttribute()
        : base(ServiceLifetime.Transient)
    {
    }

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsTransientAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceType">要导出的服务类型。</param>
    public ExportServiceAsTransientAttribute(Type serviceType)
        : base(ServiceLifetime.Transient, serviceType)
    {
    }
}

/// <summary>
/// 将服务以单例生命周期导出的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExportServiceAsSingletonAttribute : ExportServiceAttribute
{
    /// <summary>
    /// 初始化 <see cref="ExportServiceAsSingletonAttribute"/> 的新实例。
    /// </summary>
    public ExportServiceAsSingletonAttribute()
        : base(ServiceLifetime.Singleton)
    {
    }

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsSingletonAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceType">要导出的服务类型。</param>
    public ExportServiceAsSingletonAttribute(Type serviceType)
        : base(ServiceLifetime.Singleton, serviceType)
    {
    }
}

/// <summary>
/// 将服务以作用域生命周期导出的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExportServiceAsScopedAttribute : ExportServiceAttribute
{
    /// <summary>
    /// 初始化 <see cref="ExportServiceAsScopedAttribute"/> 的新实例。
    /// </summary>
    public ExportServiceAsScopedAttribute()
        : base(ServiceLifetime.Scoped)
    {
    }

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsScopedAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceType">要导出的服务类型。</param>
    public ExportServiceAsScopedAttribute(Type serviceType)
        : base(ServiceLifetime.Scoped, serviceType)
    {
    }
}