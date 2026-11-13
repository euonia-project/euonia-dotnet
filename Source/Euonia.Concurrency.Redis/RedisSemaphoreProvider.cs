using Nerosoft.Euonia.Concurrency.Redis.Internal;
using Nerosoft.Euonia.Threading;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// 使用 Redis 实现 <see cref="ISemaphoreProvider"/>。
/// </summary>
public sealed partial class RedisSemaphoreProvider : ISemaphoreProvider<RedisSynchronizationHandle>
{
    /// <summary>
    /// 注意：虽然我们将其存储为列表以简化与 RedLock 组件的交互，但实际上信号量
    /// 算法只适用于单个数据库。使用多个数据库时，我们可能会违反 <see cref="MaxCount"/>。
    /// 例如，有 3 个数据库和 2 张票时，可以有 3 个用户分别获取 AB、BC 和 AC。每个数据库都看到 2 张票被占用！
    /// </summary>
    private readonly IReadOnlyList<IDatabase> _databases;

    private readonly RedisSynchronizationOptions _options;

    /// <summary>
    /// 使用提供的 <paramref name="maxCount"/>、<paramref name="database"/> 和 <paramref name="options"/> 构造名为 <paramref name="key"/> 的信号量。
    /// </summary>
    public RedisSemaphoreProvider(RedisKey key, int maxCount, IDatabase database, Action<RedisSynchronizationOptionsBuilder>? options = null)
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

    /// <inheritdoc />
    public string Name => Key.ToString();

    /// <inheritdoc />
    public int MaxCount { get; }
}

/// <summary>
/// 提供 <see cref="RedisSemaphoreProvider"/> 的信号量获取方法的分部实现。
/// </summary>
public sealed partial class RedisSemaphoreProvider
{
    /// <inheritdoc />
    public RedisSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return Helpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public RedisSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.Acquire(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return TryAcquireAsync(new TimeoutValue(timeout), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Helpers.AcquireAsync(this, timeout, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RedisSynchronizationHandle?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
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

    private async ValueTask<RedisSynchronizationHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var primitive = new RedisSemaphorePrimitive(Key, MaxCount, _options.RedisLockTimeouts);
        var tryAcquireTasks = await new RedisLockAcquire(primitive, _databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
        return tryAcquireTasks != null
            ? new RedisSynchronizationHandle(new RedisLockHandle(primitive, tryAcquireTasks, extensionCadence: _options.ExtensionCadence, expiry: _options.RedisLockTimeouts.Expiry))
            : null;
    }
}

/// <summary>
/// 显式实现非泛型 <see cref="ISemaphoreProvider"/> 接口成员。
/// </summary>
public sealed partial class RedisSemaphoreProvider
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