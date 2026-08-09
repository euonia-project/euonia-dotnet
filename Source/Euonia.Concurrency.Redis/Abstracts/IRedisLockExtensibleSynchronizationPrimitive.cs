using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Abstracts;

/// <summary>
/// 表示可在 Redis 数据库上续期（延长）的锁同步原语。
/// </summary>
internal interface IRedisLockExtensibleSynchronizationPrimitive : IRedisLockReleasableSynchronizationPrimitive
{
    /// <summary>
    /// 获取尝试获取锁的超时时间。
    /// </summary>
    TimeoutValue AcquireTimeout { get; }

    /// <summary>
    /// 在指定的数据库上异步尝试续期锁。
    /// </summary>
    /// <param name="database">用于续期锁的 Redis 数据库。</param>
    /// <returns>表示异步操作的任务；续期成功时为 <c>true</c>，否则为 <c>false</c>。</returns>
    Task<bool> TryExtendAsync(IDatabaseAsync database);
}