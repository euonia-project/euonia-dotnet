namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块上下文的契约接口。
/// </summary>
public interface IModuleContext
{
    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之前调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void AheadConfigureServices(ServiceConfigurationContext context);

    /// <summary>
    /// 配置当前模块的服务。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void ConfigureServices(ServiceConfigurationContext context);

    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之后调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void AfterConfigureServices(ServiceConfigurationContext context);

    /// <summary>
    /// 在应用程序初始化时执行配置。
    /// </summary>
    /// <param name="context">应用程序初始化上下文。</param>
    void OnApplicationInitialization(ApplicationInitializationContext context);

    /// <summary>
    /// 表示在应用程序关闭时执行的模块逻辑。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    void OnApplicationShutdown(ApplicationShutdownContext context);
}