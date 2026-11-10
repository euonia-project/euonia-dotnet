using System.Reflection;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 保存 HTTP 端点扫描显式指定的程序集。
/// </summary>
internal sealed class HttpAssemblyRegistry
{
	/// <summary>
	/// 初始化 <see cref="HttpAssemblyRegistry"/> 的新实例。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集；为 <c>null</c> 时使用空集合。</param>
	public HttpAssemblyRegistry(IEnumerable<Assembly>? assemblies)
	{
		Assemblies = assemblies?.Where(assembly => assembly != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
	}

	/// <summary>
	/// 获取显式指定的程序集。
	/// </summary>
	public IReadOnlyList<Assembly> Assemblies { get; }
}
