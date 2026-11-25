using Nerosoft.Euonia.Hosting.RateLimiting;
using System.Threading.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 针对双窗口限流器与流控注册的测试。
/// </summary>
public class RateLimitingTests
{
	/// <summary>
	/// 默认配置应为 5 次/分钟、10 次/小时。
	/// </summary>
	[Fact]
	public void Options_Defaults_AreExpected()
	{
		var options = new DualWindowRateLimiterOptions();

		Assert.Equal(5, options.MinutePermitLimit);
		Assert.Equal(TimeSpan.FromMinutes(1), options.MinuteWindow);
		Assert.Equal(10, options.HourPermitLimit);
		Assert.Equal(TimeSpan.FromHours(1), options.HourWindow);
		Assert.Equal(0, options.QueueLimit);
		Assert.True(options.AutoReplenishment);
	}

	/// <summary>
	/// 分钟窗口超限后应拒绝。
	/// </summary>
	[Fact]
	public void AttemptAcquire_MinuteLimitExceeded_Rejects()
	{
		using var limiter = new DualWindowRateLimiter(new DualWindowRateLimiterOptions
		{
			MinutePermitLimit = 2,
			HourPermitLimit = 10
		});

		using var first = limiter.AttemptAcquire();
		using var second = limiter.AttemptAcquire();
		using var third = limiter.AttemptAcquire();

		Assert.True(first.IsAcquired);
		Assert.True(second.IsAcquired);
		Assert.False(third.IsAcquired);
	}

	/// <summary>
	/// 小时窗口超限后应拒绝。
	/// </summary>
	[Fact]
	public void AttemptAcquire_HourLimitExceeded_Rejects()
	{
		using var limiter = new DualWindowRateLimiter(new DualWindowRateLimiterOptions
		{
			MinutePermitLimit = 10,
			HourPermitLimit = 2
		});

		using var first = limiter.AttemptAcquire();
		using var second = limiter.AttemptAcquire();
		using var third = limiter.AttemptAcquire();

		Assert.True(first.IsAcquired);
		Assert.True(second.IsAcquired);
		Assert.False(third.IsAcquired);
	}

	/// <summary>
	/// 统计信息应合并两个窗口。
	/// </summary>
	[Fact]
	public void GetStatistics_MergesWindows()
	{
		using var limiter = new DualWindowRateLimiter();
		using var lease = limiter.AttemptAcquire();

		var stats = limiter.GetStatistics();

		Assert.NotNull(stats);
		Assert.True(stats.TotalSuccessfulLeases >= 1);
		Assert.Equal(0, stats.CurrentQueuedCount);
	}

	/// <summary>
	/// 异步获取租约应成功。
	/// </summary>
	[Fact]
	public async Task AcquireAsync_ReturnsLease()
	{
		using var limiter = new DualWindowRateLimiter();

		using var lease = await limiter.AcquireAsync(1, TestContext.Current.CancellationToken);

		Assert.True(lease.IsAcquired);
	}

	/// <summary>
	/// 手动补充模式耗尽后窗口到期不会惰性重置；TryReplenish 应恢复放行。
	/// </summary>
	[Fact]
	public async Task TryReplenish_ManualMode_RestoresPermitsAfterWindow()
	{
		using var limiter = new DualWindowRateLimiter(new DualWindowRateLimiterOptions
		{
			MinutePermitLimit = 1,
			MinuteWindow = TimeSpan.FromMilliseconds(100),
			HourPermitLimit = 10,
			HourWindow = TimeSpan.FromMilliseconds(100),
			AutoReplenishment = false
		});

		using (var first = limiter.AttemptAcquire())
		{
			Assert.True(first.IsAcquired);
		}

		using (var denied = limiter.AttemptAcquire())
		{
			Assert.False(denied.IsAcquired);
		}

		await Task.Delay(300, TestContext.Current.CancellationToken);

		// 关闭自动补充后窗口到期不会惰性重置，仍应拒绝。
		using (var stillDenied = limiter.AttemptAcquire())
		{
			Assert.False(stillDenied.IsAcquired);
		}

		Assert.True(limiter.TryReplenish());

		using var restored = limiter.AttemptAcquire();
		Assert.True(restored.IsAcquired);
	}

	/// <summary>
	/// 自动补充模式下 TryReplenish 应返回 false。
	/// </summary>
	[Fact]
	public void TryReplenish_AutoMode_ReturnsFalse()
	{
		using var limiter = new DualWindowRateLimiter();

		Assert.False(limiter.TryReplenish());
	}
}
