using Nerosoft.Euonia.Concurrency.Redis.Abstracts;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

internal sealed class RedisLockHandle : ISynchronizationHandle, LeaseMonitor.ILeaseHandle
{
    private readonly IRedisLockExtensibleSynchronizationPrimitive _primitive;
    private Dictionary<IDatabase, Task<bool>> _tryAcquireTasks;
    private readonly TimeoutValue _extensionCadence, _expiry;
    private readonly LeaseMonitor _monitor;

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
        // important to set this last, since the monitor constructor will read other fields of this
        _monitor = new LeaseMonitor(this);
    }

    /// <summary>
    /// Implements <see cref="ISynchronizationHandle.HandleCancellationToken"/>
    /// </summary>
    public CancellationToken HandleCancellationToken => _monitor.HandleLostToken;

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// Releases the lock asynchronously
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