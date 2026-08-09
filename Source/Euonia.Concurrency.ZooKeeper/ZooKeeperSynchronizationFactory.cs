namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// Implements <see cref="ILockFactory"/> for <see cref="ZooKeeperLockProvider"/>,
/// and <see cref="ISemaphoreFactory"/> for <see cref="ZooKeeperSemaphoreProvider"/>.
/// </summary>
public sealed class ZooKeeperSynchronizationFactory : ILockFactory, ISemaphoreFactory
{
    private readonly ZooKeeperPath _directoryPath;
    private readonly string _connectionString;
    private readonly Action<ZooKeeperSynchronizationOptionsBuilder> _options;

    /// <summary>
    /// Constructs a provider which uses <paramref name="connectionString"/> and <paramref name="options"/>. Lock and semaphore nodes will be created
    /// in the root directory '/'.
    /// </summary>
    public ZooKeeperSynchronizationFactory(string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder> options = null)
        : this(ZooKeeperPath.Root, connectionString, options)
    {
    }

    /// <summary>
    /// Constructs a provider which uses <paramref name="connectionString"/> and <paramref name="options"/>. Lock and semaphore nodes will be created
    /// in <paramref name="directoryPath"/>.
    /// </summary>
    public ZooKeeperSynchronizationFactory(ZooKeeperPath directoryPath, string connectionString, Action<ZooKeeperSynchronizationOptionsBuilder> options = null)
    {
        _directoryPath = directoryPath != default ? directoryPath : throw new ArgumentNullException(nameof(directoryPath));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _options = options;
    }

    /// <summary>
    /// Creates a <see cref="ZooKeeperLockProvider"/> using the given <paramref name="name"/>.
    /// </summary>
    private ZooKeeperLockProvider Create(string name) => new(_directoryPath, name, _connectionString, _options);

    ILockProvider ILockFactory.Create(string name) => Create(name);

    /// <summary>
    /// Creates a <see cref="ZooKeeperSemaphoreProvider"/> using the given <paramref name="name"/> and <paramref name="maxCount"/>.
    /// </summary>
    private ZooKeeperSemaphoreProvider Create(string name, int maxCount) => new(_directoryPath, name, maxCount, _connectionString, _options);

    ISemaphoreProvider ISemaphoreFactory.Create(string name, int maxCount) => Create(name, maxCount);
}