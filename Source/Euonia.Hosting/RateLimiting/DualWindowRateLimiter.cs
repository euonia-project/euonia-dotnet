using System.Threading.RateLimiting;

namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// 组合分钟与小时两个固定窗口限流器，任一窗口超限即拒绝；释放时同时释放两个租约。
/// </summary>
public sealed class DualWindowRateLimiter : RateLimiter
{
	private readonly FixedWindowRateLimiter _minute;
	private readonly FixedWindowRateLimiter _hourly;

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
