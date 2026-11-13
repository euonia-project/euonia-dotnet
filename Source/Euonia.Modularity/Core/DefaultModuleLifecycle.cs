namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 调用模块的 <see cref="IModuleContext.OnApplicationInitialization"/> 生命周期逻辑。
/// </summary>
public class OnApplicationInitializationModuleLifecycle : ModuleLifecycleBase
{
    /// <inheritdoc />
    public override void Initialize(ApplicationInitializationContext context, IModuleContext module)
    {
        module.OnApplicationInitialization(context);
    }

    /// <inheritdoc />
    public override Task InitializeAsync(ApplicationInitializationContext context, IModuleContext module)
    {
        return module.OnApplicationInitializationAsync(context);
    }
}

/// <summary>
/// 调用模块的 <see cref="IModuleContext.OnApplicationShutdown"/> 生命周期逻辑。
/// </summary>
public class OnApplicationShutdownModuleLifecycle : ModuleLifecycleBase
{
    /// <inheritdoc />
    public override void Finalize(ApplicationShutdownContext context, IModuleContext module)
    {
        module.OnApplicationShutdown(context);
    }

    /// <inheritdoc />
    public override Task FinalizeAsync(ApplicationShutdownContext context, IModuleContext module)
    {
        return module.OnApplicationShutdownAsync(context);
    }
}
