using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// Implements <see cref="ISynchronizationHandle"/>
/// </summary>
public sealed class ZooKeeperSynchronizationHandle : ISynchronizationHandle
{
    private ZooKeeperNodeHandle _innerHandle;
    private IDisposable _finalizerRegistration;

    internal ZooKeeperSynchronizationHandle(ZooKeeperNodeHandle innerHandle)
    {
        _innerHandle = innerHandle;
        // If the process exits, the fact that we use ephemeral nodes gives us guaranteed
        // abandonment protection. Until that point, though, we're vulnerable to abandonment because
        // we pool zookeeper sessions. While those sessions have a max age, they won't be able to exit
        // so long as a handle to them remains open
        _finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, innerHandle);
    }

    /// <summary>
    /// Implements <see cref="ISynchronizationHandle.HandleCancellationToken"/>
    /// </summary>
    public CancellationToken HandleCancellationToken => (Volatile.Read(ref _innerHandle) ?? throw this.ObjectDisposed()).HandleCancellationToken;

    // explicit because this is sync-over-async
    void IDisposable.Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// Releases the semaphore
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _finalizerRegistration, null)?.Dispose();
        return Interlocked.Exchange(ref _innerHandle, null)?.DisposeAsync() ?? default;
    }
}