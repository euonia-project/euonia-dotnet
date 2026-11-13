using Nerosoft.Euonia.Concurrency.Redis.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// 用于配置基于 Redis 的分布式同步算法的选项
/// </summary>
public sealed class RedisSynchronizationOptionsBuilder
{
	/// <summary>
	/// 未显式配置时使用的默认锁过期时间，为 30 秒。
	/// </summary>
	internal static readonly TimeoutValue DefaultExpiry = TimeSpan.FromSeconds(30);

	/// <summary>
	/// 我们不希望允许过期时间设得过低，因为那样锁甚至无法工作（并且默认的
	/// 最小观测过期时间最终会大于默认过期时间）
	/// </summary>
	internal static readonly TimeoutValue MinimumExpiry = TimeSpan.FromSeconds(.1);

	private TimeoutValue? _expiry,
	                      _extensionCadence,
	                      _minValidityTime,
	                      _minBusyWaitSleepTime,
	                      _maxBusyWaitSleepTime;

	/// <summary>
	/// 初始化选项构建器；实例仅由 <see cref="GetOptions"/> 创建，调用方通过公开的链式方法设置各项选项。
	/// </summary>
	internal RedisSynchronizationOptionsBuilder()
	{
	}

	/// <summary>
	/// 指定在没有自动续期的情况下锁的持续时间。由于自动续期的存在，
	/// 此值通常对程序行为影响不大。不过，将过期时间设得更长意味着
	/// 自动续期请求可以更少地发生，从而节省资源。另一方面，当锁被放弃
	/// 而没有显式释放时（例如持有进程崩溃），过期时间决定其他进程
	/// 需要等待多久才能获取它。
	/// 
	/// 默认值为 30 秒。
	/// </summary>
	public RedisSynchronizationOptionsBuilder Expiry(TimeSpan expiry)
	{
		var expiryTimeoutValue = new TimeoutValue(expiry);
		if (expiryTimeoutValue.IsInfinite || expiryTimeoutValue.CompareTo(MinimumExpiry) < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(expiry), expiry, string.Format(Resources.IDS_VALUE_MUST_BE_GREATER_OR_EQUALS_AND_LESS_THAN, MinimumExpiry.TimeSpan, "∞"));
		}

		_expiry = expiryTimeoutValue;
		return this;
	}

	/// <summary>
	/// 确定锁在被持有时续期的频率。更频繁的续期意味着更多不必要的请求，
	/// 但也降低了因进程挂起或未能在锁过期之前发出续期请求
	/// 而丢失锁的可能性。
	/// 
	/// 默认为所指定 <see cref="MinValidityTime(TimeSpan)"/> 的 1/3。
	/// </summary>
	public RedisSynchronizationOptionsBuilder ExtensionCadence(TimeSpan extensionCadence)
	{
		_extensionCadence = new TimeoutValue(extensionCadence);
		return this;
	}

	/// <summary>
	/// 锁过期时间决定锁在未续期的情况下能被持有多久。不过，由于获取锁需要
	/// 花费一些时间，我们在获取完成时并不会拥有全部过期时间。
	/// 
	/// 此值设定一个最小量，一旦获取完成，我们将保证至少还剩这么多时间。
	/// 
	/// 默认为所指定锁过期时间的 90%。
	/// </summary>
	public RedisSynchronizationOptionsBuilder MinValidityTime(TimeSpan minValidityTime)
	{
		var minValidityTimeoutValue = new TimeoutValue(minValidityTime);
		if (minValidityTimeoutValue.IsZero)
		{
			throw new ArgumentOutOfRangeException(nameof(minValidityTime), minValidityTime, ThreadingResources.IDS_CAN_NOT_BE_ZERO);
		}

		_minValidityTime = minValidityTimeoutValue;
		return this;
	}

	/// <summary>
	/// 等待获取锁需要一种在获取尝试与休眠之间交替的忙等待。
	/// 这决定了每次尝试之间休眠多长时间。较低的值会在争用情况下提高
	/// 获取请求的数量，但也会提高响应性（即等待者需要多久
	/// 才能注意到被争用的锁已可用）。
	/// 
	/// 指定一个取值范围可让实现为每次休眠在该范围内
	/// 随机选择一个实际值。这有助于避免两个客户端以某种方式
	/// "同步化"，从而导致其中一个客户端独占锁的情况。
	/// 
	/// 默认值为 [10ms, 800ms]
	/// </summary>
	public RedisSynchronizationOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
	{
		var minTimeoutValue = new TimeoutValue(min);
		var maxTimeoutValue = new TimeoutValue(max);

		if (minTimeoutValue.IsInfinite)
		{
			throw new ArgumentOutOfRangeException(nameof(min), ThreadingResources.IDS_CAN_NOT_BE_INFINITE);
		}

		if (maxTimeoutValue.IsInfinite || maxTimeoutValue.CompareTo(min) < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(max), max, string.Format(ThreadingResources.IDS_MUST_BE_NON_INFINITE_AND_GREATER_THAN_MIN, nameof(min)));
		}

		_minBusyWaitSleepTime = minTimeoutValue;
		_maxBusyWaitSleepTime = maxTimeoutValue;
		return this;
	}

	/// <summary>
	/// 应用调用方提供的配置委托，补齐默认值并校验各时间之间的关系，最终生成不可变的选项。
	/// </summary>
	/// <param name="optionsBuilder">用于设置选项的委托；为 <see langword="null"/> 时全部采用默认值。</param>
	/// <returns>包含锁超时、续期节奏与忙等休眠范围的选项。</returns>
	/// <exception cref="ArgumentOutOfRangeException">最小有效期不小于过期时间，或续期节奏不小于最小有效期。</exception>
	internal static RedisSynchronizationOptions GetOptions(Action<RedisSynchronizationOptionsBuilder>? optionsBuilder)
	{
		RedisSynchronizationOptionsBuilder? options;
		if (optionsBuilder != null)
		{
			options = new RedisSynchronizationOptionsBuilder();
			optionsBuilder(options);
		}
		else
		{
			options = null;
		}

		var expiry = options?._expiry ?? DefaultExpiry;

		TimeoutValue minValidityTime;
		if (options?._minValidityTime is { } specifiedMinValidityTime)
		{
			if (specifiedMinValidityTime.CompareTo(expiry) >= 0)
			{
				var message = string.Format(Resources.IDS_TIME_MUST_BE_LESS_THAN_EXPIRY, nameof(minValidityTime), expiry, expiry.TimeSpan);
				throw new ArgumentOutOfRangeException(nameof(minValidityTime), specifiedMinValidityTime.TimeSpan, message);
			}

			minValidityTime = specifiedMinValidityTime;
		}
		else
		{
			minValidityTime = TimeSpan.FromMilliseconds(Math.Max(0.9 * expiry.InMilliseconds, 1));
		}

		TimeoutValue extensionCadence;
		if (options?._extensionCadence is { } specifiedExtensionCadence)
		{
			// 注意：我们不允许在此处禁用自动续期，因为这会导致陷阱：
			// 人们可能放弃句柄，然后因 GC 导致其被关闭。
			// 参见此处的讨论：https://github.com/madelson/DistributedLock/issues/130。
			if (specifiedExtensionCadence.CompareTo(minValidityTime) >= 0)
			{
				var message = string.Format(Resources.IDS_TIME_MUST_BE_LESS_THAN_EXPIRY, nameof(extensionCadence), expiry, expiry.TimeSpan);
				throw new ArgumentOutOfRangeException(nameof(extensionCadence), specifiedExtensionCadence.TimeSpan, message);
			}

			extensionCadence = specifiedExtensionCadence;
		}
		else
		{
			extensionCadence = TimeSpan.FromMilliseconds(minValidityTime.InMilliseconds / 3.0);
		}

		return new RedisSynchronizationOptions(
			new RedisLockTimeouts(expiry: expiry, minValidityTime),
			extensionCadence,
			options?._minBusyWaitSleepTime ?? TimeSpan.FromMilliseconds(10),
			options?._maxBusyWaitSleepTime ?? TimeSpan.FromSeconds(0.8)
		);
	}
}