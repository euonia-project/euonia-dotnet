using Microsoft.Extensions.Configuration;
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
    /// 获取当前应用程序的配置实例。
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// 获取一个值，指示模块是否已经完成初始化。
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 获取一个值，指示应用程序是否已经关闭。
    /// </summary>
    bool IsShutdown { get; }

    /// <summary>
    /// 关闭应用程序。
    /// </summary>
    /// <remarks>该方法可安全地重复调用，后续调用将被忽略。</remarks>
    void Shutdown();
}
