using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// 为 <see cref="RedisLockProvider"/> 实现 <see cref="ILockFactory"/>，
/// 并为 <see cref="RedisSemaphoreProvider"/> 实现 <see cref="ISemaphoreFactory"/>。
/// </summary>
public sealed class RedisSynchronizationFactory : ILockFactory, ISemaphoreFactory
{
    private readonly IReadOnlyList<IDatabase> _databases;
    private readonly Action<RedisSynchronizationOptionsBuilder>? _options;

    /// <summary>
    /// 构造一个连接到所提供的 <paramref name="database"/> 并使用所提供的
    /// <paramref name="options"/> 的 <see cref="RedisSynchronizationFactory"/>。
    /// </summary>
    public RedisSynchronizationFactory(IDatabase database, Action<RedisSynchronizationOptionsBuilder>? options = null)
        : this(new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
    {
    }

    /// <summary>
    /// 构造一个连接到所提供的 <paramref name="databases"/> 并使用所提供的
    /// <paramref name="options"/> 的 <see cref="RedisSynchronizationFactory"/>。
    /// 
    /// 注意，如果提供了多个 <see cref="IDatabase"/>，则 <see cref="Create(StackExchange.Redis.RedisKey,int)"/> 将只使用第一个
    /// <see cref="IDatabase"/>。
    /// </summary>
    public RedisSynchronizationFactory(IEnumerable<IDatabase> databases, Action<RedisSynchronizationOptionsBuilder>? options = null)
    {
        _databases = RedisLockProvider.ValidateDatabases(databases);
        _options = options;
    }

    /// <summary>
    /// 使用给定的 <paramref name="key"/> 创建 <see cref="RedisLockProvider"/>。
    /// </summary>
    public RedisLockProvider Create(RedisKey key) => new(key, _databases, _options);

    ILockProvider ILockFactory.Create(string name) => Create(name);

    /// <summary>
    /// 使用所提供的 <paramref name="key"/> 和 <paramref name="maxCount"/> 创建 <see cref="RedisSemaphoreProvider"/>。
    /// </summary>
    public RedisSemaphoreProvider Create(RedisKey key, int maxCount) => new(key, maxCount, _databases[0], _options);

    ISemaphoreProvider ISemaphoreFactory.Create(string name, int maxCount) => Create(name, maxCount);
}