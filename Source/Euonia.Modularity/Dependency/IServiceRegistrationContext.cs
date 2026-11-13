using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务注册上下文接口。
/// </summary>
public interface IServiceRegistrationContext
{
    /// <summary>
    /// 获取当前正在注册服务的服务集合。
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 获取方法拦截器类型。
    /// </summary>
    ITypeList<IMethodInterceptor> Interceptors { get; }

    /// <summary>
    /// 获取实现类型。
    /// </summary>
    Type ImplementationType { get; }

    /// <summary>
    /// 获取服务的生命周期。
    /// </summary>
    ServiceLifetime Lifetime { get; }

    /// <summary>
    /// 获取当前实现类型要公开的服务类型。
    /// </summary>
    List<Type> ExposedTypes { get; }
}
