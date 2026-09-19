using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限的判定入口：下推到查询、内存过滤、单行判定与审计。
/// </summary>
/// <remarks>
/// <para>
/// 本类只消费 <see cref="CompiledScopePolicy{T}"/>，不自行解释策略——
/// 因此下列所有方法给出的结论必然一致。
/// </para>
/// <para>
/// <b>不可翻译即失败</b>：<see cref="Apply{T}(IQueryable{T}, CompiledScopePolicy{T})"/> 只做表达式下推，
/// 绝不回落到客户端求值。若某个提供程序无法翻译策略产生的表达式，应当抛出异常而不是把整表拉进内存。
/// </para>
/// </remarks>
public static class ScopeFilter
{
	/// <summary>
	/// 把数据权限下推为查询条件。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <param name="policy">已编译的策略。</param>
	/// <returns>仅包含当前用户可访问资源的查询。</returns>
	/// <exception cref="ArgumentNullException">当任一参数为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 等价于 <c>source.Where(Allow).Where(Deny 的否定)</c>：两个条件都保持表达式形态，
	/// 交给提供程序翻译成 <c>WHERE</c>。
	/// </remarks>
	public static IQueryable<T> Apply<T>(IQueryable<T> source, CompiledScopePolicy<T> policy)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));
		Check.EnsureNotNull(policy, nameof(policy));

		var query = source.Where(policy.Allow);

		if (!ScopePolicyNode<T>.IsConstantFalse(policy.Deny.Body))
		{
			var negated = Expression.Lambda<Func<T, bool>>(Expression.Not(policy.Deny.Body), policy.Deny.Parameters);
			query = query.Where(negated);
		}

		return query;
	}

	/// <summary>
	/// 在内存中过滤出当前用户可访问的资源。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <param name="policy">已编译的策略。</param>
	/// <returns>仅包含当前用户可访问资源的延迟序列。</returns>
	/// <exception cref="ArgumentNullException">当任一参数为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 用于数据已在内存中的场景（例如已加载的对象图）。大批量数据应优先使用
	/// <see cref="Apply{T}(IQueryable{T}, CompiledScopePolicy{T})"/> 下推到数据库。
	/// </remarks>
	public static IEnumerable<T> Filter<T>(IEnumerable<T> source, CompiledScopePolicy<T> policy)
		where T : class
	{
		Check.EnsureNotNull(source, nameof(source));
		Check.EnsureNotNull(policy, nameof(policy));

		return source.Where(policy.Evaluate);
	}

	/// <summary>
	/// 判定单个资源是否可访问。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="policy">已编译的策略。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	public static bool Allows<T>(T resource, CompiledScopePolicy<T> policy)
		where T : class
	{
		Check.EnsureNotNull(policy, nameof(policy));

		return policy.Evaluate(resource);
	}

	/// <summary>
	/// 解释某个资源为何可访问或被拒绝。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="resource">待判定的资源。</param>
	/// <param name="policy">策略（未经编译的原始策略，用于逐条给出命中路径）。</param>
	/// <param name="model">资源模型描述。</param>
	/// <param name="subjects">用户被授予的主体集合。</param>
	/// <returns>判定结果与命中的条件说明。</returns>
	/// <exception cref="ArgumentNullException">当任一参数为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 供审计与排障使用：逐叶子求值，列出成立的条件。这是诊断路径，不用于热路径判定。
	/// </remarks>
	public static ScopeDecision Explain<T>(T resource, ScopePolicy<T> policy, ScopeModelDescriptor model, ScopeSubjectSet subjects)
		where T : class
	{
		Check.EnsureNotNull(policy, nameof(policy));
		Check.EnsureNotNull(model, nameof(model));

		var compiled = ScopePolicyCompiler.Compile(policy, model, subjects);
		var allowed = compiled.Evaluate(resource);

		var matchedAllows = new List<string>();
		var matchedDenies = new List<string>();

		if (resource != null)
		{
			foreach (var leaf in ScopePolicyCompiler.CollectLeaves(policy, model, subjects))
			{
				if (!leaf.Condition.Compile()(resource))
				{
					continue;
				}

				if (leaf.IsDeny)
				{
					matchedDenies.Add(leaf.Description);
				}
				else
				{
					matchedAllows.Add(leaf.Description);
				}
			}
		}

		return new ScopeDecision(allowed, matchedAllows, matchedDenies);
	}
}
