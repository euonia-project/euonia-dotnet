using Nerosoft.Euonia.Threading;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;

/// <summary>
/// 为 ZooKeeper 同步算法提供通用结构。该结构为：
/// * 创建一个顺序临时节点
/// * 循环
///     * 将本节点的序列号与同级节点比较，以判断是否已获取
///     * 如果尚未获取，则等待监视器通知有内容发生变化
/// </summary>
internal class ZooKeeperSynchronizationHelper
{
    /// <summary>
    /// 获取成功后写入节点数据的内容，用于让其他观察者识别该节点已被获取。
    /// </summary>
    public static readonly IReadOnlyList<byte> AcquiredMarker = "ACQUIRED"u8.ToArray();

    private readonly ZooKeeperConnectionInfo _connectionInfo;
    private readonly IReadOnlyList<ACL> _acl;
    private readonly bool _assumePathExists, _setAcquiredMarker;

    /// <summary>
    /// 初始化同步算法提供程序。
    /// </summary>
    /// <param name="path">参与排队的 ZooKeeper 节点路径。</param>
    /// <param name="assumePathExists">是否假定路径已存在；为 <see langword="false"/> 时会尝试自动创建目录并在释放时清理。</param>
    /// <param name="connectionString">ZooKeeper 连接串。</param>
    /// <param name="optionsBuilder">用于配置超时、认证与访问控制的委托。</param>
    /// <param name="setAcquiredMarker">获取成功后是否把 <see cref="AcquiredMarker"/> 写入节点数据。</param>
    public ZooKeeperSynchronizationHelper(
        ZooKeeperPath path,
        bool assumePathExists,
        string connectionString,
        Action<ZooKeeperSynchronizationOptionsBuilder>? optionsBuilder,
        bool setAcquiredMarker = false)
    {
        Path = path;
        _assumePathExists = assumePathExists;
        var options = ZooKeeperSynchronizationOptionsBuilder.GetOptions(optionsBuilder);
        _connectionInfo = new ZooKeeperConnectionInfo(
            connectionString ?? throw new ArgumentNullException(nameof(connectionString)),
            ConnectTimeout: options.ConnectTimeout,
            SessionTimeout: options.SessionTimeout,
            AuthInfo: options.AuthInfo
        );
        _acl = options.Acl;
        _setAcquiredMarker = setAcquiredMarker;
    }

    /// <summary>
    /// 获取参与排队的 ZooKeeper 节点路径。
    /// </summary>
    public ZooKeeperPath Path { get; }

    /// <summary>
    /// 创建顺序临时节点并循环比较同级节点，直到本节点成为最旧的一个或等待超时。
    /// </summary>
    /// <param name="hasAcquired">根据当前节点状态判断是否已获取同步对象。</param>
    /// <param name="waitAsync">注册监视并等待状态变化的回调；返回 <see langword="true"/> 表示应立即重新检查。</param>
    /// <param name="timeout">整个获取过程允许等待的最长时间。</param>
    /// <param name="cancellationToken">用于取消获取过程的取消令牌。</param>
    /// <param name="nodePrefix">本次获取创建的顺序节点名称前缀。</param>
    /// <param name="alternateNodePrefix">可选的备用节点名称前缀，用于兼容旧版本创建的节点。</param>
    /// <returns>获取成功时返回持有临时节点的句柄；等待超时时返回 <see langword="null"/>。</returns>
    /// <exception cref="InvalidOperationException">创建的节点意外消失，或等待期间与 ZooKeeper 的连接丢失。</exception>
    /// <exception cref="OperationCanceledException">取消令牌已触发。</exception>
    public async Task<ZooKeeperNodeHandle?> TryAcquireAsync(
        Func<State, bool> hasAcquired,
        Func<org.apache.zookeeper.ZooKeeper, State, Watcher, Task<bool>> waitAsync,
        TimeoutValue timeout,
        CancellationToken cancellationToken,
        string nodePrefix,
        string? alternateNodePrefix = null)
    {
        var acquired = false;
        var ephemeralNodeLost = false;
        ZooKeeperConnection? connection = null;
        string? ephemeralNodePath = null;
        var timeoutSource = new CancellationTokenSource(timeout.TimeSpan);

        try
        {
            connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(_connectionInfo, cancellationToken).ConfigureAwait(false);

            // 创建一个路径，它既表示我们对同步对象的持有，也表示我们排队等待的位置
            ephemeralNodePath = await connection.CreateEphemeralSequentialNode(Path, nodePrefix, _acl, ensureDirectoryExists: !_assumePathExists).ConfigureAwait(false);

            while (true)
            {
                // 获取该节点的子节点，按前缀筛选并按存在时间排序；我们将用它来确定自己在列表中的位置
                var children = await connection.ZooKeeper.getChildrenAsync(Path.ToString()).ConfigureAwait(false);
                var sortedChildren = await ZooKeeperSequentialPathHelper.FilterAndSortAsync(
                    parentNode: Path.ToString(),
                    childrenNames: children.Children,
                    getNodeCreationTimeAsync: connection.GetNodeCreationTimeAsync,
                    prefix: nodePrefix,
                    alternatePrefix: alternateNodePrefix
                ).ConfigureAwait(false);

                // 合理性检查；如果有人在我们不知情的情况下删除了它，就可能出现这种情况。必须先检查这一点，
                // 因为它覆盖了集合为空的情形
                if (!sortedChildren.Any(t => t.Path == ephemeralNodePath))
                {
                    ephemeralNodeLost = true;
                    throw new InvalidOperationException(string.Format(Resources.IDS_NODE_CREATED_BUT_NO_LONGER_EXISTS, ephemeralNodePath));
                }

                // 查看是否已获取
                var state = new State(ephemeralNodePath, sortedChildren);
                if (hasAcquired(state))
                {
                    if (_setAcquiredMarker)
                    {
                        await connection.ZooKeeper.setDataAsync(ephemeralNodePath, AcquiredMarker.ToArray()).ConfigureAwait(false);
                    }

                    acquired = true;
                    return new ZooKeeperNodeHandle(connection, ephemeralNodePath, shouldDeleteParent: !_assumePathExists);
                }

                // 等待有内容发生变化
                var waitCompletionSource = new TaskCompletionSource<bool>();
                using var timeoutRegistration = timeoutSource.Token.Register(() => waitCompletionSource.TrySetResult(false));
                using var cancellationRegistration = cancellationToken.Register(() => waitCompletionSource.TrySetCanceled());
                // 这是必需的，因为如果连接断开且始终未恢复，我们将永远收不到会话过期通知
                using var connectionLostRegistration = connection.ConnectionLostToken.Register(
                    () => waitCompletionSource.TrySetException(new InvalidOperationException("Lost connection to ZooKeeper"))
                );
                if (!waitCompletionSource.Task.IsCompleted
                    && await waitAsync(connection.ZooKeeper, state, new WaitCompletionSourceWatcher(waitCompletionSource)).ConfigureAwait(false))
                {
                    waitCompletionSource.TrySetResult(true);
                }

                if (!await waitCompletionSource.Task.ConfigureAwait(false))
                {
                    return null; // 等待超时
                }
            }
        }
        finally
        {
            timeoutSource.Dispose();

            // 如果获取失败，清理连接/节点路径
            if (!acquired)
            {
                await CleanUpOnFailureAsync(connection, ephemeralNodePath, ephemeralNodeLost).ConfigureAwait(false);
            }
        }
    }

    private async Task CleanUpOnFailureAsync(ZooKeeperConnection? connection, string? ephemeralNodePath, bool ephemeralNodeLost)
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            if (ephemeralNodePath != null)
            {
                if (!ephemeralNodeLost)
                {
                    await connection.ZooKeeper.deleteAsync(ephemeralNodePath).ConfigureAwait(false);
                }

                if (!_assumePathExists)
                {
                    // 如果父节点应被清理，则尝试清理。此尝试几乎肯定会失败，因为其他人正持有锁。不过，我们可能遇到
                    // 一种竞态条件：另一个持有者在我们获取失败后立即释放，而我们的临时节点阻止了它们删除。因此，
                    // 我们以发后即忘的方式执行此删除，以覆盖该情形而不拖慢自身
                    _ = connection.ZooKeeper.deleteAsync(Path.ToString());
                }
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    /// <summary>
    /// 在收到已连接状态事件时完成等待任务的监视器，用于唤醒获取循环重新检查节点状态。
    /// </summary>
    private sealed class WaitCompletionSourceWatcher : Watcher
    {
        private readonly TaskCompletionSource<bool> _waitCompletionSource;

        public WaitCompletionSourceWatcher(TaskCompletionSource<bool> waitCompletionSource)
        {
            _waitCompletionSource = waitCompletionSource;
        }

        public override Task process(WatchedEvent @event)
        {
            // 只关心已连接状态事件；其他状态由 ConnectionLostToken 为我们处理
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                _waitCompletionSource.TrySetResult(true);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 获取循环中使用的节点状态快照。
    /// </summary>
    /// <param name="EphemeralNodePath">本次调用创建的临时节点路径。</param>
    /// <param name="SortedChildren">按从旧到新排序的同前缀子节点集合。</param>
    public record State(string EphemeralNodePath, (string Path, int SequenceNumber, string Prefix)[] SortedChildren);
}