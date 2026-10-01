using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的配置入口：一次 <c>AddPermission(options => …)</c> 声明扫描范围、操作入口规则与可选断言。
/// </summary>
/// <remarks>
/// <para>
/// 推荐形态——绝大多数应用只需要一个配置回调：
/// </para>
/// <code>
/// services.AddPermission(permission =>
/// {
///     permission.Scan(typeof(Order).Assembly);
///     permission.OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute));
/// });
/// </code>
/// <para>
/// <see cref="Scan"/> 可多次调用（程序集按并集累积）。规则只有两个入口：<see cref="OnAttributeOrName"/>
/// 覆盖「打特性或按推导出的名字」这一最常见约定，<see cref="OnMethod"/> 是任意谓词的逃生舱。
/// </para>
/// <para>
/// 两种特殊场景用显式断言表达：
/// </para>
/// <list type="bullet">
/// <item><see cref="NoOperationCodes"/>：本应用没有方法级权限码。</item>
/// <item><see cref="NoModels"/>：本应用没有任何权限模型与权限声明（用于零扫描范围的显式放行）。</item>
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
	private OperationCodeSourceBuilder _rules;

	/// <summary>
	/// 已登记的扫描程序集（按并集累积、幂等去重）。
	/// </summary>
	private readonly HashSet<Assembly> _assemblies = [];

	/// <summary>
	/// 获取是否声明了「本应用没有方法级权限码」。
	/// </summary>
	internal bool NoCodesAsserted { get; private set; }

	/// <summary>
	/// 显式指定的权限码来源；未指定时为 <see langword="null"/>。
	/// </summary>
	private IPermissionCodeSource _source;

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
	internal OperationCodeSourceBuilder Rules => _rules;

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
	/// 断言本应用没有方法级权限码：方法上的 <see cref="PermissionAttribute"/> 不参与判定。
	/// </summary>
	/// <returns>当前配置，便于链式调用。</returns>
	/// <remarks>
	/// 这是显式选择而非默认值：方法上的 <see cref="PermissionAttribute"/> 与类型级声明不同，
	/// 它必须由本来源声明了入口规则才会被收集，否则形同虚设——所以「没有方法级权限码」要说出来。
	/// </remarks>
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
	/// 指定来源后不能再声明入口规则（<see cref="OnAttributeOrName"/>、<see cref="OnMethod"/>）——
	/// 两者是互斥的来源形态。
	/// </remarks>
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
	internal IPermissionCodeSource ExplicitSource => _source;

	/// <summary>
	/// 确保规则构造器已创建。
	/// </summary>
	private OperationCodeSourceBuilder EnsureRules()
	{
		return _rules ??= OperationCodeSource.Create();
	}
}
