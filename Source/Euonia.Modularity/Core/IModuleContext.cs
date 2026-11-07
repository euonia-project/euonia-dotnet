namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块上下文的契约接口。
/// </summary>
/// <remarks>
/// 所有异步方法都提供默认实现：未重写异步方法时会回退到对应的同步方法。
/// 因此模块可以只实现同步版本，也可以在需要异步操作时仅重写异步版本。
/// </remarks>
public interface IModuleContext
{
    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之前调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void AheadConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// 配置当前模块的服务。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之后调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    void AfterConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之前异步调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="AheadConfigureServices"/>。</remarks>
    Task AheadConfigureServicesAsync(ServiceConfigurationContext context)
    {
        AheadConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 异步配置当前模块的服务。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="ConfigureServices"/>。</remarks>
    Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        ConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 表示在所有模块的 <see cref="ConfigureServices"/> 执行之后异步调用的方法。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="AfterConfigureServices"/>。</remarks>
    Task AfterConfigureServicesAsync(ServiceConfigurationContext context)
    {
        AfterConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在应用程序初始化时执行配置。
    /// </summary>
    /// <param name="context">应用程序初始化上下文。</param>
    void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    /// <summary>
    /// 表示在应用程序关闭时执行的模块逻辑。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }

    /// <summary>
    /// 在应用程序初始化时异步执行配置。
    /// </summary>
    /// <param name="context">应用程序初始化上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="OnApplicationInitialization"/>。</remarks>
    Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnApplicationInitialization(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 表示在应用程序关闭时异步执行的模块逻辑。
    /// </summary>
    /// <param name="context">应用程序关闭上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    /// <remarks>未重写时回退到同步的 <see cref="OnApplicationShutdown"/>。</remarks>
    Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
    {
        OnApplicationShutdown(context);
        return Task.CompletedTask;
    }
}
