namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块容器的契约接口。
/// </summary>
public interface IModuleContainer
{
	/// <summary>
	/// 获取已注册的模块。
	/// </summary>
    IReadOnlyList<IModuleDescriptor> Modules { get; }
}
