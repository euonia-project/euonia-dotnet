using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示一个可用于过滤对象集合的规约，要求 <see cref="DateTime"/> 日期值位于指定的区间内。
/// </summary>
/// <remarks>
/// 该规约仅按日期进行过滤：构造时将最大值调整为次日 00:00，从而使上界包含当天的全部时间。
/// </remarks>
/// <typeparam name="TTarget">要过滤的目标类型。</typeparam>
/// <typeparam name="TProperty">属性类型。</typeparam>
public sealed class DateSegmentSpecification<TTarget, TProperty> : SegmentSpecification<TTarget, TProperty, DateTime>
    where TTarget : class
{
    /// <summary>
    /// 初始化 <see cref="DateSegmentSpecification{TTarget, TProperty}"/> 类的新实例。
    /// </summary>
    /// <param name="property">属性表达式。</param>
    /// <param name="min">最小边界日期。</param>
    /// <param name="max">最大边界日期。</param>
    /// <param name="boundary">指示边界值是否包含在内。</param>
    public DateSegmentSpecification(Expression<Func<TTarget, TProperty>> property, DateTime? min, DateTime? max, RangeBoundary boundary)
        : base(property, min, max?.AddDays(1).Date, boundary)
    {
    }
}