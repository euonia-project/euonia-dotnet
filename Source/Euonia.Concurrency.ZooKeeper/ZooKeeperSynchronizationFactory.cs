namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 为 <see cref="ZooKeeperLockProvider"/> 实现 <see cref="ILockFactory"/>，
/// 并为 <see cref="ZooKeeperSemaphoreProvider"/> 实现 <see cref="ISemaphoreFactory"/>。
/// </summary>
public sealed class ZooKeeperSynchronizationFactory : ILockFactory, ISemaphoreFactory
{
    private readonly ZooKeeperPath _directoryPath;
    private readonly string _connectionString;
    private readonly Action<ZooKeeperSynchronizationOptionsBuilder>? _options;

    /// <summary>
    /// 构造使用 <paramref name="connectionString"/> 和 <paramref name="options"/> 的提供程序。锁与信号量节点将创建在
    /// 根目录 '/' 中。
    /// </summary>
    public ZooKeeperSynchronizationFactory(string? connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
        : this(ZooKeeperPath.Root, connectionString, options)
    {
    }

    /// <summary>
    /// 构造使用 <paramref name="connectionString"/> 和 <paramref name="options"/> 的提供程序。锁与信号量节点将创建在
    /// <paramref name="directoryPath"/> 中。
    /// </summary>
    public ZooKeeperSynchronizationFactory(ZooKeeperPath directoryPath, string? connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)
    {
        _directoryPath = directoryPath != default ? directoryPath : throw new ArgumentNullException(nameof(directoryPath));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _options = options;
    }

    /// <summary>
    /// 使用给定的 <paramref name="name"/> 创建 <see cref="ZooKeeperLockProvider"/>。
    /// </summary>
    private ZooKeeperLockProvider Create(string name) => new(_directoryPath, name, _connectionString, _options);

    ILockProvider ILockFactory.Create(string name) => Create(name);

    /// <summary>
    /// 使用给定的 <paramref name="name"/> 和 <paramref name="maxCount"/> 创建 <see cref="ZooKeeperSemaphoreProvider"/>。
    /// </summary>
    private ZooKeeperSemaphoreProvider Create(string name, int maxCount) => new(_directoryPath, name, maxCount, _connectionString, _options);

    ISemaphoreProvider ISemaphoreFactory.Create(string name, int maxCount) => Create(name, maxCount);
}