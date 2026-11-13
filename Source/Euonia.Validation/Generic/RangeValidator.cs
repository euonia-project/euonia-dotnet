namespace Nerosoft.Euonia.Validation;

/// <summary>
/// RangeValidator 类。
/// 实现 <see cref="AbstractValidator{TValue}" />
/// </summary>
/// <typeparam name="TValue">值类型。</typeparam>
/// <seealso cref="AbstractValidator{TValue}" />
public class RangeValidator<TValue> : AbstractValidator<TValue>
    where TValue : IComparable, IComparable<TValue>
{
    /// <summary>
    /// 获取最小值。
    /// </summary>
    /// <value>最小值。</value>
    public TValue Minimum { get; }

    /// <summary>
    /// 获取最大值。
    /// </summary>
    /// <value>最大值。</value>
    public TValue Maximum { get; }

    /// <summary>
    /// 初始化 <see cref="RangeValidator{TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="minimum">最小值。</param>
    /// <param name="maximum">最大值。</param>
    /// <param name="message">错误消息。</param>
    /// <exception cref="ArgumentException">最小值不能大于或等于最大值。</exception>
    public RangeValidator(TValue minimum, TValue maximum, string message)
        : base(message)
    {
        if (minimum.CompareTo(maximum) >= 0)
        {
            throw new ArgumentException(Resources.IDS_MIN_CANNOT_GREATER_THAN_MAX);
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// 验证指定的值。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <returns>如果值有效则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public override bool Validate(TValue value)
    {
        return value.CompareTo(Minimum) >= 0 && value.CompareTo(Maximum) <= 0;
    }
}
