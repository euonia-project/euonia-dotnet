using Nerosoft.Euonia.Concurrency.Redis.Internal;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// 使用 Redis 的 <see cref="ISynchronizationHandle"/> 实现。
/// </summary>
public class RedisSynchronizationHandle : ISynchronizationHandle
{
    private RedisLockHandle? _innerHandle;

    /// <summary>
    /// 使用内部锁句柄初始化 <see cref="RedisSynchronizationHandle"/>。
    /// </summary>
    /// <param name="innerHandle">负责实际续期与释放的 Redis 锁句柄。</param>
    internal RedisSynchronizationHandle(RedisLockHandle innerHandle)
    {
        _innerHandle = innerHandle;
    }

    /// <inheritdoc />
    public CancellationToken HandleCancellationToken => Volatile.Read(ref _innerHandle)?.HandleCancellationToken ?? throw this.ObjectDisposed();

    /// <summary>
    /// 释放底层同步句柄并结束锁的持有。
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _innerHandle, null)?.Dispose();

    /// <summary>
    /// 异步释放锁
    /// </summary>
    /// <returns>表示异步释放操作的值任务。</returns>
    public ValueTask DisposeAsync() => Interlocked.Exchange(ref _innerHandle, null)?.DisposeAsync() ?? default;
}