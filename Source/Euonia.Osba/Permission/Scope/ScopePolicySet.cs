namespace Nerosoft.Euonia.Osba;

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
	/// <param name="operation">业务操作。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	public ScopePolicySet<T> For(BusinessOperation operation, ScopePolicy<T> policy)
	{
		// 走内部入口：框架自己的默认键（@read/@create/…）落在保留命名空间内，
		// 不能经过「拒绝保留码」的用户校验
		return Add(ScopeKeys.For(operation), policy);
	}

	/// <summary>
	/// 为指定权限码声明策略。
	/// </summary>
	/// <param name="code">权限码，需与 <c>[Permission]</c> 声明的码一致。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一权限码被重复声明时抛出。</exception>
	public ScopePolicySet<T> For(string code, ScopePolicy<T> policy)
	{
		// 保留命名空间只允许框架自身（For(BusinessOperation)）使用
		ScopeKeys.Validate(code);

		return Add(code, policy);
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
