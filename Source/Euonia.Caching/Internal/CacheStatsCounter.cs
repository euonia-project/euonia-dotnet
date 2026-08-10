namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 基于 <see cref="Interlocked"/> 操作的线程安全统计计数器。
/// </summary>
internal sealed class CacheStatsCounter
{
    /// <summary>
    /// 存储各统计类型的计数，索引为 <see cref="CacheStatsCounterType"/> 的数值。
    /// </summary>
    private volatile long[] _counters = new long[9];

    /// <summary>
    /// 以原子方式将指定值添加到指定类型的计数器。
    /// </summary>
    /// <param name="type">统计类型。</param>
    /// <param name="value">要添加的值。</param>
    public void Add(CacheStatsCounterType type, long value)
    {
        Interlocked.Add(ref _counters[(int)type], value);
    }

    /// <summary>
    /// 以原子方式将指定类型的计数器减一。
    /// </summary>
    /// <param name="type">统计类型。</param>
    public void Decrement(CacheStatsCounterType type)
    {
        Interlocked.Decrement(ref _counters[(int)type]);
    }

    /// <summary>
    /// 获取指定类型的计数器的当前值。
    /// </summary>
    /// <param name="type">统计类型。</param>
    /// <returns>计数器的当前值。</returns>
    public long Get(CacheStatsCounterType type) => _counters[(int)type];

    /// <summary>
    /// 以原子方式将指定类型的计数器加一。
    /// </summary>
    /// <param name="type">统计类型。</param>
    public void Increment(CacheStatsCounterType type)
    {
        Interlocked.Increment(ref _counters[(int)type]);
    }

    /// <summary>
    /// 以原子方式将指定类型的计数器设置为指定值。
    /// </summary>
    /// <param name="type">统计类型。</param>
    /// <param name="value">要设置的值。</param>
    public void Set(CacheStatsCounterType type, long value)
    {
        Interlocked.Exchange(ref _counters[(int)type], value);
    }
}