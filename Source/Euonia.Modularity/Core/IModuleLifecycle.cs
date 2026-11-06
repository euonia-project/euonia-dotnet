namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义模块生命周期的接口。
/// </summary>
public interface IModuleLifecycle : ITransientDependency
{
    /// <summary>
    /// 初始化模块。
    /// </summary>
    /// <param name="context">应用程序初始化上下文，提供模块初始化所需的服务提供程序。</param>
    /// <param name="module">当前正在初始化的模块实例。</param>
    void Initialize(ApplicationInitializationContext context, IModuleContext module);

    /// <summary>
    /// 释放模块。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <param name="module">模块上下文。</param>
    void Finalize(ApplicationShutdownContext context, IModuleContext module);
}
