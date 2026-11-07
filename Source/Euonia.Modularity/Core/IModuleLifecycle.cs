namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义模块生命周期的接口。
/// </summary>
/// <remarks>
/// 异步方法提供默认实现并回退到对应的同步方法，便于既有生命周期实现继续工作。
/// </remarks>
public interface IModuleLifecycle : ITransientDependency
{
    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    /// <param name="module">当前正在初始化的模块实例。</param>
    void Initialize(ApplicationInitializationContext context, IModuleContext module)
    {
    }

    /// <summary>
    /// 异步初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    /// <param name="module">当前正在初始化的模块实例。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="Initialize"/>。</remarks>
    Task InitializeAsync(ApplicationInitializationContext context, IModuleContext module)
    {
        Initialize(context, module);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <param name="module">模块上下文。</param>
    void Finalize(ApplicationShutdownContext context, IModuleContext module)
    {
    }

    /// <summary>
    /// 异步释放模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <param name="module">模块上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="Finalize"/>。</remarks>
    Task FinalizeAsync(ApplicationShutdownContext context, IModuleContext module)
    {
        Finalize(context, module);
        return Task.CompletedTask;
    }
}
