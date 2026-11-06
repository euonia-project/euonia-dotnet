using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块生命周期的选项。
/// </summary>
public class ModuleLifecycleOptions
{
	/// <summary>
	/// 获取生命周期。
	/// </summary>
	public ITypeList<IModuleLifecycle> Lifecycle { get; } = new TypeList<IModuleLifecycle>();
}