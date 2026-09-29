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
	/// 当前累积输入的签名。
	/// </summary>
	/// <remarks>
	/// 两份清单都<b>只增不减</b>且按幂等去重（重复的来源实例与重复的程序集都不追加），
	/// 因此「计数」唯一确定当前集合——两个计数没变就意味着集合没变，不需要逐项比对。
	/// </remarks>
	public (int Sources, int Assemblies) Signature => (_sources.Count, _assemblies.Count);

	/// <summary>
	/// 是否已由使用方显式断言「本应用没有任何权限模型与 <see cref="PermissionAttribute"/> 声明」。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 与 <see cref="EmptyCodeSource.Instance"/> 是同一条规则：<b>空输入不是默认值，而是必须做出的显式选择</b>。
	/// 省略程序集时扫描范围为空，行级数据权限必然静默失效，而
	/// <see cref="PermissionSetup.RequiresSubjectResolver"/> 同时恒为 <see langword="false"/>，
	/// 让启动期校验一并短路——「能启动但什么都没生效」正是本断言要堵住的形态。
	/// </para>
	/// <para>
	/// 供 <c>provider.ValidatePermissionSetup()</c> 读取；置位入口是
	/// <c>services.AssertNoPermissionModels()</c>。
	/// </para>
	/// </remarks>
	public bool NoModelsAsserted { get; set; }

	/// <summary>
	/// 上一次成功完成重建时的输入签名；尚未成功重建过时为 <see langword="null"/>。
	/// </summary>
	/// <remarks>重建抛出（注册期校验失败）时不得记录——否则下一次调用会以为已建好而跳过校验。</remarks>
	public (int Sources, int Assemblies)? LastBuild { get; set; }

	/// <summary>
	/// 累积的贡献中是否存在权限模型或权限码声明。
	/// </summary>
	public bool HasDeclarations(ScopeModelRegistry registry)
	{
		var codeSource = CodeSource;

		return registry.HasDeclarations || Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.HasPermissionDeclarations(Assemblies, codeSource);
	}
}
