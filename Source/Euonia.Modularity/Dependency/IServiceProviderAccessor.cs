namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// <see cref="IServiceProvider"/> 的访问器。
/// </summary>
public interface IServiceProviderAccessor
{
	/// <summary>
	/// 获取 <see cref="IServiceProvider"/> 实例。
	/// </summary>
    IServiceProvider ServiceProvider { get; }
}
