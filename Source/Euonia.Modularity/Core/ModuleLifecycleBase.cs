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
	public virtual void Finalize(ApplicationShutdownContext context, IModuleContext module)
	{
	}
}