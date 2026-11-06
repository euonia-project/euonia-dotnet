using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务注册上下文接口。
/// </summary>
public interface IServiceRegistrationContext
{
    /// <summary>
    /// 获取方法拦截器类型。
    /// </summary>
    ITypeList<IMethodInterceptor> Interceptors { get; }

    /// <summary>
    /// 获取实现类型。
    /// </summary>
    Type ImplementationType { get; }
}