namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义模块管理器的接口。
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    void InitializeModules(ApplicationInitializationContext context);

    /// <summary>
    /// 卸载模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文，提供模块卸载所需的服务提供程序。</param>
    void UnloadModules(ApplicationShutdownContext context);
}
