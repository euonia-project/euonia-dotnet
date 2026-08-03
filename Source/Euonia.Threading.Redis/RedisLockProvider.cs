using StackExchange.Redis;

namespace Nerosoft.Euonia.Threading.Redis;

/// <summary>
/// 使用 Redis 实现 <see cref="ILockProvider"/>。
/// </summary>
public sealed partial class RedisLockProvider : ILockProvider<RedisSynchronizationHandle>
{
	/// <summary>
	/// 用于实现锁的 Redis 数据库列表。
	/// </summary>
	private readonly IReadOnlyList<IDatabase> _databases;

	/// <summary>
	/// Redis 同步配置选项。
	/// </summary>
	private readonly RedisSynchronizationOptions _options;

	/// <summary>
	/// 使用指定的 <paramref name="database"/> 和 <paramref name="options"/> 构造一个名为 <paramref name="key"/> 的锁。
	/// </summary>
	/// <param name="key">用于实现锁的 Redis 键。</param>
	/// <param name="database">用于实现锁的 Redis 数据库。</param>
	/// <param name="options">用于配置同步选项的可选委托。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="database"/> 为 <c>null</c> 时抛出。</exception>
	public RedisLockProvider(RedisKey key, IDatabase database, Action<RedisSynchronizationOptionsBuilder> options = null)
		: this(key, [database ?? throw new ArgumentNullException(nameof(database))], options)
	{
	}

	/// <summary>
	/// 使用指定的 <paramref name="databases"/> 和 <paramref name="options"/> 构造一个名为 <paramref name="key"/> 的锁。
	/// </summary>
	/// <param name="key">用于实现锁的 Redis 键。</param>
	/// <param name="databases">用于实现锁的 Redis 数据库列表。</param>
	/// <param name="options">用于配置同步选项的可选委托。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>default</c> 时抛出。</exception>
	public RedisLockProvider(RedisKey key, IEnumerable<IDatabase> databases, Action<RedisSynchronizationOptionsBuilder> options = null)
	{
		if (key == default(RedisKey))
		{
			throw new ArgumentNullException(nameof(key));
		}

		_databases = ValidateDatabases(databases);

		Key = key;
		_options = RedisSynchronizationOptionsBuilder.GetOptions(options);
	}

	/// <summary>
	/// 校验数据库列表，确保其非空且不含 <c>null</c> 元素。
	/// </summary>
	/// <param name="databases">要校验的 Redis 数据库列表。</param>
	/// <returns>校验通过后的数据库只读列表。</returns>
	/// <exception cref="InvalidOperationException">当列表包含 <c>null</c> 元素时抛出。</exception>
	internal static IReadOnlyList<IDatabase> ValidateDatabases(IEnumerable<IDatabase> databases)
	{
		Check.EnsureNotNullOrEmpty(databases, nameof(databases));

		if (databases.Any(t => t is null))
		{
			throw new InvalidOperationException(Resources.IDS_ONE_OR_MORE_DATABASES_ARE_NULL);
		}

		return databases.ToList();
	}

	/// <summary>
	/// 用于实现锁的 Redis 键。
	/// </summary>
	private RedisKey Key { get; }

	/// <inheritdoc />
	public string Name => Key.ToString();
}

public sealed partial class RedisLockProvider
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
		return this.As<ILockProvider<RedisSynchronizationHandle>>().TryAcquireAsync(timeout, cancellationToken);
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

	/// <summary>
	/// 尝试异步获取锁。
	/// 创建 Redis 互斥原语，在所有数据库上尝试获取锁；获取失败时返回 <c>null</c>。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务，包含获取到的锁句柄；失败时返回 <c>null</c>。</returns>
	private async ValueTask<RedisSynchronizationHandle> TryAcquireAsync(CancellationToken cancellationToken)
	{
		var primitive = new RedisMutexPrimitive(Key, RedisLockHelper.CreateLockId(), _options.RedisLockTimeouts);
		var tryAcquireTasks = await new RedisLockAcquire(primitive, _databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
		return tryAcquireTasks != null
			? new RedisSynchronizationHandle(new RedisLockHandle(primitive, tryAcquireTasks, extensionCadence: _options.ExtensionCadence, expiry: _options.RedisLockTimeouts.Expiry))
			: null;
	}
}

public sealed partial class RedisLockProvider
{
	/// <inheritdoc />
	ISynchronizationHandle ILockProvider.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
	{
		return TryAcquire(timeout, cancellationToken);
	}

	/// <inheritdoc />
	ISynchronizationHandle ILockProvider.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
	{
		return Acquire(timeout, cancellationToken);
	}

	/// <inheritdoc />
	ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
	{
		return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
	}

	/// <inheritdoc />
	ValueTask<ISynchronizationHandle> ILockProvider.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
	{
		return AcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
	}

	/// <inheritdoc />
	ValueTask<ISynchronizationHandle> ILockProvider.TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
	{
		return TryAcquireAsync(timeout, cancellationToken).Convert(TaskConversion<ISynchronizationHandle>.ValueTask);
	}
}