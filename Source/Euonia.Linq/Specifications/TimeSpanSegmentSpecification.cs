using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个可用于过滤对象集合的规约，要求 <see cref="TimeSpan"/> 属性值位于指定的区间内。
/// </summary>
/// <typeparam name="TTarget">要过滤的目标类型。</typeparam>
/// <typeparam name="TProperty">属性类型。</typeparam>
public sealed class TimeSpanSegmentSpecification<TTarget, TProperty> : SegmentSpecification<TTarget, TProperty, TimeSpan>
    where TTarget : class
{
    /// <summary>
    /// 初始化 <see cref="TimeSpanSegmentSpecification{TTarget, TProperty}"/> 类的新实例。
    /// </summary>
    /// <param name="property">属性表达式。</param>
    /// <param name="min">最小边界值。</param>
    /// <param name="max">最大边界值。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    public TimeSpanSegmentSpecification(Expression<Func<TTarget, TProperty>> property, TimeSpan? min, TimeSpan? max, RangeBoundary boundary)
        : base(property, min, max, boundary)
    {
    }
}
