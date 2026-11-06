using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 表示一个模块化应用程序，提供对模块容器、服务集合与服务提供程序的访问，并支持关闭。
/// </summary>
public interface IModularityApplication : IModuleContainer, IDisposable
{
    /// <summary>
    /// 获取启动模块类型。
    /// </summary>
    Type StartupModuleType { get; }

    /// <summary>
    /// 获取当前应用程序的 <see cref="IServiceCollection"/> 实例。
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 获取当前应用程序的 <see cref="IServiceProvider"/> 实例。
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 关闭应用程序。
    /// </summary>
    void Shutdown();
}
