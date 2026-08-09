using Nerosoft.Euonia.Concurrency.Redis.Internal;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// <see cref="ISynchronizationHandle"/> implementation with Redis.
/// </summary>
public class RedisSynchronizationHandle : ISynchronizationHandle
{
    private RedisLockHandle _innerHandle;

    internal RedisSynchronizationHandle(RedisLockHandle innerHandle)
    {
        _innerHandle = innerHandle;
    }

    /// <summary>
    /// Implements <see cref="ISynchronizationHandle.HandleCancellationToken"/>
    /// </summary>
    public CancellationToken HandleCancellationToken => Volatile.Read(ref _innerHandle)?.HandleCancellationToken ?? throw this.ObjectDisposed();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _innerHandle, null)?.Dispose();

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    /// <returns></returns>
    public ValueTask DisposeAsync() => Interlocked.Exchange(ref _innerHandle, null)?.DisposeAsync() ?? default;
}