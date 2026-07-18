/// <summary>
/// 表示条件检查的结果，包含值和验证状态。
/// </summary>
/// <typeparam name="TValue">值的类型。</typeparam>
public sealed class CheckResult<TValue>
{
    /// <summary>
    /// 使用指定值初始化 <see cref="CheckResult{TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="value">要包装的值。</param>
    internal CheckResult(TValue value)
    {
        Value = value;
    }

    /// <summary>
    /// 使用指定值和验证状态初始化 <see cref="CheckResult{TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="value">要包装的值。</param>
    /// <param name="isValid">验证是否通过。</param>
    internal CheckResult(TValue value, bool isValid)
        : this(value)
    {
        IsValid = isValid;
    }

    /// <summary>
    /// 获取包装的值。
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// 获取一个值，指示验证是否通过。
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// 判断两个 <see cref="CheckResult{TValue}"/> 实例的值是否相等。
    /// </summary>
    public static bool operator ==(CheckResult<TValue> left, CheckResult<TValue> right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return EqualityComparer<TValue>.Default.Equals(left.Value, right.Value);
    }

    /// <summary>
    /// 判断两个 <see cref="CheckResult{TValue}"/> 实例的值是否不相等。
    /// </summary>
    public static bool operator !=(CheckResult<TValue> left, CheckResult<TValue> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 将值隐式转换为 <see cref="CheckResult{TValue}"/>。
    /// </summary>
    public static implicit operator CheckResult<TValue>(TValue value)
    {
        return new CheckResult<TValue>(value);
    }

    /// <summary>
    /// 将 <see cref="CheckResult{TValue}"/> 隐式转换为值。
    /// </summary>
    public static implicit operator TValue(CheckResult<TValue> result)
    {
        return result.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj switch
        {
            null => false,
            CheckResult<TValue> other => EqualityComparer<TValue>.Default.Equals(Value, other.Value),
            TValue other => EqualityComparer<TValue>.Default.Equals(Value, other),
            _ => false
        };
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    /// <summary>
    /// 验证成功时执行指定操作。
    /// </summary>
    /// <param name="action">验证成功时要执行的操作。</param>
    /// <returns>当前实例以支持链式调用。</returns>
    public CheckResult<TValue> Success(Action<TValue> action)
    {
        if (IsValid)
        {
            action?.Invoke(Value);
        }

        return this;
    }

    /// <summary>
    /// 验证失败时执行指定操作。
    /// </summary>
    /// <param name="action">验证失败时要执行的操作。</param>
    /// <returns>当前实例以支持链式调用。</returns>
    public CheckResult<TValue> Failure(Action<TValue> action)
    {
        if (!IsValid)
        {
            action?.Invoke(Value);
        }

        return this;
    }

    /// <summary>
    /// 无论验证结果如何都执行指定操作。
    /// </summary>
    /// <param name="action">要执行的操作，接收值和验证状态。</param>
    public void Then(Action<TValue, bool> action)
    {
        action?.Invoke(Value, IsValid);
    }
}