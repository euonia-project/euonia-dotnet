using Nerosoft.Euonia.Concurrency.Redis.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// 保存 Redis 同步算法所需的超时、续期节奏与忙等休眠范围。
/// </summary>
internal readonly struct RedisSynchronizationOptions
{
    /// <summary>
    /// 使用锁超时、续期节奏与忙等休眠范围初始化 <see cref="RedisSynchronizationOptions"/>。
    /// </summary>
    /// <param name="redisLockTimeouts">锁的过期时间与获取超时设置。</param>
    /// <param name="extensionCadence">锁被持有时后台续期的间隔。</param>
    /// <param name="minBusyWaitSleepTime">两次获取尝试之间的最短休眠时间。</param>
    /// <param name="maxBusyWaitSleepTime">两次获取尝试之间的最长休眠时间。</param>
    public RedisSynchronizationOptions(
        RedisLockTimeouts redisLockTimeouts,
        TimeoutValue extensionCadence,
        TimeoutValue minBusyWaitSleepTime,
        TimeoutValue maxBusyWaitSleepTime)
    {
        RedisLockTimeouts = redisLockTimeouts;
        ExtensionCadence = extensionCadence;
        MinBusyWaitSleepTime = minBusyWaitSleepTime;
        MaxBusyWaitSleepTime = maxBusyWaitSleepTime;
    }

    /// <summary>
    /// 获取锁的过期时间与获取超时设置。
    /// </summary>
    public RedisLockTimeouts RedisLockTimeouts { get; }

    /// <summary>
    /// 获取锁被持有时后台续期的间隔。
    /// </summary>
    public TimeoutValue ExtensionCadence { get; }

    /// <summary>
    /// 获取两次获取尝试之间的最短休眠时间。
    /// </summary>
    public TimeoutValue MinBusyWaitSleepTime { get; }

    /// <summary>
    /// 获取两次获取尝试之间的最长休眠时间。
    /// </summary>
    public TimeoutValue MaxBusyWaitSleepTime { get; }
}