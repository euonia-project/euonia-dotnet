using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 权限体系在一次注册过程中的累积状态：并集的权限码来源 / 要求来源与扫描程序集、显式断言、
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
	private readonly List<IPermissionRequirementSource> _requirementSources = [];
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
	/// 累积一个权限要求来源；同一来源实例按幂等处理。
	/// </summary>
	/// <param name="requirementSource">权限要求来源。</param>
	public void AddRequirementSource(IPermissionRequirementSource requirementSource)
	{
		ArgumentNullException.ThrowIfNull(requirementSource);

		if (!_requirementSources.Any(source => ReferenceEquals(source, requirementSource)))
		{
			_requirementSources.Add(requirementSource);
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
	/// 合并后的权限码来源：按 <c>(类型, 操作)</c> 回答的只读视图。
	/// </summary>
	public IPermissionCodeSource CodeSource => _sources switch
	{
		[] => EmptyCodeSource.Instance,
		[var single] => single,
		_ => new CompositeCodeSource([.. _sources])
	};

	/// <summary>
	/// 合并后的权限要求来源：内建声明 ∪ 登记的自定义来源 ∪ 权限码来源的适配器。
	/// </summary>
	/// <remarks>
	/// 内建声明永远参与（<see cref="AttributeRequirementSource"/>），因此即使宿主不登记任何来源，
	/// 闸门也能按「类型 + 被调用的方法」收集要求；自定义来源与权限码来源是<b>增量</b>的。
	/// </remarks>
	public IPermissionRequirementSource RequirementSource
	{
		get
		{
			var sources = new List<IPermissionRequirementSource>(_requirementSources.Count + _sources.Count + 1)
			{
				AttributeRequirementSource.Instance
			};

			sources.AddRange(_requirementSources);

			// 按 (类型, 操作) 回答的来源没有方法上下文：适配后忽略 method，只作为并集的一份参与判定
			sources.AddRange(_sources.Select(PermissionRequirements.Adapt));

			return new CompositeRequirementSource([.. sources]);
		}
	}

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
	/// 声明了权限模型（<see cref="IScopeModel{T}"/>）、扫描到 <see cref="PermissionAttribute"/> 声明，
	/// 或登记了任一权限要求 / 权限码来源时为 <see langword="true"/>——它们都需要从授权数据解析
	/// 「用户被授予了什么」。
	/// </remarks>
	public bool RequiresSubjectResolver { get; set; }

	/// <summary>
	/// 获取最近一次成功构建的注册表；尚未构建时为 <see langword="null"/>。
	/// </summary>
	public ScopeModelRegistry? LastRegistry { get; private set; }

	/// <summary>
	/// 最近一次成功构建时的程序集集合；用于判断下一次注册是否真的改变了输入。
	/// </summary>
	private Assembly[]? _lastBuiltAssemblies;

	/// <summary>
	/// 尝试复用上一次成功构建的注册表。
	/// </summary>
	/// <param name="assemblies">本次要扫描的程序集（累积后的并集）。</param>
	/// <param name="registry">复用到的注册表。</param>
	/// <returns>输入与上次构建完全一致、可复用时返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 注册表是「扫了哪些程序集」的纯函数（来源只影响运行期判定，不参与建模），
	/// 因此复用判据也就只看程序集：全量构建要重扫并逐条编译校验模型，成本不低——
	/// 同一组程序集反复 <c>AddPermission</c>（多个模块各注册一次）应当是空操作。
	/// 构建抛出时不会记录，失败的调用下次照常重建。
	/// </remarks>
	public bool TryReuse(Assembly[] assemblies, [MaybeNullWhen(false)] out ScopeModelRegistry registry)
	{
		registry = LastRegistry;

		return registry != null
		       && _lastBuiltAssemblies is { } lastBuilt
		       && lastBuilt.Length == assemblies.Length
		       && lastBuilt.All(assemblies.Contains);
	}

	/// <summary>
	/// 记录一次成功构建。
	/// </summary>
	/// <param name="registry">构建出的注册表。</param>
	/// <param name="assemblies">本次扫描的程序集。</param>
	public void MarkBuilt(ScopeModelRegistry registry, Assembly[] assemblies)
	{
		LastRegistry = registry;
		_lastBuiltAssemblies = assemblies;
	}

	/// <summary>
	/// 累积的贡献中是否存在权限模型或权限声明。
	/// </summary>
	/// <param name="registry">当前构建的模型注册表。</param>
	/// <returns>存在声明则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 只读三种输入：权限模型、扫描到的 <see cref="PermissionAttribute"/> 声明、登记的来源。
	/// <see cref="EmptyCodeSource"/> 是「本应用没有方法级权限码」的显式断言而不是一份来源，
	/// 因此它不算作「登记了来源」，否则空断言会强行要求一个用不上的主体解析器。
	/// </remarks>
	public bool HasDeclarations(ScopeModelRegistry registry)
	{
		return registry.HasDeclarations
		       || _requirementSources.Count > 0
		       || _sources.Exists(static source => source is not EmptyCodeSource)
		       || PermissionDeclarationScanner.HasDeclarations(Assemblies);
	}
}
