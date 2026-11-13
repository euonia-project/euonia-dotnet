using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 持有 Redis 锁的内部句柄：由后台监视器按节奏续期，并在释放时向所有已获取的数据库发送释放命令。
/// </summary>
internal sealed class RedisLockHandle : ISynchronizationHandle, LeaseMonitor.ILeaseHandle
{
    private readonly IRedisLockExtensibleSynchronizationPrimitive _primitive;
    private Dictionary<IDatabase, Task<bool>>? _tryAcquireTasks;
    private readonly TimeoutValue _extensionCadence, _expiry;
    private readonly LeaseMonitor _monitor;

    /// <summary>
    /// 使用获取结果与续期参数初始化 <see cref="RedisLockHandle"/>，并立即启动租约监视器。
    /// </summary>
    /// <param name="primitive">用于续期与释放锁的同步原语。</param>
    /// <param name="tryAcquireTasks">获取锁时各数据库对应的成功任务。</param>
    /// <param name="extensionCadence">后台续期的间隔。</param>
    /// <param name="expiry">锁的过期时间。</param>
    public RedisLockHandle(
        IRedisLockExtensibleSynchronizationPrimitive primitive,
        Dictionary<IDatabase, Task<bool>> tryAcquireTasks,
        TimeoutValue extensionCadence,
        TimeoutValue expiry)
    {
        _primitive = primitive;
        _tryAcquireTasks = tryAcquireTasks;
        _extensionCadence = extensionCadence;
        _expiry = expiry;
        // 必须最后设置，因为监视器构造函数会读取此对象的其他字段
        _monitor = new LeaseMonitor(this);
    }

    /// <inheritdoc />
    public CancellationToken HandleCancellationToken => _monitor.HandleLostToken;

    /// <summary>
    /// 释放锁并释放底层资源。
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// 异步释放锁
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _monitor.DisposeAsync().ConfigureAwait(false);
        var tryAcquireTasks = Interlocked.Exchange(ref _tryAcquireTasks, null);
        if (tryAcquireTasks != null)
        {
            await new RedisLockRelease(_primitive, tryAcquireTasks).ReleaseAsync().ConfigureAwait(false);
        }
    }

    TimeoutValue LeaseMonitor.ILeaseHandle.LeaseDuration => _expiry;

    TimeoutValue LeaseMonitor.ILeaseHandle.MonitoringCadence => _extensionCadence;

    async Task<LeaseMonitor.LeaseState> LeaseMonitor.ILeaseHandle.RenewOrValidateLeaseAsync(CancellationToken cancellationToken)
    {
        var extendResult = await new RedisLockExtend(_primitive, _tryAcquireTasks!, cancellationToken).TryExtendAsync().ConfigureAwait(false);
        return extendResult switch
        {
            null => LeaseMonitor.LeaseState.Unknown,
            false => LeaseMonitor.LeaseState.Lost,
            true => LeaseMonitor.LeaseState.Renewed,
        };
    }
}