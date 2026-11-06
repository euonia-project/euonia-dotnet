using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 基于 ZooKeeper 的 <see cref="ISemaphoreProvider"/> 实现。采用与 <see cref="ZooKeeperLockProvider"/> 类似的方式。
/// </summary>
public sealed partial class ZooKeeperSemaphoreProvider : ISemaphoreProvider<ZooKeeperSynchronizationHandle>
{
    private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

    /// <summary>
    /// 基于提供的 <paramref name="path"/>、<paramref name="connectionString"/> 和 <paramref name="options"/> 构造一个新信号量。
    /// 
    /// 如果指定了 <paramref name="assumePathExists"/>，则在获取信号量时不会创建该节点，释放后也不会删除它
    /// （默认为 false）。
    /// </summary>
    public ZooKeeperSemaphoreProvider(ZooKeeperPath path, int maxCount, string connectionString, bool assumePathExists = false, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(path, maxCount, assumePathExists: assumePathExists, connectionString, options)
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
    /// 基于提供的 <paramref name="name"/>、<paramref name="connectionString"/> 和 <paramref name="options"/> 构造一个新信号量。
    /// 信号量的路径将是根目录 '/' 下的父节点。如果 <paramref name="name"/> 不是有效的节点名称，将对其进行转换以确保有效性。
    /// </summary>
    public ZooKeeperSemaphoreProvider(string name, int maxCount, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(ZooKeeperPath.Root, name, maxCount, connectionString, options)
    {
    }

    /// <summary>
    /// 基于提供的 <paramref name="directoryPath"/>、<paramref name="name"/>、<paramref name="connectionString"/> 和
    /// <paramref name="options"/> 构造一个新信号量。
    /// 
    /// 信号量的路径将是 <paramref name="directoryPath"/> 下的父节点。如果 <paramref name="name"/> 不是有效的节点名称，
    /// 将对其进行转换以确保有效性。
    /// </summary>
    public ZooKeeperSemaphoreProvider(ZooKeeperPath directoryPath, string name, int maxCount, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(
            (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name),
            maxCount,
            assumePathExists: false,
            connectionString,
            options)
    {
    }

    private ZooKeeperSemaphoreProvider(ZooKeeperPath nodePath, int maxCount, bool assumePathExists, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? optionsBuilder)
    {
        if (maxCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, ThreadingResources.IDS_VALUE_MUST_BE_POSITIVE);
        }

        MaxCount = maxCount;
        // 需要使用 setAcquiredMarker，因为下面的等待过程会利用数据变化
        _synchronizationHelper = new ZooKeeperSynchronizationHelper(nodePath, assumePathExists, connectionString, optionsBuilder, setAcquiredMarker: true);
    }

    /// <summary>
    /// ZooKeeper 节点路径
    /// </summary>
    public ZooKeeperPath Path => _synchronizationHelper.Path;

    /// <summary>
    /// 实现 <see cref="ISemaphoreProvider.Name"/>。显式实现该接口，以避免与以下事实混淆：
    /// 该名称会同时包含前导的 "/" 和基础目录，而不仅是传入的名称。
    /// </summary>
    public string Name => Path.ToString();

    /// <inheritdoc />
    public int MaxCount { get; }
}

/// <summary>
/// 提供 <see cref="ZooKeeperSemaphoreProvider"/> 的信号量获取方法的分部实现。
/// </summary>
public sealed partial class ZooKeeperSemaphoreProvider
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
    public ValueTask<ZooKeeperSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return TryAcquireAsync(new TimeoutValue(timeout), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ZooKeeperSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ZooKeeperSynchronizationHandle?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var nodeHandle = await _synchronizationHelper.TryAcquireAsync(
                                                         hasAcquired: state => Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath) < MaxCount,
                                                         waitAsync: async (zooKeeper, state, watcher) =>
                                                         {
                                                             var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
                                                             Invariant.Require(ephemeralNodeIndex >= MaxCount);

                                                             // 如果我们是下一个排队等待许可的节点，则等待子节点集合中的任何变化
                                                             if (ephemeralNodeIndex == MaxCount)
                                                             {
                                                                 var childNames = new HashSet<string>((await zooKeeper.getChildrenAsync(Path.ToString(), watcher).ConfigureAwait(false)).Children);
                                                                 // 如果排在我们前面的任何子节点已不存在，则等待结束。否则，
                                                                 // 让监视器在子节点集合发生任何变化时通知我们
                                                                 return state.SortedChildren.Take(ephemeralNodeIndex)
                                                                             .Any(t => !childNames.Contains(t.Path[(t.Path.LastIndexOf(ZooKeeperPath.Separator) + 1)..]));
                                                             }

                                                             // 否则，我们只监视排在我们前面的节点，等待其数据变为已获取标记。虽然在这种情况下我们也可以
                                                             // 监视所有子节点，但那种方式效率较低，因为它会产生羊群效应，每个新的等待者或刚释放的等待者都会唤醒其他所有人。
                                                             var nextLowestChildData = await zooKeeper.getDataAsync(state.SortedChildren[ephemeralNodeIndex - 1].Path, watcher).ConfigureAwait(false);
                                                             // 如果它已被获取，则等待结束。否则，监视器会在任何数据变化或该节点被删除时通知我们
                                                             return nextLowestChildData.Data.SequenceEqual(ZooKeeperSynchronizationHelper.AcquiredMarker);
                                                         },
                                                         timeout,
                                                         cancellationToken,
                                                         nodePrefix: "semaphore-"
                                                     )
                                                     // 这里被迫使用 sync-over-async，因为 ZooKeeperNetEx 没有同步 API
                                                     .AwaitSyncOverAsync()
                                                     .ConfigureAwait(false);

        return nodeHandle != null ? new ZooKeeperSynchronizationHandle(nodeHandle) : null;
    }
}

/// <summary>
/// 显式实现非泛型 <see cref="ISemaphoreProvider"/> 接口成员。
/// </summary>
public sealed partial class ZooKeeperSemaphoreProvider
{
    ISynchronizationHandle ISemaphoreProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ISynchronizationHandle? ISemaphoreProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    ValueTask<ISynchronizationHandle?> ISemaphoreProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ISemaphoreProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle?> ISemaphoreProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).ConvertNullable(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}