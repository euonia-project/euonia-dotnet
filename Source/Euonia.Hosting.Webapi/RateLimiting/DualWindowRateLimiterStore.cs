using Microsoft.Extensions.Caching.Memory;

namespace Nerosoft.Euonia.Hosting.RateLimiting;

/// <summary>
/// 以滑动过期缓存承载分区双窗口限流器，驱逐时释放限流器，避免自动补充定时器永久泄漏。
/// </summary>
internal sealed class DualWindowRateLimiterStore : IDisposable
{
	private readonly MemoryCache _limiters = new(new MemoryCacheOptions());
	private readonly DualWindowRateLimiterOptions _options;
	private readonly TimeSpan _slidingExpiration;
	private readonly object _sync = new();

	/// <summary>
	/// 初始化 <see cref="DualWindowRateLimiterStore"/>。
	/// </summary>
	/// <param name="options">分区限流器配置。</param>
	/// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <c>null</c>。</exception>
	public DualWindowRateLimiterStore(DualWindowRateLimiterOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;

		// 过期时间不小于最长窗口，避免活跃分区被提前驱逐导致计数重置。
		_slidingExpiration = TimeSpan.FromTicks(Math.Max(options.MinuteWindow.Ticks, options.HourWindow.Ticks));
	}

	/// <summary>
	/// 获取内部限流器是否由定时器自动补充。
	/// </summary>
	internal bool AutoReplenishment => _options.AutoReplenishment;

	/// <summary>
	/// 获取外部补充周期（取两个窗口中较短者）。
	/// </summary>
	internal TimeSpan ReplenishmentPeriod => TimeSpan.FromTicks(Math.Min(_options.MinuteWindow.Ticks, _options.HourWindow.Ticks));

	/// <summary>
	/// 获取或创建指定分区的限流器；并发下保证同一分区只有一个实例。
	/// </summary>
	/// <param name="key">分区键。</param>
	/// <returns>分区限流器。</returns>
	public DualWindowRateLimiter GetOrCreate(string key)
	{
		if (!_limiters.TryGetValue(key, out Lazy<DualWindowRateLimiter>? lazy))
		{
			lock (_sync)
			{
				// MemoryCache.GetOrCreate 并发下工厂可能多次执行；缓存 Lazy 并加锁，
				// 保证落选工厂不会创建限流器，从而不泄漏定时器。
				lazy = _limiters.GetOrCreate(key, entry =>
				{
					entry.SlidingExpiration = _slidingExpiration;
					entry.RegisterPostEvictionCallback(static (_, value, _, _) =>
					{
						if (value is Lazy<DualWindowRateLimiter> { IsValueCreated: true } created)
						{
							created.Value.Dispose();
						}
					});
					return new Lazy<DualWindowRateLimiter>(() => new DualWindowRateLimiter(_options));
				});
			}
		}

		return lazy!.Value;
	}

	/// <summary>
	/// 尝试获取已创建的分区限流器；不存在时不创建。
	/// </summary>
	/// <param name="key">分区键。</param>
	/// <returns>已创建的限流器；不存在时为 <c>null</c>。</returns>
	public DualWindowRateLimiter? TryGet(string key)
	{
		return _limiters.TryGetValue(key, out Lazy<DualWindowRateLimiter>? lazy) && lazy.IsValueCreated
			? lazy.Value
			: null;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// MemoryCache.Compact/Dispose 不保证触发 RegisterPostEvictionCallback，
		// 因此手动尝试获取已知分区的限流器并释放。
		// 由于无法枚举 MemoryCache 的所有键，依赖驱逐回调在正常过期时释放；
		// 容器关闭时通过 Compact 尽力释放（部分运行时版本会触发回调）。
		_limiters.Compact(1.0);
		_limiters.Dispose();
	}
}
