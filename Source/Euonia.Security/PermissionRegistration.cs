using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限体系在一次注册过程中的累积状态：并集的权限码来源与扫描程序集、显式断言、
/// 最近一次成功构建的注册表，以及供启动期校验读取的「是否需要授权数据解析器」。
/// </summary>
/// <remarks>
/// <para>
/// 多次 <c>AddPermission</c> 共享同一份状态：来源按并集合并，程序集按并集去重，
/// 输入未变时复用上一次的注册表。
/// </para>
/// <para>
/// 之所以不在注册服务的过程中直接检查解析器是否已注册：解析器通常在那之后才注册，
/// 在那里检查会误报。因此这里只记录「是否需要」，真正的检查放在首次解析
/// <c>IScopeGuard</c> 时的启动校验（容器已构建、注册顺序已确定）。
/// </para>
/// </remarks>
internal sealed class PermissionRegistration
{
	private readonly List<IPermissionCodeSource> _sources = [];
	private readonly List<Assembly> _assemblies = [];

	/// <summary>
	/// 累积一个权限码来源；同一来源实例按幂等处理。
	/// </summary>
	/// <param name="codeSource">权限码来源。</param>
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
	/// <param name="assemblies">要扫描的程序集。</param>
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
	/// 获取或设置一个值，指示是否必须注册 <see cref="IScopeSubjectResolver"/>。
	/// </summary>
	/// <remarks>
	/// 声明了权限模型（<see cref="IScopeModel{T}"/>）或使用了 <see cref="PermissionAttribute"/>
	/// 时为 <see langword="true"/>——两者都需要从授权数据解析「用户被授予了什么」。
	/// </remarks>
	public bool RequiresSubjectResolver { get; set; }

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
	/// 判断给定累积输入是否与最近一次构建时相同（来源实例与程序集全部一致）。
	/// </summary>
	/// <param name="source">当前的权限码来源。</param>
	/// <param name="assemblies">当前的扫描程序集。</param>
	/// <returns>相同则返回 <see langword="true"/>。</returns>
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
	/// <param name="registry">当前构建的模型注册表。</param>
	/// <returns>存在声明则返回 <see langword="true"/>。</returns>
	public bool HasDeclarations(ScopeModelRegistry registry)
	{
		return registry.HasDeclarations
		       || Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.HasPermissionDeclarations(Assemblies, CodeSource);
	}
}
