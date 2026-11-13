using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;
using org.apache.zookeeper;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// <see cref="ISynchronizationHandle"/> 的实现，其中持有原语
/// 基于 ZooKeeper 临时节点的存在。
/// </summary>
internal sealed class ZooKeeperNodeHandle : ISynchronizationHandle
{
    private readonly ZooKeeperConnection _connection;
    private readonly ZooKeeperPath _nodePath;
    private readonly bool _shouldDeleteParent;
    private readonly Lazy<HandleLostState> _handleLostState;

    private volatile bool _disposed;

    /// <summary>
    /// 使用已获取的临时节点初始化句柄，并延迟启动节点丢失监视。
    /// </summary>
    /// <param name="connection">持有临时节点的 ZooKeeper 连接。</param>
    /// <param name="nodePath">临时节点的完整路径。</param>
    /// <param name="shouldDeleteParent">释放句柄时是否尝试删除父目录。</param>
    public ZooKeeperNodeHandle(ZooKeeperConnection connection, string nodePath, bool shouldDeleteParent)
    {
        _connection = connection;
        _nodePath = new ZooKeeperPath(nodePath);
        _shouldDeleteParent = shouldDeleteParent;

        _handleLostState = new Lazy<HandleLostState>(() =>
        {
            var handleLostSource = CancellationTokenSource.CreateLinkedTokenSource(_connection.ConnectionLostToken);
            var handleLostToken = handleLostSource.Token; // 在源被释放之前先取出该令牌
            var disposalSource = new CancellationTokenSource();
            var monitoringTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var result = await WaitForNotExistsOrChangedAsync(
                            _connection,
                            _nodePath.ToString(), 
                            timeoutToken: disposalSource.Token
                        ).ConfigureAwait(false);
                        switch (result)
                        {
                            case false: // disposalSource 已触发
                                return;
                            case true: // 节点已不存在
                                handleLostSource.Cancel();
                                return;
                        }
                    }
                }
                finally
                {
                    handleLostSource.Dispose();
                }
            });
            return new HandleLostState(handleLostToken, disposalSource, monitoringTask);
        });
    }

    /// <inheritdoc />
    public CancellationToken HandleCancellationToken => _disposed ? throw this.ObjectDisposed() : _handleLostState.Value.Token;

    /// <inheritdoc />
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <inheritdoc />
    public ValueTask DisposeAsync() =>
        // 此处被迫使用同步方式等待异步调用，因为 ZooKeeperNetEx 没有同步 API
        InternalDisposeAsync().AwaitSyncOverAsync();

    private async Task InternalDisposeAsync()
    {
        if (_disposed) { return; }
        _disposed = true;

        try
        {
            // 清理监视
            if (_handleLostState.IsValueCreated)
            {
                _handleLostState.Value.DisposalSource.Cancel();
                _handleLostState.Value.DisposalSource.Dispose();
                await _handleLostState.Value.MonitoringTask.ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                // 清理节点
                await _connection.ZooKeeper.deleteAsync(_nodePath.ToString()).ConfigureAwait(false);

                if (_shouldDeleteParent)
                {
                    try { await _connection.ZooKeeper.deleteAsync(_nodePath.GetDirectory()!.Value.ToString()).ConfigureAwait(false); }
                    catch (KeeperException.NotEmptyException) { } // 无法删除仍有其他子节点的节点
                    catch (KeeperException.NoNodeException) { } // 无法删除不存在的节点（竞态条件）
                }
            }
            finally
            {
                _connection.Dispose();
            }
        }
    }

    /// <summary>
    /// 当 <paramref name="path"/> 不存在时返回 true。
    /// 收到指示 <paramref name="path"/> 已更改的监视事件时返回 null。
    /// 如果 <paramref name="timeoutToken"/> 触发，则返回 false。
    /// </summary>
    public static async Task<bool?> WaitForNotExistsOrChangedAsync(
        ZooKeeperConnection connection,
        string path,
        CancellationToken timeoutToken)
    {
        using var watcher = new TaskWatcher<bool?>((_, s) => s.TrySetResult(null));
        using var timeoutRegistration = timeoutToken.Register(
            () => watcher.TaskCompletionSource.TrySetResult(false)
        );
        // 这是必要的，因为如果连接断开且始终未恢复，我们将永远收不到会话过期通知
        using var connectionLostRegistration = connection.ConnectionLostToken.Register(
            () => watcher.TaskCompletionSource.TrySetException(new InvalidOperationException("Lost connection to ZooKeeper"))
        );

        var exists = await connection.ZooKeeper.existsAsync(path, watcher).ConfigureAwait(false);
        return exists == null ? true : await watcher.TaskCompletionSource.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// 在收到已连接状态事件时调用回调完成任务的通用监视器，释放后不再触发回调。
    /// </summary>
    /// <typeparam name="TResult">等待任务完成后返回的结果类型。</typeparam>
    private sealed class TaskWatcher<TResult> : Watcher, IDisposable
    {
        private volatile Action<WatchedEvent, TaskCompletionSource<TResult>>? _watchedEventHandler;

        public TaskWatcher(Action<WatchedEvent, TaskCompletionSource<TResult>> watchedEventHandler)
        {
            _watchedEventHandler = watchedEventHandler;
        }

        public TaskCompletionSource<TResult> TaskCompletionSource { get; } = new();

        public void Dispose() => _watchedEventHandler = null;

        public override Task process(WatchedEvent @event)
        {
            // 只关心已连接状态的事件；其他状态由 ConnectionLostToken 负责处理
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                _watchedEventHandler?.Invoke(@event, TaskCompletionSource);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 记录句柄丢失监视所需的取消令牌、释放信号源与后台监视任务。
    /// </summary>
    /// <param name="Token">节点丢失或连接断开时触发的令牌。</param>
    /// <param name="DisposalSource">用于通知监视任务停止的信号源。</param>
    /// <param name="MonitoringTask">后台监视节点是否消失的任务。</param>
    private record HandleLostState(CancellationToken Token, CancellationTokenSource DisposalSource, Task MonitoringTask);
}