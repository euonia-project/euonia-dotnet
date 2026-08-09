using Nerosoft.Euonia.Concurrency.Redis.Internal;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// Implements a <see cref="ISemaphoreProvider"/> using Redis.
/// </summary>
public sealed partial class RedisSemaphoreProvider : ISemaphoreProvider<RedisSynchronizationHandle>
{
    /// <summary>
    /// Note: while we store this as a list to simplify the interactions with the RedLock components, in fact the semaphore
    /// algorithm only works with a single database. With multiple databases, we risk violating our <see cref="MaxCount"/>.
    /// For example, with 3 dbs and 2 tickets, we can have 3 users acquiring AB, BC, and AC. Each database sees 2 tickets taken!
    /// </summary>
    private readonly IReadOnlyList<IDatabase> _databases;

    private readonly RedisSynchronizationOptions _options;

    /// <summary>
    /// Constructs a semaphore named <paramref name="key"/> using the provided <paramref name="maxCount"/>, <paramref name="database"/>, and <paramref name="options"/>.
    /// </summary>
    public RedisSemaphoreProvider(RedisKey key, int maxCount, IDatabase database, Action<RedisSynchronizationOptionsBuilder> options = null)
    {
        if (key == default(RedisKey))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (maxCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, ThreadingResources.IDS_VALUE_MUST_BE_POSITIVE);
        }

        _databases = new[] { database ?? throw new ArgumentNullException(nameof(database)) };

        Key = key;
        MaxCount = maxCount;
        _options = RedisSynchronizationOptionsBuilder.GetOptions(options);
    }

    private RedisKey Key { get; }

    /// <summary>
    /// Implements <see cref="ISemaphoreProvider.Name"/>
    /// </summary>
    public string Name => Key.ToString();

    /// <summary>
    /// Implements <see cref="ISemaphoreProvider.MaxCount"/>
    /// </summary>
    public int MaxCount { get; }
}

public sealed partial class RedisSemaphoreProvider
{
    /// <inheritdoc />
    public RedisSynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RedisSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return this.As<ISemaphoreProvider<RedisSynchronizationHandle>>().TryAcquireAsync(timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        return BusyWaitHelper.WaitAsync(
            state: this,
            tryGetValue: (@this, token) => @this.TryAcquireAsync(token),
            timeout: timeout,
            minSleepTime: _options.MinBusyWaitSleepTime,
            maxSleepTime: _options.MaxBusyWaitSleepTime,
            cancellationToken: cancellationToken
        );
    }

    private async ValueTask<RedisSynchronizationHandle> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var primitive = new RedisSemaphorePrimitive(Key, MaxCount, _options.RedisLockTimeouts);
        var tryAcquireTasks = await new RedisLockAcquire(primitive, _databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
        return tryAcquireTasks != null
            ? new RedisSynchronizationHandle(new RedisLockHandle(primitive, tryAcquireTasks, extensionCadence: _options.ExtensionCadence, expiry: _options.RedisLockTimeouts.Expiry))
            : null;
    }
}

public sealed partial class RedisSemaphoreProvider
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