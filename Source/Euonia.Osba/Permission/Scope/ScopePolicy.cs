using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限策略：描述「什么样的资源可以被访问」的组合表达式。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 策略是一个闭合的组合子集合，通过 <see cref="Self"/>、<see cref="Grant"/>、<see cref="All"/>、
/// <see cref="Any"/>、<see cref="Deny"/>、<see cref="Where"/> 构造，不支持自定义派生。
/// </para>
/// <para>
/// <b>判定语义</b>：策略编译为 <c>(Allow, Deny)</c> 一对表达式，
/// 最终判定为 <c>Allow &amp;&amp; !Deny</c>。
/// </para>
/// <para>
/// <b>Deny 一律上浮</b>：策略树中任意位置出现的 <see cref="Deny"/> 都对整个策略生效，
/// 相当于防火墙式的「拒绝优先」。因此在 <see cref="Any"/> 的分支里写 <see cref="Deny"/>，
/// 也会作用于整体，而不是只影响该分支。这是有意的保守选择。
/// </para>
/// </remarks>
public abstract class ScopePolicy<T>
	where T : class
{
	/// <summary>
	/// 防止外部派生。
	/// </summary>
	private protected ScopePolicy()
	{
	}

	/// <summary>
	/// 「本人可访问」：等价于 <c>Grant(<see cref="ScopeDimensions.Owner"/>)</c>。
	/// </summary>
	/// <returns>策略。</returns>
	/// <remarks>
	/// 本方法并不特殊，它只是 <see cref="Grant"/> 的便捷写法。是否成立取决于
	/// <see cref="IScopeSubjectResolver"/> 是否授予了 owner 维度——因此「本人数据」也可以被撤销。
	/// </remarks>
	public static ScopePolicy<T> Self()
	{
		return Grant(ScopeDimensions.Owner);
	}

	/// <summary>
	/// 「资源在该维度上的值，属于用户在该维度上被授予的集合」。
	/// </summary>
	/// <param name="dimension">维度名，必须已在本资源模型的 <see cref="ScopeModelBuilder{T}.Map"/> 中声明。</param>
	/// <returns>策略。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="dimension"/> 为 <see langword="null"/> 时抛出。</exception>
	public static ScopePolicy<T> Grant(string dimension)
	{
		Check.EnsureNotNullOrWhiteSpace(dimension, nameof(dimension));
		return new GrantScopePolicy<T>(dimension);
	}

	/// <summary>
	/// 逻辑「与」：所有分支的允许条件都成立，且没有任何分支的拒绝条件成立。
	/// </summary>
	/// <param name="policies">子策略。</param>
	/// <returns>策略。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policies"/> 为 <see langword="null"/> 时抛出。</exception>
	public static ScopePolicy<T> All(params ScopePolicy<T>[] policies)
	{
		Check.EnsureNotNull(policies, nameof(policies));
		Check.Ensure(policies.Length > 0, "All 至少需要一个子策略；「无约束」必须显式写成 Where(_ => true)。");
		Check.Ensure(policies.All(policy => policy != null), "All 的子策略不能为 null。");

		return new AllScopePolicy<T>(policies);
	}

	/// <summary>
	/// 逻辑「或」：任一分支的允许条件成立，且没有任何分支的拒绝条件成立。
	/// </summary>
	/// <param name="policies">子策略。</param>
	/// <returns>策略。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policies"/> 为 <see langword="null"/> 时抛出。</exception>
	public static ScopePolicy<T> Any(params ScopePolicy<T>[] policies)
	{
		Check.EnsureNotNull(policies, nameof(policies));
		Check.Ensure(policies.Length > 0, "Any 至少需要一个子策略；「无约束」必须显式写成 Where(_ => true)。");
		Check.Ensure(policies.All(policy => policy != null), "Any 的子策略不能为 null。");

		return new AnyScopePolicy<T>(policies);
	}

	/// <summary>
	/// 拒绝：子策略的允许条件一旦成立即拒绝访问，且压过所有允许条件。
	/// </summary>
	/// <param name="policy">产生拒绝条件的子策略。</param>
	/// <returns>策略。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	public static ScopePolicy<T> Deny(ScopePolicy<T> policy)
	{
		Check.EnsureNotNull(policy, nameof(policy));

		// Deny 是否决（override），不是布尔取反；嵌套 Deny 既无意义又容易掩盖笔误
		Check.Ensure(policy is not DenyScopePolicy<T>, "Deny 不可嵌套。Deny 表示「否决」，不是布尔取反；需要取反请用 Where(x => !...)。");

		return new DenyScopePolicy<T>(policy);
	}

	/// <summary>
	/// 直接给出一个谓词条件（ABAC 逃生舱）。
	/// </summary>
	/// <param name="predicate">谓词，例如 <c>x =&gt; x.Amount &lt; 100000</c>。</param>
	/// <returns>策略。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 谓词必须是<b>表达式</b>，以便与 <see cref="Grant"/> 一起下推到数据库。若谓词引用了用户侧的值，
	/// 应当在构造策略时把该值捕获进闭包（会被烘成查询常量），而不是在谓词内部再去解析用户信息。
	/// </remarks>
	public static ScopePolicy<T> Where(Expression<Func<T, bool>> predicate)
	{
		Check.EnsureNotNull(predicate, nameof(predicate));
		return new WhereScopePolicy<T>(predicate);
	}

	/// <summary>
	/// 将本策略归约为允许/拒绝一对表达式（body 层，共用同一规范参数）。
	/// </summary>
	/// <param name="context">编译上下文。</param>
	/// <returns>归约结果。</returns>
	internal abstract ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context);

	/// <summary>
	/// 收集本策略树中的叶子条件，用于审计（<see cref="ScopeFilter.Explain"/>）。
	/// </summary>
	/// <param name="context">编译上下文。</param>
	/// <param name="negated">当前是否处于 <see cref="Deny"/> 之下。</param>
	/// <param name="traces">收集结果。</param>
	internal abstract void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces);

	/// <summary>
	/// 获取本策略的可读描述，用于启动校验的报错信息与审计。
	/// </summary>
	/// <returns>策略描述。</returns>
	public abstract override string ToString();
}
