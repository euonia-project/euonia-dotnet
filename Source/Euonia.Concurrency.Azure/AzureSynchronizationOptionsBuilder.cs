using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// 指定 Azure Blob 租约的选项。
/// </summary>
public sealed class AzureSynchronizationOptionsBuilder
{
	/// <summary>
	/// 租约时长的边界与默认值。
	/// 依据 https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob：
	/// "锁时长可以是 15 到 60 秒，也可以是无限时长"。
	/// </summary>
	private static readonly TimeoutValue
		_minLeaseDuration = TimeSpan.FromSeconds(15),
		_maxNonInfiniteLeaseDuration = TimeSpan.FromSeconds(60),
		_defaultLeaseDuration = TimeSpan.FromSeconds(30);

	/// <summary>
	/// 可配置的租约时长、续约节奏及忙等待休眠时间范围。
	/// </summary>
	private TimeoutValue? _duration, _renewalCadence, _minBusyWaitSleepTime, _maxBusyWaitSleepTime;

	/// <summary>
	/// 私有构造函数，阻止外部直接实例化，仅通过 <see cref="GetOptions"/> 创建。
	/// </summary>
	private AzureSynchronizationOptionsBuilder()
	{
	}

	/// <summary>
	/// 指定租约在没有自动续约的情况下将持续多长时间。
	/// </summary>
	/// <remarks>
	/// <para>如果启用了自动续约（默认启用），则较短的时长意味着更频繁的自动续约请求，
	/// 而无限时长意味着不进行自动续约。此外，如果持有租约的进程在未显式释放的情况下退出，
	/// 时长将决定其他进程需要等待多久才能获取该租约。</para>
	/// <para>如果禁用了自动续约，则时长决定租约被持有的时间。</para>
	/// <para>默认值为 30 秒。</para>
	/// </remarks>
	/// <param name="duration">租约的持续时间。</param>
	/// <returns>返回当前的 <see cref="AzureSynchronizationOptionsBuilder"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentOutOfRangeException">当时长不在有效范围内时抛出。</exception>
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
	/// 确定持有租约时租约被续约的频率。
	/// </summary>
	/// <remarks>
	/// <para>更频繁的续约意味着更多不必要的请求，但也降低了因进程挂起或未能在租约时长到期前完成续约请求而丢失租约的概率。</para>
	/// <para>要禁用自动续约，请指定 <see cref="Timeout.InfiniteTimeSpan"/>。</para>
	/// <para>默认值为指定租约时长的 1/3（可以为无限时长）。</para>
	/// </remarks>
	/// <param name="renewalCadence">租约续约的节奏。</param>
	/// <returns>返回当前的 <see cref="AzureSynchronizationOptionsBuilder"/> 实例，以便进行链式调用。</returns>
	public AzureSynchronizationOptionsBuilder RenewalCadence(TimeSpan renewalCadence)
	{
		_renewalCadence = new TimeoutValue(renewalCadence);
		return this;
	}

	/// <summary>
	/// 指定获取租约时忙等待的休眠时间范围。
	/// </summary>
	/// <remarks>
	/// <para>等待获取租约需要忙等待，即在获取尝试与休眠之间交替进行。休眠时间越短，
	/// 竞争条件下获取请求的流量越高，但响应性也越高（等待者注意到有竞争的租约变为可用所需的时间越短）。</para>
	/// <para>指定一个范围值允许实现在每次休眠时从该范围内随机选择一个实际值。
	/// 这有助于避免两个客户端变得"同步"，从而导致一个客户端独占租约的情况。</para>
	/// <para>默认范围为 [250ms, 1s]。</para>
	/// </remarks>
	/// <param name="min">休眠时间的最小值。</param>
	/// <param name="max">休眠时间的最大值。</param>
	/// <returns>返回当前的 <see cref="AzureSynchronizationOptionsBuilder"/> 实例，以便进行链式调用。</returns>
	/// <exception cref="ArgumentOutOfRangeException">当最小值不是有限时长，或最大值不是有限时长且大于最小值时抛出。</exception>
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

	/// <summary>
	/// 根据配置委托构建 <see cref="AzureSynchronizationOptions"/>。
	/// 应用默认值（租约时长 30 秒、续约节奏为时长的 1/3、忙等待范围 [250ms, 1s]）并校验续约节奏小于租约时长。
	/// </summary>
	/// <param name="optionsBuilder">用于配置选项的可选委托。</param>
	/// <returns>构建完成的 <see cref="AzureSynchronizationOptions"/> 实例。</returns>
	/// <exception cref="ArgumentOutOfRangeException">当续约节奏大于或等于租约时长时抛出。</exception>
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