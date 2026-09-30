using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 多次 <c>AddPermission</c> 的累积结果：并集的权限码来源与程序集。
/// </summary>
internal sealed class PermissionModelSetup
{
	private readonly List<IPermissionCodeSource> _sources = [];
	private readonly List<Assembly> _assemblies = [];

	/// <summary>
	/// 累积一个权限码来源；同一来源实例按幂等处理。
	/// </summary>
	public void AddSource(IPermissionCodeSource codeSource)
	{
		ArgumentNullException.ThrowIfNull(codeSource);

		if (!_sources.Any(source => ReferenceEquals(source, codeSource)))
		{
			_sources.Add(codeSource);
		}
	}

	/// <summary>
	/// 累积扫描程序集；同一程序集按幂等处理。
	/// </summary>
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
	/// 合并后的扫描程序集。
	/// </summary>
	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	/// <summary>
	/// 获取是否已由使用方显式断言没有任何权限模型与权限声明（供启动期校验读取）。
	/// </summary>
	public bool NoModelsAsserted { get; set; }

	/// <summary>
	/// 获取或设置最近一次成功构建的注册表；输入未变时重复注册直接复用它。
	/// </summary>
	/// <remarks>
	/// 全量构建要重扫全部程序集并逐条编译校验模型，成本不低——同一组输入反复
	/// <c>AddPermission</c>（例如多个模块都传同一个来源实例与同一个程序集）应当是空操作。
	/// 构建抛出时不会记录，失败的调用下次照常重建。
	/// </remarks>
	public ScopeModelRegistry LastRegistry { get; set; }

	/// <summary>
	/// 判断给定累积输入是否与最近一次构建时相同（来源实例、程序集、操作词汇全部一致）。
	/// </summary>
	public bool SameAsLastBuild(IPermissionCodeSource source, IReadOnlyCollection<Assembly> assemblies)
	{
		if (LastRegistry == null)
		{
			return false;
		}

		var sourcesMatch = _sources.Count switch
		{
			0 => source is EmptyCodeSource,
			1 => ReferenceEquals(_sources[0], source),
			_ => source is CompositeCodeSource
		};

		return sourcesMatch
		       && _assemblies.Count == assemblies.Count
		       && _assemblies.All(assemblies.Contains);
	}

	/// <summary>
	/// 累积的贡献中是否存在权限模型或权限码声明。
	/// </summary>
	public bool HasDeclarations(ScopeModelRegistry registry)
	{
		return registry.HasDeclarations
		       || Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.HasPermissionDeclarations(Assemblies, CodeSource);
	}
}
