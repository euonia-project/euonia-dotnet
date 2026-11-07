using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务注册上下文的默认实现。
/// </summary>
public class ServiceRegistrationContext : IServiceRegistrationContext
{
    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public ITypeList<IMethodInterceptor> Interceptors { get; } = new TypeList<IMethodInterceptor>();

    /// <inheritdoc />
    public Type ImplementationType { get; }

    /// <inheritdoc />
    public ServiceLifetime Lifetime { get; }

    /// <inheritdoc />
    public List<Type> ExposedTypes { get; }

    /// <summary>
    /// 初始化 <see cref="ServiceRegistrationContext"/> 类的新实例。
    /// </summary>
    /// <param name="services">当前正在注册服务的服务集合。</param>
    /// <param name="implementationType">服务的实现类型。</param>
    /// <param name="lifetime">服务的生命周期。</param>
    /// <param name="exposedTypes">实现类型要公开的服务类型。</param>
    /// <exception cref="ArgumentNullException">任一参数为 <see langword="null"/> 时抛出。</exception>
    public ServiceRegistrationContext(IServiceCollection services, Type implementationType, ServiceLifetime lifetime, List<Type> exposedTypes)
    {
        Services = Check.EnsureNotNull(services, nameof(services));
        ImplementationType = Check.EnsureNotNull(implementationType, nameof(implementationType));
        Lifetime = lifetime;
        ExposedTypes = Check.EnsureNotNull(exposedTypes, nameof(exposedTypes));
    }
}
