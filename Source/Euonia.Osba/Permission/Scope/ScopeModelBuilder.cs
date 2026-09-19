using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

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
/// 映射必须是资源的<b>自身数据</b>（通常是数据列），而不是固化标签：列值一变，其归属立即变化。
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
	/// <exception cref="ArgumentNullException">当参数为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一维度被重复声明时抛出。</exception>
	public ScopeModelBuilder<T> Map(string dimension, Expression<Func<T, string>> selector)
	{
		Check.EnsureNotNullOrWhiteSpace(dimension, nameof(dimension));
		Check.EnsureNotNull(selector, nameof(selector));
		Check.Ensure(!_dimensions.ContainsKey(dimension), "维度 '{0}' 在类型 '{1}' 的权限模型中重复声明。", dimension, typeof(T).Name);

		_dimensions[dimension] = selector;
		return this;
	}

	/// <summary>
	/// 声明资源的分类属性（例如敏感级、密级）。
	/// </summary>
	/// <param name="name">分类名。</param>
	/// <param name="selector">取值表达式，例如 <c>x =&gt; x.Level</c>。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="ArgumentNullException">当参数为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当同一分类被重复声明时抛出。</exception>
	/// <remarks>
	/// 分类属性<b>不参与授权</b>——它不表示「用户被授予了什么」，而是资源的固有属性，
	/// 只能被 <see cref="ScopePolicy{T}.Where"/> 之类的谓词引用（例如「没有相应密级就一律拒绝」）。
	/// </remarks>
	public ScopeModelBuilder<T> Classify(string name, Expression<Func<T, object>> selector)
	{
		Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
		Check.EnsureNotNull(selector, nameof(selector));
		Check.Ensure(!_classifications.ContainsKey(name), "分类 '{0}' 在类型 '{1}' 的权限模型中重复声明。", name, typeof(T).Name);

		_classifications[name] = selector;
		return this;
	}

	/// <inheritdoc />
	IReadOnlyDictionary<string, LambdaExpression> IScopeModelBuilder.Dimensions => _dimensions;

	/// <inheritdoc />
	IReadOnlyDictionary<string, LambdaExpression> IScopeModelBuilder.Classifications => _classifications;
}
