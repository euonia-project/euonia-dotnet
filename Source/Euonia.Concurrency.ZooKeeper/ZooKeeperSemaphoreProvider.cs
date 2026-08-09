using Nerosoft.Euonia.Concurrency.ZooKeeper.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// An implementation of <see cref="ISemaphoreProvider"/> based on ZooKeeper. Uses an approach similar to <see cref="ZooKeeperLockProvider"/>.
/// </summary>
public sealed partial class ZooKeeperSemaphoreProvider : ISemaphoreProvider<ZooKeeperSynchronizationHandle>
{
    private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

    /// <summary>
    /// Constructs a new semaphore based on the provided <paramref name="path"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// 
    /// If <paramref name="assumePathExists"/> is specified, then the node will not be created as part of acquiring nor will it be 
    /// deleted after releasing (defaults to false).
    /// </summary>
    public ZooKeeperSemaphoreProvider(ZooKeeperPath path, int maxCount, string connectionString, bool assumePathExists = false, Action<ZooKeeperSynchronizationOptionsBuilder> options = null)
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
    /// Constructs a new semaphore based on the provided <paramref name="name"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// The semaphore's path will be a parent node of the root directory '/'. If <paramref name="name"/> is not a valid node name, it will be transformed to ensure
    /// validity.
    /// </summary>
    public ZooKeeperSemaphoreProvider(string name, int maxCount, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder> options = null)
        : this(ZooKeeperPath.Root, name, maxCount, connectionString, options)
    {
    }

    /// <summary>
    /// Constructs a new semaphore based on the provided <paramref name="directoryPath"/>, <paramref name="name"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// 
    /// The semaphore's path will be a parent node of <paramref name="directoryPath"/>. If <paramref name="name"/> is not a valid node name, it will be transformed to ensure
    /// validity.
    /// </summary>
    public ZooKeeperSemaphoreProvider(ZooKeeperPath directoryPath, string name, int maxCount, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder> options = null)
        : this(
            (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name),
            maxCount,
            assumePathExists: false,
            connectionString,
            options)
    {
    }

    private ZooKeeperSemaphoreProvider(ZooKeeperPath nodePath, int maxCount, bool assumePathExists, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder> optionsBuilder)
    {
        if (maxCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, ThreadingResources.IDS_VALUE_MUST_BE_POSITIVE);
        }

        MaxCount = maxCount;
        // setAcquiredMarker is needed because we use data changes as part of our wait procedure below
        _synchronizationHelper = new ZooKeeperSynchronizationHelper(nodePath, assumePathExists, connectionString, optionsBuilder, setAcquiredMarker: true);
    }

    /// <summary>
    /// The zookeeper node path
    /// </summary>
    public ZooKeeperPath Path => _synchronizationHelper.Path;

    /// <summary>
    /// Implements <see cref="ISemaphoreProvider.Name"/>. Implemented explicitly to avoid confusion with the fact
    /// that this will include the leading "/" and base directory alongside the passed-in name.
    /// </summary>
    public string Name => Path.ToString();

    /// <summary>
    /// Implements <see cref="ISemaphoreProvider.MaxCount"/>
    /// </summary>
    public int MaxCount { get; }
}

public sealed partial class ZooKeeperSemaphoreProvider
{
    /// <inheritdoc />
    public ZooKeeperSynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ZooKeeperSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ZooKeeperSynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return this.As<ISemaphoreProvider<ZooKeeperSynchronizationHandle>>().TryAcquireAsync(timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ZooKeeperSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ZooKeeperSynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var nodeHandle = await _synchronizationHelper.TryAcquireAsync(
                                                         hasAcquired: state => Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath) < MaxCount,
                                                         waitAsync: async (zooKeeper, state, watcher) =>
                                                         {
                                                             var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
                                                             Invariant.Require(ephemeralNodeIndex >= MaxCount);

                                                             // if we're the next node in line for a ticket, wait for any changes in the collection of children
                                                             if (ephemeralNodeIndex == MaxCount)
                                                             {
                                                                 var childNames = new HashSet<string>((await zooKeeper.getChildrenAsync(Path.ToString(), watcher).ConfigureAwait(false)).Children);
                                                                 // If any of the children in front of us are missing, then the wait is done. Otherwise,
                                                                 // let the watcher notify us when there is any change to the set of children
                                                                 return state.SortedChildren.Take(ephemeralNodeIndex)
                                                                             .Any(t => !childNames.Contains(t.Path[(t.Path.LastIndexOf(ZooKeeperPath.Separator) + 1)..]));
                                                             }

                                                             // Otherwise, we just watch for the node ahead of us in line to have its data changed to the acquired marker. While we could
                                                             // watch all children in this case as well, that approach is less efficient because it will generate a herd effect where each
                                                             // new waiter or released waiter wakes up everyone else.
                                                             var nextLowestChildData = await zooKeeper.getDataAsync(state.SortedChildren[ephemeralNodeIndex - 1].Path, watcher).ConfigureAwait(false);
                                                             // If it's already acquired, then the wait is done. Otherwise, the watcher will notify us on any data change or on deletion of that node
                                                             return nextLowestChildData.Data.SequenceEqual(ZooKeeperSynchronizationHelper.AcquiredMarker);
                                                         },
                                                         timeout,
                                                         cancellationToken,
                                                         nodePrefix: "semaphore-"
                                                     )
                                                     // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
                                                     .AwaitSyncOverAsync()
                                                     .ConfigureAwait(false);

        return nodeHandle != null ? new ZooKeeperSynchronizationHandle(nodeHandle) : null;
    }
}

public sealed partial class ZooKeeperSemaphoreProvider
{
    ISynchronizationHandle ISemaphoreProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ISynchronizationHandle ISemaphoreProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    ValueTask<ISynchronizationHandle> ISemaphoreProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ISemaphoreProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }

    ValueTask<ISynchronizationHandle> ISemaphoreProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
    }
}