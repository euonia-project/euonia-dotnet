namespace Nerosoft.Euonia.Security;

/// <summary>
/// 按权限码声明资源访问策略的集合，是 <see cref="ScopeModel{T}.Declare"/> 的入参。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 未在 <see cref="For(string, ScopePolicy{T})"/> 中单独声明的权限码，
/// 一律使用模型必填的默认策略 <see cref="ScopeModel{T}.Policy"/>。
/// </para>
/// <para>
/// 同一个操作若能解析出<b>多个</b>都有策略的权限码，属配置歧义，
/// 会在启动期被拒绝——不允许「实际生效的是哪一个」靠猜。
/// </para>
/// </remarks>
public sealed class ScopePolicySet<T>
	where T : class
{
	private readonly Dictionary<string, ScopePolicy<T>> _policies = new(StringComparer.Ordinal);

	/// <summary>
	/// 为指定操作声明策略（等价于为该操作的默认键 <c>@read</c>/<c>@create</c>… 声明）。
	/// </summary>
	/// <param name="operation">业务操作名；可以是 <see cref="BusinessOperation"/> 的常量，也可以是宿主自定义的操作。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="operation"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当 <paramref name="operation"/> 使用了框架保留前缀时抛出。</exception>
	/// <remarks>
	/// 键由 <see cref="ScopeKeys.For(string)"/> 从操作名派生，因而落在保留命名空间内；
	/// 本方法不调用 <see cref="ScopeKeys.Validate(string)"/>，故派生出的 <c>@…</c> 键不会被当成非法权限码。
	/// </remarks>
	public ScopePolicySet<T> ForOperation(string operation, ScopePolicy<T> policy)
	{
		return Add(ScopeKeys.For(operation), policy);
	}

	/// <summary>
	/// 为指定权限码声明策略。
	/// </summary>
	/// <param name="code">权限码，需与 <c>[Permission]</c> 声明的码一致。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="code"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当权限码落在框架保留命名空间内，或同一权限码被重复声明时抛出。</exception>
	/// <remarks>
	/// 与 <see cref="ForOperation"/> 分开而不是合并成 <c>For(string, …)</c>：
	/// 「<c>read</c> 是操作名」与「<c>read</c> 是权限码」在字符串层面无法区分，
	/// 合并等于让调用方靠猜。框架刻意把两种意图写成两个方法。
	/// </remarks>
	public ScopePolicySet<T> For(string code, ScopePolicy<T> policy)
	{
		return Add(ScopeKeys.Validate(code), policy);
	}

	/// <summary>
	/// 登记一条按码声明的策略（不做保留码校验）。
	/// </summary>
	private ScopePolicySet<T> Add(string code, ScopePolicy<T> policy)
	{
		Check.EnsureNotNullOrWhiteSpace(code, nameof(code));
		Check.EnsureNotNull(policy, nameof(policy));
		Check.Ensure(!_policies.ContainsKey(code), "权限码 '{0}' 的策略被重复声明。", code);

		_policies[code] = policy;
		return this;
	}

	/// <summary>
	/// 尝试获取指定权限码上显式声明的策略。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <param name="policy">策略。</param>
	/// <returns>存在则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	internal bool TryGet(string code, out ScopePolicy<T> policy)
	{
		if (code == null)
		{
			policy = null;
			return false;
		}

		return _policies.TryGetValue(code, out policy);
	}

	/// <summary>
	/// 获取已显式声明策略的权限码集合。
	/// </summary>
	public IReadOnlyCollection<string> Codes => _policies.Keys;
}
