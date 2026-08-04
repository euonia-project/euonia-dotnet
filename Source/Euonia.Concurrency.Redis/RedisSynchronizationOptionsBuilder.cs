using Nerosoft.Euonia.Concurrency.Redis.Internal;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Redis;

/// <summary>
/// Options for configuring a redis-based distributed synchronization algorithm
/// </summary>
public sealed class RedisSynchronizationOptionsBuilder
{
	internal static readonly TimeoutValue DefaultExpiry = TimeSpan.FromSeconds(30);

	/// <summary>
	/// We don't want to allow expiry to go too low, since then the lock doesn't even work (and the default
	/// min observed expiry will end up greater than the default expiry)
	/// </summary>
	internal static readonly TimeoutValue MinimumExpiry = TimeSpan.FromSeconds(.1);

	private TimeoutValue? _expiry,
	                      _extensionCadence,
	                      _minValidityTime,
	                      _minBusyWaitSleepTime,
	                      _maxBusyWaitSleepTime;

	internal RedisSynchronizationOptionsBuilder()
	{
	}

	/// <summary>
	/// Specifies how long the lock will last, absent auto-extension. Because auto-extension exists,
	/// this value generally will have little effect on program behavior. However, making the expiry longer means that
	/// auto-extension requests can occur less frequently, saving resources. On the other hand, when a lock is abandoned
	/// without explicit release (e. g. if the holding process crashes), the expiry determines how long other processes
	/// would need to wait in order to acquire it.
	/// 
	/// Defaults to 30s.
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
	/// Determines how frequently the lock will be extended while held. More frequent extension means more unnecessary requests
	/// but also a lower chance of losing the lock due to the process hanging or otherwise failing to get its extension request in
	/// before the lock expiry elapses.
	/// 
	/// Defaults to 1/3 of the specified <see cref="MinValidityTime(TimeSpan)"/>.
	/// </summary>
	public RedisSynchronizationOptionsBuilder ExtensionCadence(TimeSpan extensionCadence)
	{
		_extensionCadence = new TimeoutValue(extensionCadence);
		return this;
	}

	/// <summary>
	/// The lock expiry determines how long the lock will be held without being extended. However, since it takes some amount
	/// of time to acquire the lock, we will not have all of expiry available upon acquisition.
	/// 
	/// This value sets a minimum amount which we'll be guaranteed to have left once acquisition completes.
	/// 
	/// Defaults to 90% of the specified lock expiry.
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
	/// Waiting to acquire a lock requires a busy wait that alternates acquire attempts and sleeps.
	/// This determines how much time is spent sleeping between attempts. Lower values will raise the
	/// volume of acquire requests under contention but will also raise the responsiveness (how long
	/// it takes a waiter to notice that a contended the lock has become available).
	/// 
	/// Specifying a range of values allows the implementation to select an actual value in the range 
	/// at random for each sleep. This helps avoid the case where two clients become "synchronized"
	/// in such a way that results in one client monopolizing the lock.
	/// 
	/// The default is [10ms, 800ms]
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

	internal static RedisSynchronizationOptions GetOptions(Action<RedisSynchronizationOptionsBuilder> optionsBuilder)
	{
		RedisSynchronizationOptionsBuilder options;
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
			// Note: we do not allow for disabling auto-extension here because it leads to traps
			// where people might abandon the handle and then have it be closed due to GC.
			// See discussion here: https://github.com/madelson/DistributedLock/issues/130.
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