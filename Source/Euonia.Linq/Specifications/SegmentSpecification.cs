using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个可用于过滤对象集合的规约。
/// </summary>
/// <typeparam name="TTarget">要过滤的目标类型。</typeparam>
/// <typeparam name="TProperty">属性类型。</typeparam>
/// <typeparam name="TValue">属性值的类型。</typeparam>
public class SegmentSpecification<TTarget, TProperty, TValue> : ISpecification<TTarget>
	where TTarget : class
	where TValue : struct, IComparable<TValue>
{
	private readonly Expression<Func<TTarget, TProperty>> _property;

	private readonly PredicateExpressionBuilder<TTarget> _builder;

	private readonly RangeBoundary _boundary;

	/// <summary>
	/// 初始化继承自 <see cref="SegmentSpecification{TTarget, TProperty, TValue}"/> 的新实例。
	/// </summary>
	/// <param name="property">属性表达式。</param>
	/// <param name="min">最小边界值。</param>
	/// <param name="max">最大边界值。</param>
	/// <param name="boundary">指示边界值是否包含在内。</param>
	/// <exception cref="ArgumentNullException"><paramref name="property"/> 为 <see langword="null"/>，或 <paramref name="min"/> 与 <paramref name="max"/> 均为 <see langword="null"/>。</exception>
	/// <exception cref="ArgumentException">最小值大于最大值时抛出。</exception>
	protected SegmentSpecification(Expression<Func<TTarget, TProperty>> property, TValue? min, TValue? max, RangeBoundary boundary)
	{
		_builder = new PredicateExpressionBuilder<TTarget>();
		_property = property ?? throw new ArgumentNullException(nameof(property), Resources.IDS_PROPERTY_EXPRESSION_CAN_NOT_NULL);
		if (min == null && max == null)
		{
			// ReSharper disable once NotResolvedInText
			throw new ArgumentNullException("min/max", string.Format(Resources.IDS_AT_LEAST_ONE_PARAMETER_IS_REQUIRED, $"{nameof(min)}/{nameof(max)}"));
		}

		if (IsMinGreaterThanMax(min, max))
		{
			throw new ArgumentException(string.Format(Resources.IDS_VALUE_OF_MIN_CAN_NOT_GREATER_THAN_MAX, min, max));
		}

		MinimumValue = GetValue(min);
		MaximumValue = GetValue(max);
		_boundary = boundary;
	}

	/// <summary>
	/// 获取最大值。
	/// </summary>
	protected TValue? MaximumValue { get; }

	/// <summary>
	/// 获取最小值。
	/// </summary>
	protected TValue? MinimumValue { get; }

	/// <summary>
	/// 检查最小值是否大于最大值。
	/// </summary>
	/// <param name="min">最小值。</param>
	/// <param name="max">最大值。</param>
	/// <returns>最小值大于最大值时为 <see langword="true"/>；任一值为 <see langword="null"/> 时为 <see langword="false"/>。</returns>
	protected virtual bool IsMinGreaterThanMax(TValue? min, TValue? max)
	{
		if (min == null || max == null)
		{
			return false;
		}

		return min.Value.CompareTo(max.Value) > 0;
	}

	/// <summary>
	/// 获取值。
	/// </summary>
	/// <param name="value">值。</param>
	/// <returns>返回传入的值。</returns>
	protected virtual TValue? GetValue(TValue? value)
	{
		return value;
	}

	/// <summary>
	/// 获取用于最小值的查询运算符。
	/// </summary>
	/// <param name="boundary">边界指示。</param>
	/// <returns>边界包含左侧时返回 <see cref="QueryOperator.GreaterThanOrEqual"/>，否则返回 <see cref="QueryOperator.GreaterThan"/>。</returns>
	protected virtual QueryOperator GetMinValueOperator(RangeBoundary boundary)
	{
#pragma warning disable IDE0066 // 将 switch 语句转换为表达式
		switch (boundary)
		{
			case RangeBoundary.Left:
			case RangeBoundary.Both:
				return QueryOperator.GreaterThanOrEqual;
			default:
				return QueryOperator.GreaterThan;
		}
#pragma warning restore IDE0066 // 将 switch 语句转换为表达式
	}

	/// <summary>
	/// 获取用于最大值的查询运算符。
	/// </summary>
	/// <param name="boundary">边界指示。</param>
	/// <returns>边界包含右侧时返回 <see cref="QueryOperator.LessThanOrEqual"/>，否则返回 <see cref="QueryOperator.LessThan"/>。</returns>
	protected virtual QueryOperator GetMaxValueOperator(RangeBoundary boundary)
	{
#pragma warning disable IDE0066
		switch (boundary)
		{
			case RangeBoundary.Right:
			case RangeBoundary.Both:
				return QueryOperator.LessThanOrEqual;
			default:
				return QueryOperator.LessThan;
		}
#pragma warning restore IDE0066
	}

	/// <inheritdoc />
	public virtual Expression<Func<TTarget, bool>> Satisfy()
	{
		if (MinimumValue != null)
		{
			_builder.Append(_property, GetMinValueOperator(_boundary), MinimumValue);
		}

		if (MaximumValue != null)
		{
			_builder.Append(_property, GetMaxValueOperator(_boundary), MaximumValue);
		}

		return _builder.ToLambda();
	}
}