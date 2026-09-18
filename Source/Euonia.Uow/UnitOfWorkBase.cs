using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 工作单元实现的基类，提供作用域内服务解析与可释放行为的通用实现。
/// </summary>
/// <inheritdoc cref="DisposableObject" />
public abstract class UnitOfWorkBase : DisposableObject
{
    /// <summary>
    /// 获取限定于当前工作单元作用域的 <see cref="IServiceProvider"/>。
    /// </summary>
    /// <value>用于在工作单元生命周期内解析服务的服务提供程序。</value>
    public abstract IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 从工作单元作用域中解析指定类型的服务。
    /// </summary>
    /// <typeparam name="TService">要解析的服务类型。</typeparam>
    /// <returns>服务实例；若未注册则返回 <c>null</c>。</returns>
    public TService GetService<TService>()
        where TService : class
    {
        return ServiceProvider.GetService<TService>();
    }

    /// <summary>
    /// 从工作单元作用域中解析指定类型的所有已注册服务。
    /// </summary>
    /// <typeparam name="TService">要解析的服务类型。</typeparam>
    /// <returns>包含所有已解析服务的序列；若未注册则为空序列。</returns>
    public IEnumerable<TService> GetServices<TService>()
        where TService : class
    {
        return ServiceProvider.GetServices<TService>();
    }

    /// <summary>
    /// 从工作单元作用域中解析指定 <paramref name="serviceType"/> 的服务。
    /// </summary>
    /// <param name="serviceType">要解析的服务类型。</param>
    /// <returns>服务实例；若未注册则返回 <c>null</c>。</returns>
    public object GetService(Type serviceType)
    {
        return ServiceProvider.GetService(serviceType);
    }

    /// <summary>
    /// 从工作单元作用域中解析指定 <paramref name="serviceType"/> 的所有已注册服务。
    /// </summary>
    /// <param name="serviceType">要解析的服务类型。</param>
    /// <returns>包含所有已解析服务的序列；若未注册则为空序列。</returns>
    public IEnumerable<object> GetServices(Type serviceType)
    {
        return ServiceProvider.GetServices(serviceType);
    }
}