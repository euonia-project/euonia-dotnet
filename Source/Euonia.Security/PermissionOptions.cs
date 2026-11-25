using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的配置入口：一次 <c>AddPermission(options => …)</c> 声明扫描范围、可选的要求来源与断言。
/// </summary>
/// <remarks>
/// <para>
/// 推荐形态——绝大多数应用只需要一次 <see cref="Scan"/>：
/// </para>
/// <code>
/// services.AddPermission(permission => permission.Scan(typeof(Order).Assembly));
/// </code>
/// <para>
/// 权限码直接写在对类型或方法上的 <see cref="PermissionAttribute"/> 里，引擎按<b>声明</b>发现它们，
/// 因此新增权限码、新增操作都不需要回来改配置。方法级声明只在该方法被工厂调用时生效。
/// </para>
/// <para>
/// 只有三处例外需要额外声明：
/// </para>
/// <list type="bullet">
/// <item><see cref="Requirements"/>：权限码来自数据（数据库 / 配置表 / 既有鉴权系统），与声明取并集。</item>
/// <item><see cref="NoModels"/>：本应用没有任何权限模型与权限声明（用于零扫描范围的显式放行）。</item>
/// <item><see cref="Source"/> / <see cref="OnAttributeOrName"/> / <see cref="OnMethod"/> / <see cref="NoOperationCodes"/>：兼容入口，见各成员的备注。</item>
/// </list>
/// <para>
/// 未调用 <see cref="Scan"/> 且未断言 <see cref="NoModels"/> 时，启动期校验会拒绝——
/// 空扫描范围会让行级数据权限静默失效，这不是合法状态。
/// </para>
/// </remarks>
public sealed class PermissionOptions
{
	/// <summary>
	/// 方法入口规则构造器；未声明任何规则时为 <see langword="null"/>。
	/// </summary>
	private OperationCodeSourceBuilder? _rules;

	/// <summary>
	/// 已登记的扫描程序集（按并集累积、幂等去重）。
	/// </summary>
	private readonly HashSet<Assembly> _assemblies = [];

	/// <summary>
	/// 已登记的权限要求来源（按实例幂等去重，与内建声明取并集）。
	/// </summary>
	private readonly List<IPermissionRequirementSource> _requirementSources = [];

	/// <summary>
	/// 获取是否声明了「本应用没有方法级权限码」。
	/// </summary>
	internal bool NoCodesAsserted { get; private set; }

	/// <summary>
	/// 显式指定的权限码来源；未指定时为 <see langword="null"/>。
	/// </summary>
	private IPermissionCodeSource? _source;

	/// <summary>
	/// 获取是否声明了「本应用没有任何权限模型与权限声明」。
	/// </summary>
	internal bool NoModelsAsserted { get; private set; }

	/// <summary>
	/// 获取累积的扫描程序集。
	/// </summary>
	internal IReadOnlyCollection<Assembly> Assemblies => _assemblies;

	/// <summary>
	/// 获取规则构造器；尚未声明任何规则时为 <see langword="null"/>。
	/// </summary>
	internal OperationCodeSourceBuilder? Rules => _rules;

	/// <summary>
	/// 获取累积的权限要求来源。
	/// </summary>
	internal IReadOnlyCollection<IPermissionRequirementSource> RequirementSources => _requirementSources;

	/// <summary>
	/// 登记要扫描权限模型与权限声明的程序集；可多次调用，程序集按并集累积。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>当前配置，便于链式调用。</returns>
	public PermissionOptions Scan(params Assembly[] assemblies)
	{
		foreach (var assembly in assemblies ?? [])
		{
			if (assembly != null)
			{
				_assemblies.Add(assembly);
			}
		}

		return this;
	}

	/// <summary>
	/// 登记一个权限要求来源：权限码不在声明里、而在数据里（数据库、配置表、既有鉴权系统）时使用。
	/// </summary>
	/// <param name="source">权限要求来源，按「类型 + 将被调用的方法（可选）」回答要求。</param>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// 来源是<b>增量</b>的：它与写在类型/方法上的 <see cref="PermissionAttribute"/> 取并集，
	/// 也与其他已登记来源取并集（按「码 + 角色」去重）。不登记来源时闸门同样工作，
	/// 只是要求全部来自声明。
	/// </para>
	/// <para>
	/// 同一个来源实例重复登记是幂等的。需要按 <c>(类型, 操作)</c> 回答时，用
	/// <see cref="PermissionRequirements.Adapt"/> 适配后登记。
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/> 时抛出。</exception>
	public PermissionOptions Requirements(IPermissionRequirementSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		if (!_requirementSources.Any(existing => ReferenceEquals(existing, source)))
		{
			_requirementSources.Add(source);
		}

		return this;
	}

	/// <summary>
	/// 兼容入口：本应用没有方法级权限码的断言。
	/// </summary>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// 不改变判定行为（方法级 <see cref="PermissionAttribute"/> 总是生效），但算作一次显式配置：
	/// 「空回调 + 本断言」因此不会被判为「配置是空的」。
	/// </remarks>
	[Obsolete("兼容入口：不改变判定行为；仅为兼容既有配置保留（仍算一次显式配置）。")]
	public PermissionOptions NoOperationCodes()
	{
		NoCodesAsserted = true;
		return this;
	}

	/// <summary>
	/// 断言本应用没有任何权限模型与权限声明；仅用于没有任何扫描范围的场景。
	/// </summary>
	/// <returns>当前配置，便于链式调用。</returns>
	public PermissionOptions NoModels()
	{
		NoModelsAsserted = true;
		return this;
	}

	/// <summary>
	/// 声明「打上指定特性或按其推导的约定名匹配的方法，即该操作的入口」。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="namePrefix">特性名中要剥离的前缀；为 <see langword="null"/> 时不去前缀。</param>
	/// <param name="attributeTypes">入口特性类型；一个操作可以有多个（如 Create 与 Insert 同义）。</param>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// 兼容入口：声明的规则会作为一份权限码来源参与要求并集。方法级 <see cref="PermissionAttribute"/>
	/// 总是生效，新代码无需调用本方法。
	/// </remarks>
	[Obsolete("兼容入口：声明操作入口规则。方法级 [Permission] 总是生效，新代码无需调用。")]
	public PermissionOptions OnAttributeOrName(string operation, string namePrefix, params Type[] attributeTypes)
	{
		EnsureRules().OnAttributeOrName(operation, namePrefix, attributeTypes);
		return this;
	}

	/// <summary>
	/// 声明任意入口判定规则，用于内置约定都不适用的框架。
	/// </summary>
	/// <param name="operation">业务操作名。</param>
	/// <param name="predicate">判定方法是否为该操作入口的谓词。</param>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// 兼容入口：声明的谓词会作为一份权限码来源参与要求并集。方法级 <see cref="PermissionAttribute"/>
	/// 总是生效，新代码无需调用本方法。
	/// </remarks>
	[Obsolete("兼容入口：声明任意入口判定谓词。方法级 [Permission] 总是生效，新代码无需调用。")]
	public PermissionOptions OnMethod(string operation, Func<MethodInfo, bool> predicate)
	{
		EnsureRules().OnMethod(operation, predicate);
		return this;
	}

	/// <summary>
	/// 直接指定权限码来源；规则不在代码里（例如来自数据库）时使用。
	/// </summary>
	/// <param name="source">权限码来源，回答「某类型在某操作上声明了哪些权限码」。</param>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// 兼容入口：按 <c>(类型, 操作)</c> 回答的来源会经由 <see cref="PermissionRequirements.Adapt"/>
	/// 参与要求并集。需要按「类型 + 将被调用的方法」回答时，请改用 <see cref="Requirements"/>。
	/// 指定来源后不能再声明入口规则（<see cref="OnAttributeOrName"/>、<see cref="OnMethod"/>）。
	/// </remarks>
	[Obsolete("兼容入口：按 (类型, 操作) 指定权限码来源。请使用 Requirements(IPermissionRequirementSource)。")]
	public PermissionOptions Source(IPermissionCodeSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		Check.Ensure(_source == null, Resources.IDS_PERMISSION_SOURCE_DUPLICATED);

		_source = source;
		return this;
	}

	/// <summary>
	/// 获取显式指定的权限码来源；未指定时为 <see langword="null"/>。
	/// </summary>
	internal IPermissionCodeSource? ExplicitSource => _source;

	/// <summary>
	/// 确保规则构造器已创建。
	/// </summary>
	private OperationCodeSourceBuilder EnsureRules()
	{
		return _rules ??= OperationCodeSource.Create();
	}
}
