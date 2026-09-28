using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 多次 <c>AddPermission</c> 的累积结果：并集的权限码来源与并集的程序集。
/// </summary>
internal sealed class PermissionModelSetup
{
	private readonly List<IPermissionCodeSource> _sources = [];
	private readonly List<Assembly> _assemblies = [];

	/// <summary>
	/// 累积一次贡献；同一来源实例与同一程序集按幂等处理。
	/// </summary>
	public void Add(IPermissionCodeSource codeSource, Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(codeSource);

		if (!_sources.Any(source => ReferenceEquals(source, codeSource)))
		{
			_sources.Add(codeSource);
		}

		AddAssemblies(assemblies);
	}

	/// <summary>
	/// 只累积程序集，不改变权限码来源；同一程序集按幂等处理。
	/// </summary>
	/// <remarks>供「模型分散在多个程序集、来源只有一处」的注册路径使用。</remarks>
	public void AddAssemblies(Assembly[] assemblies)
	{
		foreach (var assembly in assemblies ?? [])
		{
			if (assembly != null && !_assemblies.Contains(assembly))
			{
				_assemblies.Add(assembly);
			}
		}
	}

	/// <summary>
	/// 合并后的权限码来源。
	/// </summary>
	public IPermissionCodeSource CodeSource => _sources switch
	{
		[] => EmptyCodeSource.Instance,
		[var single] => single,
		_ => new CompositeCodeSource([.. _sources])
	};

	/// <summary>
	/// 合并后的程序集。
	/// </summary>
	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	/// <summary>
	/// 累积的贡献中是否存在权限模型或权限码声明。
	/// </summary>
	public bool HasDeclarations(ScopeModelRegistry registry)
	{
		var codeSource = CodeSource;

		return registry.HasDeclarations || Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.HasPermissionDeclarations(Assemblies, codeSource);
	}
}
