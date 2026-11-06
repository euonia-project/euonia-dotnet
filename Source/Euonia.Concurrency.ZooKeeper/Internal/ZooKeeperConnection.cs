using Nerosoft.Euonia.Threading;
using org.apache.zookeeper;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// 我们不希望为每个锁使用一个会话（<see cref="org.apache.zookeeper.ZooKeeper"/>），因为“会话的创建与关闭在 ZooKeeper 中开销很大，
/// 它们需要法定人数确认，当需要处理数千个客户端连接时，它们会成为 ZooKeeper 集群的瓶颈”
/// （https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html）。
/// 
/// 然而，ZooKeeper 会话确实会随时间“泄漏”监视点（watch，参见 https://issues.apache.org/jira/browse/ZOOKEEPER-442）。
/// 
/// 本类通过管理一个缓存会话池来平衡这两个问题，池中的会话会存活一段时间，但不会永久存活。
/// </summary>
internal class ZooKeeperConnection : IDisposable
{
    /// <summary>
    /// 希望 10 分钟既能避免泄漏变得过于严重，又能通过允许在高负载下复用会话来提高效率。
    /// </summary>
    public static readonly Pool DefaultPool = new(maxAge: TimeSpan.FromMinutes(10));

    private readonly InternalConnection _internalConnection;
    private Action? _releaseToPool;

    private ZooKeeperConnection(InternalConnection internalConnection, Action releaseToPool)
    {
        _internalConnection = internalConnection;
        _releaseToPool = releaseToPool;
    }

    /// <summary>
    /// 获取底层的 ZooKeeper 客户端实例。
    /// </summary>
    public org.apache.zookeeper.ZooKeeper ZooKeeper => _internalConnection.ZooKeeper;

    /// <summary>
    /// 获取在连接丢失或会话过期后触发的取消令牌。
    /// </summary>
    public CancellationToken ConnectionLostToken => _internalConnection.ConnectionLostToken;

    /// <summary>
    /// 释放本次连接引用并将其归还连接池；重复调用不会产生额外效果。
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref _releaseToPool, null)?.Invoke();

    /// <summary>
    /// 缓存 ZooKeeper 连接的池：按连接信息复用连接，并在超过最大存活时间或连接丢失后移除条目。
    /// </summary>
    public sealed class Pool
    {
        private readonly Dictionary<ZooKeeperConnectionInfo, ConnectionEntry> _connections = new();
        private readonly TimeoutValue _maxAge;

        /// <summary>
        /// 初始化连接池。
        /// </summary>
        /// <param name="maxAge">池中连接的最长缓存存活时间，不能为无限。</param>
        public Pool(TimeoutValue maxAge)
        {
            Invariant.Require(!maxAge.IsInfinite);
            _maxAge = maxAge.TimeSpan;
        }

        // ReSharper disable once InconsistentlySynchronizedField
        private object PoolLock => global::Extensions.As<ICollection>((object)_connections).SyncRoot;

        /// <summary>
        /// 获取与指定连接信息对应的连接：命中缓存则复用并增加引用计数，否则新建连接并加入池中。
        /// </summary>
        /// <param name="connectionInfo">目标 ZooKeeper 的连接信息，同时作为池的缓存键。</param>
        /// <param name="cancellationToken">用于取消等待连接建立的取消令牌。</param>
        /// <returns>可释放的连接包装；释放时归还本次引用计数。</returns>
        public Task<ZooKeeperConnection> ConnectAsync(ZooKeeperConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (PoolLock)
            {
                // 如果池中已有条目，则使用该条目
                if (_connections.TryGetValue(connectionInfo, out var entry))
                {
                    ++entry.UserCount;
                    return ToResultAsync(entry);
                }

                // 创建新连接
                var newConnectionTask = InternalConnectAsync(connectionInfo);
                var newEntry = new ConnectionEntry(newConnectionTask)
                {
                    // 为 2 是因为既包括当前请求，也包括即将创建的超时任务
                    UserCount = 2
                };
                _connections.Add(connectionInfo, newEntry);
                // 不能把调用方的取消令牌传给池的簿记续延：调用方取消时续延会被跳过，
                // 失败的连接会永久留在池中，之后同连接串的所有申请都会复用它并持续失败。
                newConnectionTask.ContinueWith(OnConnectionTaskCompleted);
                return ToResultAsync(newEntry);

                void OnConnectionTaskCompleted(Task<InternalConnection> internalConnectionTask)
                {
                    // 如果从未连接成功，只需释放我们对任务的持有
                    if (internalConnectionTask.Status != TaskStatus.RanToCompletion)
                    {
                        ReleaseEntry(connectionInfo, newEntry, remove: true);
                    }
                    // 否则，等待最大存活时间耗尽或连接丢失后再释放持有。这样既能保证连接不会被过早地从池中移除，
                    // 也能保证即使连接不再被使用，最终仍会被移除
                    else
                    {
                        Task.Delay(_maxAge.TimeSpan, internalConnectionTask.Result.ConnectionLostToken)
                            .ContinueWith(_ => ReleaseEntry(connectionInfo, newEntry, remove: true));
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
                    // 如果构造连接失败，仍然释放对条目的持有以便进行清理
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
                // 失败时，清理我们创建的实例
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
                // 这一保证之所以成立，是因为我们把超时监视任务也计为一个“使用者”
                Invariant.Require(
                    !shouldDispose || !(_connections.TryGetValue(connectionInfo, out var registeredEntry) && registeredEntry == entry),
                    "If we're disposing then the entry must be removed from the pool"
                );
            }

            if (shouldDispose)
            {
                // 在后台启动连接释放，以避免阻塞/抛出异常，
                // 并处理任务尚未完成的情况
                entry.ConnectionTask.ContinueWith(
                    task =>
                    {
                        var _ = task.Result.DisposeAsync();
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
            }
        }

        /// <summary>
        /// 连接池中的条目，记录连接的创建任务与当前引用计数。
        /// </summary>
        private class ConnectionEntry
        {
            public ConnectionEntry(Task<InternalConnection> connectionTask)
            {
                ConnectionTask = connectionTask;
            }

            public Task<InternalConnection> ConnectionTask { get; }

            // 由池的锁保护
            public int UserCount { get; set; }
        }
    }

    /// <summary>
    /// 池中实际持有的连接，封装 ZooKeeper 客户端及其连接状态监视器。
    /// </summary>
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

    /// <summary>
    /// 监视 ZooKeeper 连接状态：首次连接成功时完成初始化任务，重连超时或会话过期时触发连接丢失。
    /// </summary>
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
                    // 如果正在重连，则清除任何已安排的取消
                    _connectionLostSource.CancelAfter(Timeout.Infinite);
                    Volatile.Write(ref _isWaitingForReconnect, 0);
                }
            }
            else
            {
                if (!TaskCompletionSource.TrySetException(new InvalidOperationException($"Failed to connect to ZooKeeper. State: {@event.getState()}")))
                {
                    // 如果看到过期事件（如果我们从未重连成功，该事件可能不会到来），就直接触发连接丢失
                    if (@event.getState() == Event.KeeperState.Expired)
                    {
                        _connectionLostSource.Cancel();
                    }
                    // 否则，ZooKeeper 将尝试重连。在触发之前，最多给它一个会话超时的时间。参见
                    // https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html
                    else if (Interlocked.Exchange(ref _isWaitingForReconnect, 1) == 0)
                    {
                        // 仅当我们从“非重连”状态切换到“重连”状态时才这样做；否则如果收到多个失败事件，
                        // 我们会不断延长计时器
                        _connectionLostSource.CancelAfter(_sessionTimeout.TimeSpan);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose() => _connectionLostSource.Dispose();
    }
}