using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 将实现类按其服务类型导出到依赖注入容器的特性。
/// </summary>
/// <remarks>
/// 同一个实现类型可以应用多个该特性，以便使用不同生命周期或不同服务类型集合进行注册。
/// 如果特性未指定服务类型，则按约定从类型的接口和类型自身推断公开的服务类型。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
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
        if (serviceTypes is not { Length: > 0 })
        {
            return;
        }

        var exportedTypes = new List<Type>(serviceTypes.Length);

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

            exportedTypes.AddIfNotContains(serviceType);
        }

        ServiceTypes = exportedTypes.Count > 0 ? exportedTypes.AsReadOnly() : null;
    }

    /// <summary>
    /// 获取服务生命周期。
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// 获取已实现的服务类型。
    /// </summary>
    public IReadOnlyList<Type>? ServiceTypes { get; }

    /// <summary>
    /// 获取或设置一个值，指示是否在公开服务类型中包含实现类型自身。
    /// </summary>
    /// <remarks>该设置仅在显式指定 <see cref="ServiceTypes"/> 时生效。</remarks>
    public bool IncludeSelf { get; set; }

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
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
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

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsTransientAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceTypes">要导出的服务类型。</param>
    public ExportServiceAsTransientAttribute(params Type[] serviceTypes)
        : base(ServiceLifetime.Transient, serviceTypes)
    {
    }
}

/// <summary>
/// 将服务以单例生命周期导出的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
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

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsSingletonAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceTypes">要导出的服务类型。</param>
    public ExportServiceAsSingletonAttribute(params Type[] serviceTypes)
        : base(ServiceLifetime.Singleton, serviceTypes)
    {
    }
}

/// <summary>
/// 将服务以作用域生命周期导出的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
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

    /// <summary>
    /// 初始化 <see cref="ExportServiceAsScopedAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceTypes">要导出的服务类型。</param>
    public ExportServiceAsScopedAttribute(params Type[] serviceTypes)
        : base(ServiceLifetime.Scoped, serviceTypes)
    {
    }
}
