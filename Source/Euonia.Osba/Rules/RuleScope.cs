namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 一次操作范围内生效的规则集合：附加规则、排除项，以及「跳过检查」声明。
/// </summary>
/// <remarks>
/// <para>
/// 由执行器按操作构建，并挂到目标对象的 <see cref="Rules"/> 上（<c>AttachOperationRules</c>），
/// 操作结束即卸下。它<b>绝不写入</b>按类型进程级共享的 <see cref="RuleManager"/>：
/// 后者被所有并发请求共用，写入会让请求之间互相看到对方的规则。
/// </para>
/// <para>
/// 生命周期与操作相同，且仅供「先构建、后使用」的单线程场景。
/// </para>
/// </remarks>
internal sealed class RuleScope
{
	/// <summary>
	/// 本次操作附加的规则。
	/// </summary>
	private readonly List<IRuleBase> _rules = [];

	/// <summary>
	/// 本次操作排除的规则类型。
	/// </summary>
	private readonly List<Type> _excludedTypes = [];

	/// <summary>
	/// 本次操作排除的规则名称，比较时忽略大小写。
	/// </summary>
	private readonly List<string> _excludedNames = [];

	/// <summary>
	/// 获取本次操作附加的规则。
	/// </summary>
	internal IReadOnlyList<IRuleBase> Rules => _rules;

	/// <summary>
	/// 获取一个值，指示本次操作是否跳过规则检查。
	/// </summary>
	internal bool SkipChecks { get; private set; }

	/// <summary>
	/// 附加一条规则。
	/// </summary>
	/// <param name="rule">要附加的规则。</param>
	internal void Add(IRuleBase rule)
	{
		_rules.Add(rule);
	}

	/// <summary>
	/// 排除指定类型的规则。
	/// </summary>
	/// <param name="ruleType">要排除的规则类型，按精确类型匹配（不含派生类型）。</param>
	internal void Exclude(Type ruleType)
	{
		_excludedTypes.Add(ruleType);
	}

	/// <summary>
	/// 按名称排除规则。
	/// </summary>
	/// <param name="ruleName">要排除的规则名称，忽略大小写。</param>
	internal void Exclude(string ruleName)
	{
		_excludedNames.Add(ruleName);
	}

	/// <summary>
	/// 声明本次操作跳过规则检查。
	/// </summary>
	internal void SkipAllChecks()
	{
		SkipChecks = true;
	}

	/// <summary>
	/// 判断规则是否被本次操作排除。
	/// </summary>
	/// <param name="rule">要判断的规则。</param>
	/// <returns>被排除则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// 类型按<b>精确</b>匹配而非可赋值性：否则一次 <c>BypassRule&lt;RuleBase&gt;()</c> 就会
	/// 连带排除框架自动注入的数据权限规则，把「排除一条」变成「关掉全部」。
	/// </remarks>
	internal bool IsExcluded(IRuleBase rule)
	{
		return _excludedTypes.Contains(rule.GetType())
		       || _excludedNames.Contains(rule.Name, StringComparer.OrdinalIgnoreCase);
	}
}
