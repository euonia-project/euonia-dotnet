using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 定义资源在数据权限各维度上的取值来源。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 映射使用<b>表达式</b>而非委托，这是本设计能够下推到数据库的前提：表达式可以被翻译成
/// <c>WHERE</c> 条件，委托不能。
/// </para>
/// <para>
/// 映射必须是资源的<b>自身数据</b>（通常是数据列；<see cref="MapMany"/> 声明的子表维度则是它的子表数据），
/// 而不是固化标签：值一变，其归属立即变化。
/// </para>
/// <para>
/// 维度取值的形态有两种：<see cref="Map"/> 的单值，与 <see cref="MapMany"/> 的集合。
/// 二者在策略里没有区别——都是 <see cref="ScopePolicy{T}.Grant"/>，语义统一为
/// 「资源在该维度上的取值集合与用户被授予的集合相交非空」。
/// </para>
/// </remarks>
public sealed class ScopeModelBuilder<T> : IScopeModelBuilder
	where T : class
{
	private readonly Dictionary<string, LambdaExpression> _dimensions = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, LambdaExpression> _classifications = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 声明资源在指定维度上的取值来源。
	/// </summary>
	/// <param name="dimension">维度名，例如 <see cref="ScopeDimensions.Dept"/>。</param>
	/// <param name="selector">取值表达式，例如 <c>x =&gt; x.DeptId</c>。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="dimension"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="ArgumentNullException">当 <paramref name="selector"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一维度被重复声明时抛出。</exception>
	public ScopeModelBuilder<T> Map(string dimension, Expression<Func<T, string>> selector)
	{
		Check.EnsureNotNullOrWhiteSpace(dimension, nameof(dimension));
		Check.EnsureNotNull(selector, nameof(selector));
		Check.Ensure(!_dimensions.ContainsKey(dimension), Resources.IDS_SCOPE_DIMENSION_DUPLICATED, dimension, typeof(T).Name);

		_dimensions[dimension] = selector;
		return this;
	}

	/// <summary>
	/// 声明一个<b>子表维度</b>：资源在该维度上的取值来自子表（关系表），而非资源行自身的列。
	/// </summary>
	/// <param name="dimension">维度名，例如 <see cref="ScopeDimensions.Member"/>。</param>
	/// <param name="selector">取值表达式，例如 <c>x =&gt; x.Members.Select(m =&gt; m.UserId)</c>。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="dimension"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="ArgumentNullException">当 <paramref name="selector"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一维度被重复声明（含被 <see cref="Map"/> 声明过）时抛出。</exception>
	/// <remarks>
	/// <para>
	/// <see cref="Map"/> 声明的维度是<b>单值</b>的（<c>x.DeptId</c>），条件是「该值 ∈ 用户被授予的集合」；
	/// 本方法声明的维度是<b>集合</b>的（<c>x.Members.Select(m =&gt; m.UserId)</c>），条件是
	/// 「该集合与用户被授予的集合<b>有交集</b>」。两者共同构成同一套语义：
	/// 资源在该维度上的取值集合与授予集合相交非空（单值是集合的退化情形）。
	/// </para>
	/// <para>
	/// 典型场景是「查询我加入的团队 / 家庭 / 组织」——成员关系存在子表里，本方法让它直接参与行级判定，
	/// 下推为 <c>EXISTS</c> 子查询，无需把成员关系反向展开成扁平 id 集合。
	/// 子表行的属性（<c>status</c>、<c>expires_at</c>）可直接写在选择器里
	/// （<c>x =&gt; x.Members.Where(m =&gt; m.Status == "active").Select(m =&gt; m.UserId)</c>），
	/// 因此「只算有效成员」这类条件由数据库实时求值，而不是解析期的一次性快照。
	/// </para>
	/// <para>
	/// <b>取值的来源不再是资源行自身的列</b>，这带来一条使用要求：单行判定（<c>IScopeGuard.Allows</c> /
	/// <c>AllowsObject</c>，以及工厂边界的自动判定）在内存中求值同一个表达式，
	/// 因此实例上对应的子集合<b>必须已加载</b>；未加载时会以明确异常失败，而不是静默拒绝。
	/// 查询路径（<c>IScopeGuard.Apply</c>）由数据库求值，不受此限。
	/// </para>
	/// </remarks>
	public ScopeModelBuilder<T> MapMany(string dimension, Expression<Func<T, IEnumerable<string>>> selector)
	{
		Check.EnsureNotNullOrWhiteSpace(dimension, nameof(dimension));
		Check.EnsureNotNull(selector, nameof(selector));
		Check.Ensure(!_dimensions.ContainsKey(dimension), Resources.IDS_SCOPE_DIMENSION_DUPLICATED, dimension, typeof(T).Name);

		_dimensions[dimension] = selector;
		return this;
	}

	/// <summary>
	/// 声明资源的分类属性（例如敏感级、密级）。
	/// </summary>
	/// <param name="name">分类名。</param>
	/// <param name="selector">取值表达式，例如 <c>x =&gt; x.Level</c>。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="ArgumentNullException">当 <paramref name="selector"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一分类被重复声明时抛出。</exception>
	/// <remarks>
	/// 分类属性<b>不参与授权</b>——它不表示「用户被授予了什么」，而是资源的固有属性，
	/// 只能被 <see cref="ScopePolicy{T}.Where"/> 之类的谓词引用（例如「没有相应密级就一律拒绝」）。
	/// </remarks>
	public ScopeModelBuilder<T> Classify(string name, Expression<Func<T, object>> selector)
	{
		Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
		Check.EnsureNotNull(selector, nameof(selector));
		Check.Ensure(!_classifications.ContainsKey(name), Resources.IDS_SCOPE_CLASSIFICATION_DUPLICATED, name, typeof(T).Name);

		_classifications[name] = selector;
		return this;
	}

	/// <inheritdoc />
	IReadOnlyDictionary<string, LambdaExpression> IScopeModelBuilder.Dimensions => _dimensions;

	/// <inheritdoc />
	IReadOnlyDictionary<string, LambdaExpression> IScopeModelBuilder.Classifications => _classifications;
}
