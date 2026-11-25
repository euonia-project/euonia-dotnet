using System.Threading.RateLimiting;

namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// 组合分钟与小时两个固定窗口限流器，任一窗口超限即拒绝；释放时同时释放两个租约。
/// </summary>
public sealed class DualWindowRateLimiter : ReplenishingRateLimiter
{
	private readonly FixedWindowRateLimiter _minute;
	private readonly FixedWindowRateLimiter _hourly;
	private readonly bool _autoReplenishment;
	private readonly TimeSpan _replenishmentPeriod;

	/// <summary>
	/// 使用默认配置（5 次/分钟、10 次/小时）初始化 <see cref="DualWindowRateLimiter"/>。
	/// </summary>
	public DualWindowRateLimiter()
		: this(new DualWindowRateLimiterOptions())
	{
	}

	/// <summary>
	/// 使用指定配置初始化 <see cref="DualWindowRateLimiter"/>。
	/// </summary>
	/// <param name="options">窗口配置。</param>
	/// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <c>null</c>。</exception>
	/// <exception cref="ArgumentOutOfRangeException">窗口或请求数配置非法。</exception>
	public DualWindowRateLimiter(DualWindowRateLimiterOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();

		_autoReplenishment = options.AutoReplenishment;
		_replenishmentPeriod = TimeSpan.FromTicks(Math.Min(options.MinuteWindow.Ticks, options.HourWindow.Ticks));

		_minute = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
		{
			PermitLimit = options.MinutePermitLimit,
			Window = options.MinuteWindow,
			QueueLimit = options.QueueLimit,
			AutoReplenishment = options.AutoReplenishment
		});

		_hourly = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
		{
			PermitLimit = options.HourPermitLimit,
			Window = options.HourWindow,
			QueueLimit = options.QueueLimit,
			AutoReplenishment = options.AutoReplenishment
		});
	}

	/// <inheritdoc />
	public override bool IsAutoReplenishing => _autoReplenishment;

	/// <inheritdoc />
	public override TimeSpan ReplenishmentPeriod => _replenishmentPeriod;

	/// <inheritdoc />
	public override TimeSpan? IdleDuration
	{
		get
		{
			var minute = _minute.IdleDuration;
			var hourly = _hourly.IdleDuration;

			if (minute is null)
			{
				return hourly;
			}

			return hourly is null || minute < hourly ? minute : hourly;
		}
	}

	/// <inheritdoc />
	public override RateLimiterStatistics? GetStatistics()
	{
		var minute = _minute.GetStatistics();
		var hourly = _hourly.GetStatistics();

		if (minute is null || hourly is null)
		{
			return null;
		}

		return new RateLimiterStatistics
		{
			CurrentAvailablePermits = Math.Min(minute.CurrentAvailablePermits, hourly.CurrentAvailablePermits),
			CurrentQueuedCount = minute.CurrentQueuedCount + hourly.CurrentQueuedCount,
			TotalSuccessfulLeases = minute.TotalSuccessfulLeases + hourly.TotalSuccessfulLeases,
			TotalFailedLeases = minute.TotalFailedLeases + hourly.TotalFailedLeases
		};
	}

	/// <summary>
	/// 手动补充两个窗口的请求计数。
	/// </summary>
	/// <returns>
	/// 任一内部限流器处于手动补充模式时返回 <c>true</c>；两个窗口均启用自动补充时返回 <c>false</c>。
	/// 返回值不表示计数是否实际被重置：仅当窗口自上次补充起已经过期时才会重置计数。
	/// </returns>
	/// <remarks>
	/// 当 <see cref="IsAutoReplenishing"/> 为 <c>false</c> 时，由宿主定时器或调用方定期调用，避免窗口过期后永久拒绝请求。
	/// </remarks>
	public override bool TryReplenish()
	{
		var minuteReplenished = _minute.TryReplenish();
		var hourlyReplenished = _hourly.TryReplenish();

		return minuteReplenished || hourlyReplenished;
	}

	/// <inheritdoc />
	protected override RateLimitLease AttemptAcquireCore(int permitCount)
	{
		var minuteLease = _minute.AttemptAcquire(permitCount);

		if (!minuteLease.IsAcquired)
		{
			return minuteLease;
		}

		var hourlyLease = _hourly.AttemptAcquire(permitCount);

		if (!hourlyLease.IsAcquired)
		{
			minuteLease.Dispose();
			return hourlyLease;
		}

		return new DualLease(minuteLease, hourlyLease);
	}

	/// <inheritdoc />
	protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
	{
		var minuteLease = await _minute.AcquireAsync(permitCount, cancellationToken);

		if (!minuteLease.IsAcquired)
		{
			return minuteLease;
		}

		var hourlyLease = await _hourly.AcquireAsync(permitCount, cancellationToken);

		if (!hourlyLease.IsAcquired)
		{
			minuteLease.Dispose();
			return hourlyLease;
		}

		return new DualLease(minuteLease, hourlyLease);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_minute.Dispose();
			_hourly.Dispose();
		}

		base.Dispose(disposing);
	}

	/// <summary>
	/// 组合分钟与小时租约。
	/// </summary>
	private sealed class DualLease(RateLimitLease first, RateLimitLease second) : RateLimitLease
	{
		/// <inheritdoc />
		public override bool IsAcquired => first.IsAcquired && second.IsAcquired;

		/// <inheritdoc />
		public override IEnumerable<string> MetadataNames => first.MetadataNames.Concat(second.MetadataNames).Distinct(StringComparer.Ordinal);

		/// <inheritdoc />
		public override bool TryGetMetadata(string metadataName, out object? metadata)
		{
			return first.TryGetMetadata(metadataName, out metadata) || second.TryGetMetadata(metadataName, out metadata);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				first.Dispose();
				second.Dispose();
			}
		}
	}
}
