using Nerosoft.Euonia.Threading;
using org.apache.zookeeper;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// We don't want to use one session (<see cref="org.apache.zookeeper.ZooKeeper"/>) per lock because "The creation and closing of sessions are 
/// costly in ZooKeeper because they need quorum confirmations, they become the bottleneck of a ZooKeeper ensemble when it needs to handle 
/// thousands of client connections" (https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html). 
/// 
/// However, ZooKeeper sessions do "leak" watches over time (see https://issues.apache.org/jira/browse/ZOOKEEPER-442).
/// 
/// This class attempts to balance those two concerns by managing a pool of cached sessions that live for a while but not forever.
/// </summary>
internal class ZooKeeperConnection : IDisposable
{
    /// <summary>
    /// Hopefully, 10m prevents leaks from ever getting too bad while granting efficiencies by allowing us to re-use
    /// sessions under load.
    /// </summary>
    public static readonly Pool DefaultPool = new(maxAge: TimeSpan.FromMinutes(10));

    private readonly InternalConnection _internalConnection;
    private Action _releaseToPool;

    private ZooKeeperConnection(InternalConnection internalConnection, Action releaseToPool)
    {
        _internalConnection = internalConnection;
        _releaseToPool = releaseToPool;
    }

    public org.apache.zookeeper.ZooKeeper ZooKeeper => _internalConnection.ZooKeeper;

    public CancellationToken ConnectionLostToken => _internalConnection.ConnectionLostToken;

    public void Dispose() => Interlocked.Exchange(ref _releaseToPool, null)?.Invoke();

    public sealed class Pool
    {
        private readonly Dictionary<ZooKeeperConnectionInfo, ConnectionEntry> _connections = new();
        private readonly TimeoutValue _maxAge;

        public Pool(TimeoutValue maxAge)
        {
            Invariant.Require(!maxAge.IsInfinite);
            _maxAge = maxAge.TimeSpan;
        }

        // ReSharper disable once InconsistentlySynchronizedField
        private object PoolLock => global::Extensions.As<ICollection>((object)_connections).SyncRoot;

        public Task<ZooKeeperConnection> ConnectAsync(ZooKeeperConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (PoolLock)
            {
                // if we have an entry in the pool, use that
                if (_connections.TryGetValue(connectionInfo, out var entry))
                {
                    ++entry.UserCount;
                    return ToResultAsync(entry);
                }

                // create a new connection
                var newConnectionTask = InternalConnectAsync(connectionInfo);
                var newEntry = new ConnectionEntry(newConnectionTask)
                {
                    // 2 because we have both the current request and the timeout task we're about to create
                    UserCount = 2
                };
                _connections.Add(connectionInfo, newEntry);
                newConnectionTask.ContinueWith(OnConnectionTaskCompleted, cancellationToken);
                return ToResultAsync(newEntry);

                void OnConnectionTaskCompleted(Task<InternalConnection> internalConnectionTask)
                {
                    // if we never connected, just release our hold on the task
                    if (internalConnectionTask.Status != TaskStatus.RanToCompletion)
                    {
                        ReleaseEntry(connectionInfo, newEntry, remove: true);
                    }
                    // Otherwise, wait until either max age has elapsed or the connection is lost to release our hold. This
                    // ensures both that the connection won't be removed from the pool prematurely as well as that it will be
                    // eventually removed even if it stops being used
                    else
                    {
                        Task.Delay(_maxAge.TimeSpan, internalConnectionTask.Result.ConnectionLostToken)
                            .ContinueWith(_ => ReleaseEntry(connectionInfo, newEntry, remove: true), cancellationToken);
                    }
                }
            }

            async Task<ZooKeeperConnection> ToResultAsync(ConnectionEntry entry)
            {
                try
                {
                    var internalConnection = await entry.ConnectionTask.ConfigureAwait(false);
                    return new ZooKeeperConnection(internalConnection, releaseToPool: () => ReleaseEntry(connectionInfo, entry, remove: false));
                }
                catch
                {
                    // if we fail to construct a connection, still release our hold on the entry to allow cleanup
                    ReleaseEntry(connectionInfo, entry, remove: false);
                    throw;
                }
            }
        }

        private async Task<InternalConnection> InternalConnectAsync(ZooKeeperConnectionInfo connectionInfo)
        {
            var watcher = new ConnectionWatcher(connectionInfo.SessionTimeout);
            var zooKeeper = new org.apache.zookeeper.ZooKeeper(connectionInfo.ConnectionString, connectionInfo.SessionTimeout.InMilliseconds, watcher);

            using var timeoutSource = new CancellationTokenSource(connectionInfo.ConnectTimeout.TimeSpan);
            using var timeoutRegistration = timeoutSource.Token.Register(
                // ReSharper disable once AccessToDisposedClosure
                () => watcher.TaskCompletionSource.TrySetException(new TimeoutException($"Timed out connecting to ZooKeeper after {connectionInfo.ConnectTimeout.InMilliseconds}ms"))
            );

            foreach (var authInfo in connectionInfo.AuthInfo)
            {
                zooKeeper.addAuthInfo(authInfo.Scheme, authInfo.Auth.ToArray());
            }

            try
            {
                await watcher.TaskCompletionSource.Task.ConfigureAwait(false);
                return new InternalConnection(zooKeeper, watcher);
            }
            catch
            {
                // on failure, clean up the instance we created
                try
                {
                    await zooKeeper.closeAsync().ConfigureAwait(false);
                }
                finally
                {
                    watcher.Dispose();
                }

                throw;
            }
        }

        private void ReleaseEntry(ZooKeeperConnectionInfo connectionInfo, ConnectionEntry entry, bool remove)
        {
            bool shouldDispose;
            lock (PoolLock)
            {
                if (remove)
                {
                    global::Extensions.As<ICollection<KeyValuePair<ZooKeeperConnectionInfo, ConnectionEntry>>>((object)_connections)
                          .Remove(new KeyValuePair<ZooKeeperConnectionInfo, ConnectionEntry>(connectionInfo, entry));
                }

                shouldDispose = --entry.UserCount == 0;
                // this guarantee is upheld by the fact that we include the timeout watcher task as a "user"
                Invariant.Require(
                    !shouldDispose || !(_connections.TryGetValue(connectionInfo, out var registeredEntry) && registeredEntry == entry),
                    "If we're disposing then the entry must be removed from the pool"
                );
            }

            if (shouldDispose)
            {
                // kick off connection disposal in the background to
                // avoid blocking/throwing and to handle the case where the task hasn't yet completed
                entry.ConnectionTask.ContinueWith(
                    task =>
                    {
                        var _ = task.Result.DisposeAsync();
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
            }
        }

        private class ConnectionEntry
        {
            public ConnectionEntry(Task<InternalConnection> connectionTask)
            {
                ConnectionTask = connectionTask;
            }

            public Task<InternalConnection> ConnectionTask { get; }

            // protected by the pool's lock
            public int UserCount { get; set; }
        }
    }

    private class InternalConnection : IAsyncDisposable
    {
        private readonly ConnectionWatcher _watcher;

        public InternalConnection(org.apache.zookeeper.ZooKeeper zooKeeper, ConnectionWatcher watcher)
        {
            ZooKeeper = zooKeeper;
            _watcher = watcher;
        }

        public org.apache.zookeeper.ZooKeeper ZooKeeper { get; }

        public CancellationToken ConnectionLostToken => _watcher.ConnectionLost;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await ZooKeeper.closeAsync().ConfigureAwait(false);
            }
            finally
            {
                _watcher.Dispose();
            }
        }
    }

    private class ConnectionWatcher : Watcher, IDisposable
    {
        private readonly CancellationTokenSource _connectionLostSource = new();
        private readonly TimeoutValue _sessionTimeout;
        private int _isWaitingForReconnect;

        public ConnectionWatcher(TimeoutValue sessionTimeout)
        {
            _sessionTimeout = sessionTimeout;
        }

        public TaskCompletionSource<ValueTuple> TaskCompletionSource { get; } = new();
        public CancellationToken ConnectionLost => _connectionLostSource.Token;

        public override Task process(WatchedEvent @event)
        {
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                if (!TaskCompletionSource.TrySetResult(default))
                {
                    // if we're reconnecting, clear any scheduled cancellation
                    _connectionLostSource.CancelAfter(Timeout.Infinite);
                    Volatile.Write(ref _isWaitingForReconnect, 0);
                }
            }
            else
            {
                if (!TaskCompletionSource.TrySetException(new InvalidOperationException($"Failed to connect to ZooKeeper. State: {@event.getState()}")))
                {
                    // if we see the expired event (which might not come if we never reconnect), just fire connection lost
                    if (@event.getState() == Event.KeeperState.Expired)
                    {
                        _connectionLostSource.Cancel();
                    }
                    // Otherwise, zookeeper will be attempting reconnection. Give it up to the session timeout to
                    // do so before we fire. See https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html
                    else if (Interlocked.Exchange(ref _isWaitingForReconnect, 1) == 0)
                    {
                        // Only do this if we changed from !reconnecting to reconnecting; otherwise if we get multiple
                        // failure events we'll keep re-upping the timer
                        _connectionLostSource.CancelAfter(_sessionTimeout.TimeSpan);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose() => _connectionLostSource.Dispose();
    }
}