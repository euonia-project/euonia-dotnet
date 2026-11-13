using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 基于 ZooKeeper 临时节点的同步句柄；释放时删除该节点。
/// </summary>
public sealed class ZooKeeperSynchronizationHandle : ISynchronizationHandle
{
    private ZooKeeperNodeHandle? _innerHandle;
    private IDisposable? _finalizerRegistration;

    /// <summary>
    /// 使用内部节点句柄初始化 <see cref="ZooKeeperSynchronizationHandle"/>，并注册到托管终结器队列。
    /// </summary>
    /// <param name="innerHandle">持有 ZooKeeper 临时节点的内部句柄。</param>
    internal ZooKeeperSynchronizationHandle(ZooKeeperNodeHandle innerHandle)
    {
        _innerHandle = innerHandle;
        // 如果进程退出，使用临时节点可为我们提供可靠的
        // 放弃保护。但在那之前，我们仍可能被放弃，因为
        // 我们池化了 ZooKeeper 会话。虽然这些会话有最大存活时间，但只要仍持有
        // 它们的句柄，它们就无法退出
        _finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, innerHandle);
    }

    /// <inheritdoc />
    public CancellationToken HandleCancellationToken => (Volatile.Read(ref _innerHandle) ?? throw this.ObjectDisposed()).HandleCancellationToken;

    // 显式实现，因为这里是同步等待异步调用
    void IDisposable.Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// 释放信号量
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _finalizerRegistration, null)?.Dispose();
        return Interlocked.Exchange(ref _innerHandle, null)?.DisposeAsync() ?? default;
    }
}