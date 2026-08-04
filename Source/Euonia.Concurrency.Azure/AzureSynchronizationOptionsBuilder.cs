using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// Specifies options for an Azure blob lease
/// </summary>
public sealed class AzureSynchronizationOptionsBuilder
{
	/// <summary>
	/// From https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob:
	/// "The lock duration can be 15 to 60 seconds, or can be infinite"
	/// </summary>
	private static readonly TimeoutValue
		_minLeaseDuration = TimeSpan.FromSeconds(15),
		_maxNonInfiniteLeaseDuration = TimeSpan.FromSeconds(60),
		_defaultLeaseDuration = TimeSpan.FromSeconds(30);

	private TimeoutValue? _duration, _renewalCadence, _minBusyWaitSleepTime, _maxBusyWaitSleepTime;

	private AzureSynchronizationOptionsBuilder()
	{
	}

	/// <summary>
	/// Specifies how long the lease will last, absent auto-renewal.
	/// 
	/// If auto-renewal is enabled (the default), then a shorter duration means more frequent auto-renewal requests,
	/// while an infinite duration means no auto-renewal requests. Furthermore, if the lease-holding process were to
	/// exit without explicitly releasing, then duration determines how long other processes would need to wait in 
	/// order to acquire the lease.
	/// 
	/// If auto-renewal is disabled, then duration determines how long the lease will be held.
	/// 
	/// Defaults to 30s.
	/// </summary>
	public AzureSynchronizationOptionsBuilder Duration(TimeSpan duration)
	{
		var durationTimeoutValue = new TimeoutValue(duration);
		if (durationTimeoutValue.CompareTo(_minLeaseDuration) < 0
		    || (!durationTimeoutValue.IsInfinite && durationTimeoutValue.CompareTo(_maxNonInfiniteLeaseDuration) > 0))
		{
			throw new ArgumentOutOfRangeException(nameof(duration), duration, string.Format(Resources.IDS_MUST_BE_INFINITE_OR_IN, $"{_minLeaseDuration}, {_maxNonInfiniteLeaseDuration}"));
		}

		_duration = durationTimeoutValue;
		return this;
	}

	/// <summary>
	/// Determines how frequently the lease will be renewed when held. More frequent renewal means more unnecessary requests
	/// but also a lower chance of losing the lease due to the process hanging or otherwise failing to get its renewal request in
	/// before the lease duration expires.
	/// 
	/// To disable auto-renewal, specify <see cref="Timeout.InfiniteTimeSpan"/>
	/// 
	/// Defaults to 1/3 of the specified lease duration (may be infinite).
	/// </summary>
	public AzureSynchronizationOptionsBuilder RenewalCadence(TimeSpan renewalCadence)
	{
		_renewalCadence = new TimeoutValue(renewalCadence);
		return this;
	}

	/// <summary>
	/// Waiting to acquire a lease requires a busy wait that alternates acquire attempts and sleeps.
	/// This determines how much time is spent sleeping between attempts. Lower values will raise the
	/// volume of acquire requests under contention but will also raise the responsiveness (how long
	/// it takes a waiter to notice that a contended the lease has become available).
	/// 
	/// Specifying a range of values allows the implementation to select an actual value in the range 
	/// at random for each sleep. This helps avoid the case where two clients become "synchronized"
	/// in such a way that results in one client monopolizing the lease.
	/// 
	/// The default is [250ms, 1s]
	/// </summary>
	public AzureSynchronizationOptionsBuilder BusyWaitSleepTime(TimeSpan min, TimeSpan max)
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

	internal static AzureSynchronizationOptions GetOptions(Action<AzureSynchronizationOptionsBuilder> optionsBuilder)
	{
		AzureSynchronizationOptionsBuilder options;
		if (optionsBuilder != null)
		{
			options = new AzureSynchronizationOptionsBuilder();
			optionsBuilder(options);

			if (options._renewalCadence is { } renewalCadence && !renewalCadence.IsInfinite)
			{
				var duration = options._duration ?? _defaultLeaseDuration;
				if (renewalCadence.CompareTo(duration) >= 0)
				{
					var message = string.Format(Resources.IDS_MUST_BE_GREATTER_THAN, nameof(renewalCadence), $"{nameof(duration)} ({duration})", $"{nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}");
					throw new ArgumentOutOfRangeException(nameof(renewalCadence), renewalCadence.TimeSpan, message);
				}
			}
		}
		else
		{
			options = null;
		}

		var durationToUse = options?._duration ?? _defaultLeaseDuration;
		return new AzureSynchronizationOptions(
			durationToUse,
			options?._renewalCadence ?? (durationToUse.IsInfinite ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(durationToUse.InMilliseconds / 3.0)),
			options?._minBusyWaitSleepTime ?? TimeSpan.FromMilliseconds(250),
			options?._maxBusyWaitSleepTime ?? TimeSpan.FromSeconds(1)
		);
	}
}