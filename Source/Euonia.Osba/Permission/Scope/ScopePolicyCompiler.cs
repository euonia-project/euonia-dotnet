using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限策略的<b>唯一编译出口</b>：把策略与用户主体集合编译成一对谓词。
/// </summary>
/// <remarks>
/// <para>
/// 所有判定路径（下推、内存过滤、单行判定、审计）都必须经由本类，这是「读过滤与写判定
/// 不可能漂移」这条不变式的保证。
/// </para>
/// <para>
/// 编译会把用户被授予的值烘成表达式常量，因此编译结果与<b>当次</b>的用户主体绑定；
/// 用户授权变化后需要重新编译（<see cref="IScopeGuard"/> 按请求缓存并负责这一点）。
/// </para>
/// </remarks>
public static class ScopePolicyCompiler
{
	/// <summary>
	/// 编译策略。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="policy">策略。</param>
	/// <param name="model">资源模型描述。</param>
	/// <param name="subjects">用户被授予的主体集合。</param>
	/// <returns>编译结果。</returns>
	/// <exception cref="ArgumentNullException">当任一参数为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当策略引用了模型中未映射的维度时抛出。</exception>
	public static CompiledScopePolicy<T> Compile<T>(ScopePolicy<T> policy, ScopeModelDescriptor model, ScopeSubjectSet subjects)
		where T : class
	{
		Check.EnsureNotNull(policy, nameof(policy));
		Check.EnsureNotNull(model, nameof(model));

		var context = new ScopeCompileContext<T>(model, subjects ?? ScopeSubjectSet.Empty);
		var node = policy.Reduce(context);

		return new CompiledScopePolicy<T>(context.Lambda(node.Allow), context.Lambda(node.Deny), node.HasAllow);
	}

	/// <summary>
	/// 收集策略树中的叶子条件，供审计使用。
	/// </summary>
	/// <typeparam name="T">资源类型。</typeparam>
	/// <param name="policy">策略。</param>
	/// <param name="model">资源模型描述。</param>
	/// <param name="subjects">用户被授予的主体集合。</param>
	/// <returns>叶子条件列表。</returns>
	internal static IReadOnlyList<ScopePolicyLeaf<T>> CollectLeaves<T>(ScopePolicy<T> policy, ScopeModelDescriptor model, ScopeSubjectSet subjects)
		where T : class
	{
		var context = new ScopeCompileContext<T>(model, subjects);
		var traces = new List<ScopePolicyLeaf<T>>();

		policy.CollectLeaves(context, false, traces);

		return traces;
	}
}
