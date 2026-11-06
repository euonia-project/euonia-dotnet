using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 描述 RedLock 各阶段使用的超时时间，并根据过期时间与最小有效期推导出获取超时。
/// </summary>
internal readonly struct RedisLockTimeouts
{
    /// <summary>
    /// 使用过期时间与最小有效期初始化 <see cref="RedisLockTimeouts"/>。
    /// </summary>
    /// <param name="expiry">锁在 Redis 中的过期时间。</param>
    /// <param name="minValidityTime">获取完成后保证剩余的最小有效期。</param>
    public RedisLockTimeouts(TimeoutValue expiry, TimeoutValue minValidityTime)
    {
        Expiry = expiry;
        MinValidityTime = minValidityTime;
    }

    /// <summary>
    /// 获取锁在 Redis 中的过期时间。
    /// </summary>
    public TimeoutValue Expiry { get; }

    /// <summary>
    /// 获取完成后保证剩余的最小有效期。
    /// </summary>
    public TimeoutValue MinValidityTime { get; }

    /// <summary>
    /// 获取锁允许消耗的最长时间，由过期时间减去最小有效期得到。
    /// </summary>
    public TimeoutValue AcquireTimeout => Expiry.TimeSpan - MinValidityTime.TimeSpan;
}