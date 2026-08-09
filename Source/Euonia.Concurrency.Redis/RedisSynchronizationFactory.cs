using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// Implements <see cref="ILockFactory"/> for <see cref="RedisLockProvider"/>,
/// and <see cref="ISemaphoreFactory"/> for <see cref="RedisSemaphoreProvider"/>.
/// </summary>
public sealed class RedisSynchronizationFactory : ILockFactory, ISemaphoreFactory
{
    private readonly IReadOnlyList<IDatabase> _databases;
    private readonly Action<RedisSynchronizationOptionsBuilder> _options;

    /// <summary>
    /// Constructs a <see cref="RedisSynchronizationFactory"/> that connects to the provided <paramref name="database"/>
    /// and uses the provided <paramref name="options"/>.
    /// </summary>
    public RedisSynchronizationFactory(IDatabase database, Action<RedisSynchronizationOptionsBuilder> options = null)
        : this(new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
    {
    }

    /// <summary>
    /// Constructs a <see cref="RedisSynchronizationFactory"/> that connects to the provided <paramref name="databases"/>
    /// and uses the provided <paramref name="options"/>.
    /// 
    /// Note that if multiple <see cref="IDatabase"/>s are provided, <see cref="Create(StackExchange.Redis.RedisKey,int)"/> will use only the first
    /// <see cref="IDatabase"/>.
    /// </summary>
    public RedisSynchronizationFactory(IEnumerable<IDatabase> databases, Action<RedisSynchronizationOptionsBuilder> options = null)
    {
        _databases = RedisLockProvider.ValidateDatabases(databases);
        _options = options;
    }

    /// <summary>
    /// Creates a <see cref="RedisLockProvider"/> using the given <paramref name="key"/>.
    /// </summary>
    public RedisLockProvider Create(RedisKey key) => new(key, _databases, _options);

    ILockProvider ILockFactory.Create(string name) => Create(name);

    /// <summary>
    /// Creates a <see cref="RedisSemaphoreProvider"/> using the provided <paramref name="key"/> and <paramref name="maxCount"/>.
    /// </summary>
    public RedisSemaphoreProvider Create(RedisKey key, int maxCount) => new(key, maxCount, _databases[0], _options);

    ISemaphoreProvider ISemaphoreFactory.Create(string name, int maxCount) => Create(name, maxCount);
}