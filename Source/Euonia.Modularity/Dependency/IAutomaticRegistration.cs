using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义用于自动注册的接口。
/// </summary>
public interface IAutomaticRegistration
{
    /// <summary>
    /// 将指定程序集中的类型添加到服务集合。
    /// </summary>
    /// <param name="services">要添加服务的服务集合。</param>
    /// <param name="assembly">要扫描并注册其中类型的程序集。</param>
    void AddAssembly(IServiceCollection services, Assembly assembly);

    /// <summary>
    /// 将类型添加到服务集合。
    /// </summary>
    /// <param name="services">要添加服务的服务集合。</param>
    /// <param name="types">要注册的类型数组。</param>
    void AddTypes(IServiceCollection services, params Type[] types);

    /// <summary>
    /// 将单个类型添加到服务集合。
    /// </summary>
    /// <param name="services">要添加服务的服务集合。</param>
    /// <param name="type">要注册的单个类型。</param>
    void AddType(IServiceCollection services, Type type);
}
