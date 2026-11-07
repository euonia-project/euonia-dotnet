namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块生命周期基类。
/// </summary>
public abstract class ModuleLifecycleBase : IModuleLifecycle
{
    /// <inheritdoc/>
    public virtual void Initialize(ApplicationInitializationContext context, IModuleContext module)
    {
    }

    /// <inheritdoc/>
    public virtual Task InitializeAsync(ApplicationInitializationContext context, IModuleContext module)
    {
        Initialize(context, module);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual void Finalize(ApplicationShutdownContext context, IModuleContext module)
    {
    }

    /// <inheritdoc/>
    public virtual Task FinalizeAsync(ApplicationShutdownContext context, IModuleContext module)
    {
        Finalize(context, module);
        return Task.CompletedTask;
    }
}
