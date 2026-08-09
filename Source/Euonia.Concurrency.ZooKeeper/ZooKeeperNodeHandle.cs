using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;
using org.apache.zookeeper;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// <see cref="ISynchronizationHandle"/> implementation where holding the primitive
/// is based on the existence of an ephemeral zookeeper node
/// </summary>
internal sealed class ZooKeeperNodeHandle : ISynchronizationHandle
{
    private readonly ZooKeeperConnection _connection;
    private readonly ZooKeeperPath _nodePath;
    private readonly bool _shouldDeleteParent;
    private readonly Lazy<HandleLostState> _handleLostState;

    private volatile bool _disposed;

    public ZooKeeperNodeHandle(ZooKeeperConnection connection, string nodePath, bool shouldDeleteParent)
    {
        _connection = connection;
        _nodePath = new ZooKeeperPath(nodePath);
        _shouldDeleteParent = shouldDeleteParent;

        _handleLostState = new Lazy<HandleLostState>(() =>
        {
            var handleLostSource = CancellationTokenSource.CreateLinkedTokenSource(_connection.ConnectionLostToken);
            var handleLostToken = handleLostSource.Token; // grab this now before the source is disposed
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
                            case false: // disposalSource triggered
                                return;
                            case true: // node no longer exists
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

    public CancellationToken HandleCancellationToken => _disposed ? throw this.ObjectDisposed() : _handleLostState.Value.Token;

    public void Dispose() => this.DisposeSyncViaAsync();

    public ValueTask DisposeAsync() =>
        // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
        InternalDisposeAsync().AwaitSyncOverAsync();

    private async Task InternalDisposeAsync()
    {
        if (_disposed) { return; }
        _disposed = true;

        try
        {
            // clean up monitoring
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
                // clean up the node
                await _connection.ZooKeeper.deleteAsync(_nodePath.ToString()).ConfigureAwait(false);

                if (_shouldDeleteParent)
                {
                    try { await _connection.ZooKeeper.deleteAsync(_nodePath.GetDirectory()!.Value.ToString()).ConfigureAwait(false); }
                    catch (KeeperException.NotEmptyException) { } // can't delete nodes which have other children
                    catch (KeeperException.NoNodeException) { } // can't delete nodes which don't exist (race condition)
                }
            }
            finally
            {
                _connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns true when <paramref name="path"/> does not exist. 
    /// Returns null when we receive a watch event indicating that <paramref name="path"/> has changed. 
    /// Returns false if the <paramref name="timeoutToken"/> fires.
    /// </summary>
    public static async Task<bool?> WaitForNotExistsOrChangedAsync(
        ZooKeeperConnection connection,
        string path,
        CancellationToken timeoutToken)
    {
        using var watcher = new TaskWatcher<bool?>((_, s) => s.TrySetResult(null));
        using var timeoutRegistration = timeoutToken.Register(
            state => ((TaskWatcher<bool?>)state).TaskCompletionSource.TrySetResult(false),
            state: watcher
        );
        // this is needed because if the connection goes down and never recovers, we'll never get the session expired notification
        using var connectionLostRegistration = connection.ConnectionLostToken.Register(
            state => ((TaskWatcher<bool?>)state).TaskCompletionSource.TrySetException(new InvalidOperationException("Lost connection to ZooKeeper")),
            state: watcher
        );

        var exists = await connection.ZooKeeper.existsAsync(path, watcher).ConfigureAwait(false);
        return exists == null ? true : await watcher.TaskCompletionSource.Task.ConfigureAwait(false);
    }

    private sealed class TaskWatcher<TResult> : Watcher, IDisposable
    {
        private volatile Action<WatchedEvent, TaskCompletionSource<TResult>> _watchedEventHandler;

        public TaskWatcher(Action<WatchedEvent, TaskCompletionSource<TResult>> watchedEventHandler)
        {
            _watchedEventHandler = watchedEventHandler;
        }

        public TaskCompletionSource<TResult> TaskCompletionSource { get; } = new();

        public void Dispose() => _watchedEventHandler = null;

        public override Task process(WatchedEvent @event)
        {
            // only care about connected state events; the ConnectionLostToken takes care of the other states for us
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                _watchedEventHandler?.Invoke(@event, TaskCompletionSource);
            }

            return Task.CompletedTask;
        }
    }

    private record HandleLostState(CancellationToken Token, CancellationTokenSource DisposalSource, Task MonitoringTask);
}