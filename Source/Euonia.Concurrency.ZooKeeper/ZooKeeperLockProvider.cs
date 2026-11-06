using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 基于 ZooKeeper 的 <see cref="ILockProvider"/> 实现。使用
/// https://zookeeper.apache.org/doc/r3.1.2/recipes.html 中描述的锁配方。
/// </summary>
public sealed partial class ZooKeeperLockProvider : ILockProvider<ZooKeeperSynchronizationHandle>
{
    private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

    /// <summary>
    /// 基于提供的 <paramref name="path"/>、<paramref name="connectionString"/> 和 <paramref name="options"/> 构造一个新锁。
    /// 
    /// 如果指定了 <paramref name="assumePathExists"/>，则在获取锁时不会创建该节点，释放后也不会删除它
    /// （默认为 false）。
    /// </summary>
    public ZooKeeperLockProvider(
        ZooKeeperPath path,
        string connectionString,
        bool assumePathExists = false,
        Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(path, assumePathExists: assumePathExists, connectionString, options)
    {
        if (path == default)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path == ZooKeeperPath.Root)
        {
            throw new ArgumentException(Resources.IDS_PATH_CAN_NOT_BE_THE_ROOT, nameof(path));
        }
    }

    /// <summary>
    /// 基于提供的 <paramref name="name"/>、<paramref name="connectionString"/> 和 <paramref name="options"/> 构造一个新锁。
    /// 锁的路径将是根目录 '/' 下的父节点。如果 <paramref name="name"/> 不是有效的节点名称，将对其进行转换以确保有效性。
    /// </summary>
    public ZooKeeperLockProvider(string name, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(ZooKeeperPath.Root, name, connectionString, options)
    {
    }

    /// <summary>
    /// 基于提供的 <paramref name="directoryPath"/>、<paramref name="name"/>、<paramref name="connectionString"/> 和
    /// <paramref name="options"/> 构造一个新锁。
    /// 
    /// 锁的路径将是 <paramref name="directoryPath"/> 下的父节点。如果 <paramref name="name"/> 不是有效的节点名称，
    /// 将对其进行转换以确保有效性。
    /// </summary>
    public ZooKeeperLockProvider(ZooKeeperPath directoryPath, string name, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(
            (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name),
            assumePathExists: false,
            connectionString,
            options)
    {
    }

    private ZooKeeperLockProvider(ZooKeeperPath path, bool assumePathExists, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? optionsBuilder) =>
        _synchronizationHelper = new ZooKeeperSynchronizationHelper(path, assumePathExists, connectionString, optionsBuilder);

    /// <summary>
    /// ZooKeeper 节点路径
    /// </summary>
    private ZooKeeperPath Path => _synchronizationHelper.Path;

    /// <summary>
    /// 实现 <see cref="ILockProvider.Name"/>。显式实现该接口，以避免与以下事实混淆：
    /// 该名称会同时包含前导的 "/" 和基础目录，而不仅是传入的名称。
    /// </summary>
    public string Name => Path.ToString();
}

/// <summary>
/// 提供 <see cref="ZooKeeperLockProvider"/> 的锁获取方法的分部实现。
/// </summary>
public sealed partial class ZooKeeperLockProvider
{
    /// <inheritdoc />
    public ZooKeeperSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ZooKeeperSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ZooKeeperSynchronizationHandle?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var nodeHandle = await _synchronizationHelper.TryAcquireAsync(
                                                         hasAcquired: state => state.SortedChildren[0].Path == state.EphemeralNodePath,
                                                         waitAsync: async (zooKeeper, state, watcher) =>
                                                         {
                                                             var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
                                                             var nextLowestChildNode = state.SortedChildren[ephemeralNodeIndex - 1].Path;
                                                             // 如果下一个更小的子节点已经不存在，则等待结束。否则，将监视器留在该节点上，
                                                             // 以便在其发生变化时收到通知（在此之前我们无法获取锁）
                                                             return await zooKeeper.existsAsync(nextLowestChildNode, watcher).ConfigureAwait(false) == null;
                                                         },
                                                         timeout,
                                                         cancellationToken,
                                                         nodePrefix: "lock-"
                                                     )
                                                     // 这里被迫使用 sync-over-async，因为 ZooKeeperNetEx 没有同步 API
                                                     .AwaitSyncOverAsync()
                                                     .ConfigureAwait(false);

        return nodeHandle != null ? new ZooKeeperSynchronizationHandle(nodeHandle) : null;
    }

    /// <inheritdoc />
    public ValueTask<ZooKeeperSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return TryAcquireAsync(new TimeoutValue(timeout), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ZooKeeperSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }
}

/// <summary>
/// 显式实现非泛型 <see cref="ILockProvider"/> 接口成员。
/// </summary>
public sealed partial class ZooKeeperLockProvider
{
    ISynchronizationHandle? ILockProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    ISynchronizationHandle ILockProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ValueTask<ISynchronizationHandle?> ILockProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ILockProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle?> ILockProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}