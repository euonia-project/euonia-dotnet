using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 已编译的数据权限策略：允许/拒绝一对谓词。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 这是数据权限的<b>单一真值来源</b>。下推与内存求值都基于这里的同一对表达式，
/// 因此「查询过滤掉了哪些行」与「单行判定放行哪些行」不可能得出不同结论。
/// </para>
/// <para>
/// 判定语义恒为 <c>Allow &amp;&amp; !Deny</c>。当策略只包含拒绝条件时，
/// <see cref="Allow"/> 恒真而 <see cref="HasAllow"/> 为 <see langword="false"/>。
/// </para>
/// <para>
/// 「不可能得出不同结论」的前提是两边求值的是<b>同一份数据</b>：下推由数据库求值，
/// 内存求值则由实例承载。子表维度（<see cref="ScopeModelBuilder{T}.MapMany"/>）的取值来自子集合，
/// 因此内存求值要求对象图完整（子集合已加载），否则明确失败——详见 <see cref="Evaluate"/>。
/// </para>
/// </remarks>
public sealed class CompiledScopePolicy<T>
	where T : class
{
	private readonly Lazy<Func<T, bool>> _allow;
	private readonly Lazy<Func<T, bool>> _deny;

	/// <summary>集合维度的加载探测与已编译的探测委托，按需编译：下推路径不做探测，不应付出编译开销。</summary>
	private readonly Lazy<(ScopeLoadGuard<T> Guard, Func<T, bool> IsMissing)[]> _loadGuards;

	internal CompiledScopePolicy(Expression<Func<T, bool>> allow, Expression<Func<T, bool>> deny, bool hasAllow, string scopeKey, IReadOnlyList<ScopeLoadGuard<T>> loadGuards)
	{
		Allow = allow;
		Deny = deny;
		HasAllow = hasAllow;
		ScopeKey = scopeKey;

		// 委托按需编译并缓存：下推路径根本不求值，不应付出编译开销
		_allow = new Lazy<Func<T, bool>>(() => Allow.Compile());
		_deny = new Lazy<Func<T, bool>>(() => Deny.Compile());

		var guards = loadGuards ?? [];
		_loadGuards = new Lazy<(ScopeLoadGuard<T> Guard, Func<T, bool> IsMissing)[]>(() => guards
			.Select(guard => (guard, guard.IsMissing.Compile()))
			.ToArray());
	}

	/// <summary>
	/// 获取本次编译使用的权限码（策略键）。
	/// </summary>
	/// <remarks>仅用于审计与排障；它<b>不会</b>出现在 <see cref="Allow"/> / <see cref="Deny"/> 中。</remarks>
	public string ScopeKey { get; }

	/// <summary>
	/// 允许条件；策略只含拒绝条件时它是恒真或恒假的常量，因此判断策略有没有提供允许条件必须看 <see cref="HasAllow"/>，
	/// 不能看表达式形状。
	/// </summary>
	/// <remarks>可直接交给 <c>IQueryable.Where</c> 下推到数据库。</remarks>
	public Expression<Func<T, bool>> Allow { get; }

	/// <summary>
	/// 拒绝条件；策略没有拒绝条件时它是恒假常量（归约会剔除恒假的拒绝项，不会留下 <c>x || false</c> 这类节点）。
	/// </summary>
	/// <remarks>
	/// 下推时应使用其否定形式：<c>source.Where(allow).Where(deny.Not())</c>。
	/// </remarks>
	public Expression<Func<T, bool>> Deny { get; }

	/// <summary>
	/// 获取策略是否提供了允许条件。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="false"/> 表示策略不提供任何允许条件，此时 <see cref="Allow"/> 取决于策略形状：
	/// <c>Any</c> 之下全是拒绝条件会归约为恒假（拒绝一切），<c>All</c> 之下则归约为恒真
	/// （除拒绝清单外全部放行）。两者都不是常见写法，辨识与取舍见 README §5.3。
	/// </remarks>
	public bool HasAllow { get; }

	/// <summary>
	/// 在内存中判定单个资源是否可访问。
	/// </summary>
	/// <param name="resource">待判定的资源。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <exception cref="InvalidOperationException">策略引用了子表维度，但实例上对应的子集合未加载（为空引用）时抛出。</exception>
	internal bool Evaluate(T resource)
	{
		if (resource == null)
		{
			return false;
		}

		EnsureEvaluable(resource);

		return _allow.Value(resource) && !_deny.Value(resource);
	}

	/// <summary>
	/// 在内存判定之前检查对象图是否完整：策略引用的子表维度，其子集合必须是已加载的。
	/// </summary>
	/// <param name="resource">待判定的资源；为 <see langword="null"/> 时直接返回。</param>
	/// <exception cref="InvalidOperationException">任一子表维度的子集合为空引用时抛出。</exception>
	/// <remarks>
	/// <para>
	/// 子表维度（见 <see cref="ScopeModelBuilder{T}.MapMany"/>）的取值来自子集合，而内存判定
	/// （<c>Allows</c> / <c>AllowsObject</c> / <c>Filter</c> / <c>Explain</c>）在实例上求值，
	/// 因此需要对象图完整。<b>未加载时明确失败，而不是判为拒绝</b>：后者会把「没加载」伪装成
	/// 「无权限」，写侧表现为合法用户被拒且毫无线索。
	/// </para>
	/// <para>
	/// 这是探测而非求值短路：即便策略本身会先短路到某个结论，对象图不完整也照样报错——
	/// 结论若建立在没加载的数据上，本身就是错的。
	/// </para>
	/// </remarks>
	internal void EnsureEvaluable(T resource)
	{
		if (resource == null || _loadGuards.Value.Length == 0)
		{
			return;
		}

		foreach (var (guard, isMissing) in _loadGuards.Value)
		{
			bool missing;

			try
			{
				missing = isMissing(resource);
			}
			catch (NullReferenceException)
			{
				// 中间导航为空（例如 x.Team.Members 而 x.Team 未加载）同样属于对象图不完整
				missing = true;
			}

			// 只有真的缺失时才拼消息：判定是热路径，探测本身不该产生分配
			if (missing)
			{
				throw new InvalidOperationException(MissingSubCollection(guard));
			}
		}
	}

	private static string MissingSubCollection(ScopeLoadGuard<T> guard)
	{
		return string.Format(
			Resources.IDS_SCOPE_SUBCOLLECTION_NOT_LOADED,
			guard.Dimension,
			typeof(T).Name,
			guard.Path);
	}
}
