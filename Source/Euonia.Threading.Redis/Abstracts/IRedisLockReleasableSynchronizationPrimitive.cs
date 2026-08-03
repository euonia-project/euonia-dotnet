using StackExchange.Redis;

namespace Nerosoft.Euonia.Threading.Redis;

/// <summary>
/// 表示可在 Redis 数据库上释放的锁同步原语。
/// </summary>
internal interface IRedisLockReleasableSynchronizationPrimitive
{
    /// <summary>
    /// 在指定的数据库上异步释放锁。
    /// </summary>
    /// <param name="database">用于释放锁的 Redis 数据库。</param>
    /// <param name="fireAndForget">指示是否以即发即弃（fire-and-forget）方式执行，不等待结果。</param>
    /// <returns>表示异步释放操作的任务。</returns>
    Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget);

    /// <summary>
    /// 在指定的数据库上同步释放锁。
    /// </summary>
    /// <param name="database">用于释放锁的 Redis 数据库。</param>
    /// <param name="fireAndForget">指示是否以即发即弃（fire-and-forget）方式执行，不等待结果。</param>
    void Release(IDatabase database, bool fireAndForget);
}