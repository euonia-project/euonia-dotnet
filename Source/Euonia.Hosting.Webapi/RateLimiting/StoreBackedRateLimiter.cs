using System.Diagnostics;
using System.Threading.RateLimiting;

namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// 分区包装限流器：每次获取租约时从共享存储解析当前分区限流器，
/// 使框架驱逐并重建分区后仍复用同一计数；真实限流器的释放由存储负责。
/// </summary>
/// <param name="store">分区限流器存储。</param>
/// <param name="key">分区键。</param>
internal sealed class StoreBackedRateLimiter(DualWindowRateLimiterStore store, string key) : ReplenishingRateLimiter
{
	private long _lastAccessTimestamp = Stopwatch.GetTimestamp();

	/// <inheritdoc />
	public override bool IsAutoReplenishing => store.AutoReplenishment;

	/// <inheritdoc />
	public override TimeSpan ReplenishmentPeriod => store.ReplenishmentPeriod;

	/// <inheritdoc />
	public override TimeSpan? IdleDuration => Stopwatch.GetElapsedTime(Volatile.Read(ref _lastAccessTimestamp));

	/// <inheritdoc />
	public override RateLimiterStatistics? GetStatistics()
	{
		var limiter = store.TryGet(key);

		if (limiter is null)
		{
			return null;
		}

		try
		{
			return limiter.GetStatistics();
		}
		catch (ObjectDisposedException)
		{
			// 限流器恰好被存储驱逐释放。
			return null;
		}
	}

	/// <inheritdoc />
	protected override RateLimitLease AttemptAcquireCore(int permitCount)
	{
		Volatile.Write(ref _lastAccessTimestamp, Stopwatch.GetTimestamp());
		return store.GetOrCreate(key).AttemptAcquire(permitCount);
	}

	/// <inheritdoc />
	protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
	{
		Volatile.Write(ref _lastAccessTimestamp, Stopwatch.GetTimestamp());
		return await store.GetOrCreate(key).AcquireAsync(permitCount, cancellationToken);
	}

	/// <inheritdoc />
	public override bool TryReplenish()
	{
		// 分区尚未创建时无需补充；恰好被驱逐释放时按未补充处理。
		try
		{
			return store.TryGet(key)?.TryReplenish() ?? false;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		// 内部限流器由存储统一持有与释放，这里不释放，避免框架驱逐分区时误杀共享计数。
		base.Dispose(disposing);
	}
}
