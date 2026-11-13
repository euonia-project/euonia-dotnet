namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义模块管理器的接口。
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// 获取一个值，指示模块是否已经初始化。
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    void InitializeModules(ApplicationInitializationContext context);

    /// <summary>
    /// 异步初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="InitializeModules"/>。</remarks>
    Task InitializeModulesAsync(ApplicationInitializationContext context, CancellationToken cancellationToken = default)
    {
        InitializeModules(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 卸载模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文，提供模块卸载所需的服务提供程序。</param>
    void UnloadModules(ApplicationShutdownContext context);

    /// <summary>
    /// 异步卸载模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文，提供模块卸载所需的服务提供程序。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="UnloadModules"/>。</remarks>
    Task UnloadModulesAsync(ApplicationShutdownContext context, CancellationToken cancellationToken = default)
    {
        UnloadModules(context);
        return Task.CompletedTask;
    }
}
