namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 表示任务的超时值。
/// </summary>
public readonly struct TimeoutValue : IEquatable<TimeoutValue>, IComparable<TimeoutValue>
{
    /// <summary>
    /// 使用指定的超时值初始化 <see cref="TimeoutValue"/> 的新实例。
    /// </summary>
    /// <param name="timeout">超时时间。</param>
    /// <exception cref="ArgumentOutOfRangeException">当超时值超出有效范围时抛出。</exception>
    public TimeoutValue(TimeSpan? timeout)
    {
        if (timeout is { } timeoutValue)
        {
            // 基于 Task.Wait(TimeSpan) 的实现
            // https://referencesource.microsoft.com/#mscorlib/system/threading/Tasks/Task.cs,855657030ba22f78

            var totalMilliseconds = (long)timeoutValue.TotalMilliseconds;
            if (totalMilliseconds is < -1 or > int.MaxValue)
            {
                var message = $"Must be {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)} ({Timeout.InfiniteTimeSpan}) or a non-negative value <= {TimeSpan.FromMilliseconds(int.MaxValue)})";
                throw new ArgumentOutOfRangeException(nameof(timeout), timeoutValue, message);
            }

            InMilliseconds = (int)totalMilliseconds;
        }
        else
        {
            InMilliseconds = Timeout.Infinite;
        }
    }

    /// <summary>
    /// 获取以毫秒为单位的超时值。
    /// </summary>
    public int InMilliseconds { get; }

    /// <summary>
    /// 获取以秒为单位的超时值。
    /// </summary>
    public int InSeconds => IsInfinite ? throw new InvalidOperationException("infinite timeout cannot be converted to seconds") : InMilliseconds / 1000;

    /// <summary>
    /// 获取一个值，指示超时是否为无限。
    /// </summary>
    public bool IsInfinite => InMilliseconds == Timeout.Infinite;

    /// <summary>
    /// 获取一个值，指示超时是否为零。
    /// </summary>
    public bool IsZero => InMilliseconds == 0;

    /// <summary>
    /// 获取以 <see cref="TimeSpan"/> 表示的超时值。
    /// </summary>
    public TimeSpan TimeSpan => TimeSpan.FromMilliseconds(InMilliseconds);

    /// <summary>
    /// 确定指定的 <see cref="TimeoutValue"/> 是否等于当前的 <see cref="TimeoutValue"/>。
    /// </summary>
    /// <param name="that">要比较的 <see cref="TimeoutValue"/>。</param>
    /// <returns>如果相等则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool Equals(TimeoutValue that) => InMilliseconds == that.InMilliseconds;

    /// <summary>
    /// 确定指定的 <see cref="object"/> 是否等于当前的 <see cref="TimeoutValue"/>。
    /// </summary>
    /// <param name="obj">要比较的对象。</param>
    /// <returns>如果相等则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public override bool Equals(object obj) => obj is TimeoutValue that && Equals(that);

    /// <inheritdoc />
    public override int GetHashCode() => InMilliseconds;

    /// <summary>
    /// 将当前的 <see cref="TimeoutValue"/> 与另一个 <see cref="TimeoutValue"/> 进行比较，
    /// 并返回一个毫秒值，指示当前实例在排序顺序中是位于另一个 <see cref="TimeoutValue"/> 之前、之后还是相同位置。
    /// </summary>
    /// <param name="that">要比较的 <see cref="TimeoutValue"/>。</param>
    /// <returns>一个值，指示两个对象的相对顺序。</returns>
    public int CompareTo(TimeoutValue that) => IsInfinite ? (that.IsInfinite ? 0 : 1) : that.IsInfinite ? -1 : InMilliseconds.CompareTo(that.InMilliseconds);

    /// <summary>
    /// 比较两个 <see cref="TimeoutValue"/> 是否相等。
    /// </summary>
    /// <param name="a">第一个 <see cref="TimeoutValue"/>。</param>
    /// <param name="b">第二个 <see cref="TimeoutValue"/>。</param>
    /// <returns>如果相等则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator ==(TimeoutValue a, TimeoutValue b) => a.Equals(b);

    /// <summary>
    /// 比较两个 <see cref="TimeoutValue"/> 是否不相等。
    /// </summary>
    /// <param name="a">第一个 <see cref="TimeoutValue"/>。</param>
    /// <param name="b">第二个 <see cref="TimeoutValue"/>。</param>
    /// <returns>如果不相等则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public static bool operator !=(TimeoutValue a, TimeoutValue b) => !(a == b);

    /// <summary>
    /// 将 <see cref="TimeSpan"/> 可空值隐式转换为 <see cref="TimeoutValue"/>。
    /// </summary>
    /// <param name="timeout">要转换的超时值。</param>
    /// <returns>转换后的 <see cref="TimeoutValue"/>。</returns>
    public static implicit operator TimeoutValue(TimeSpan? timeout) => new(timeout);

    /// <inheritdoc />
    public override string ToString() => IsInfinite ? "∞" : IsZero ? "0" : TimeSpan.ToString();
}