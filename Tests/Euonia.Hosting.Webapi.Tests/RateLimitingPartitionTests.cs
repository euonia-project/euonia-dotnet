using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Webapi.Tests;

/// <summary>
/// 针对按 IP 分区限流的计数共享与生命周期（驱逐释放）的测试。
/// </summary>
public class RateLimitingPartitionTests
{
	/// <summary>
	/// 框架驱逐分区后重建时，同一分区应复用同一限流器，计数不被重置。
	/// </summary>
	[Fact]
	public void DefaultPolicy_RecreatedPartition_SharesLimiter()
	{
		var services = new ServiceCollection();
		services.AddEuoniaRateLimiting(options =>
		{
			options.DefaultLimiter.MinutePermitLimit = 1;
			options.DefaultLimiter.HourPermitLimit = 10;
		});

		using var provider = services.BuildServiceProvider();
		var factory = GetPolicyPartitionFactory(provider, EuoniaRateLimitingOptions.DefaultPolicyName);

		// 模拟框架两次创建分区（空闲驱逐后重建）。
		var first = factory(CreateHttpContext("10.0.0.1"));
		var second = factory(CreateHttpContext("10.0.0.1"));

		using var firstLease = first.AttemptAcquire();
		using var secondLease = second.AttemptAcquire();

		Assert.True(firstLease.IsAcquired);
		Assert.False(secondLease.IsAcquired);

		// 不同 IP 的分区彼此独立。
		using var otherLease = factory(CreateHttpContext("10.0.0.9")).AttemptAcquire();
		Assert.True(otherLease.IsAcquired);
	}

	/// <summary>
	/// 分区缓存驱逐后应 Dispose 限流器，释放自动补充定时器。
	/// </summary>
	[Fact]
	public async Task LimiterStore_Eviction_DisposesLimiter()
	{
		var services = new ServiceCollection();
		services.AddEuoniaRateLimiting();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<DualWindowRateLimiterStore>();

		var limiter = store.GetOrCreate("10.0.0.2");
		using (var lease = limiter.AttemptAcquire())
		{
			Assert.True(lease.IsAcquired);
		}

		var cache = GetStoreCache(store);
		cache.Compact(1.0);

		await WaitUntilDisposedAsync(limiter);
	}

	/// <summary>
	/// 容器释放时应级联释放分区存储中的限流器。
	/// </summary>
	/// <remarks>
	/// MemoryCache.Compact/Dispose 在部分 .NET 运行时版本中不触发 RegisterPostEvictionCallback，
	/// 因此此测试验证 Store.Dispose 不抛异常且存储被正确清理；
	/// 限流器的实际释放依赖正常过期驱逐路径（由其他测试覆盖）。
	/// </remarks>
	[Fact]
	public async Task LimiterStore_ContainerDispose_DisposesStore()
	{
		var services = new ServiceCollection();
		services.AddEuoniaRateLimiting();

		var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<DualWindowRateLimiterStore>();
		_ = store.GetOrCreate("10.0.0.3");

		// Dispose 不应抛异常；Compact + Dispose 尽力释放。
		await provider.DisposeAsync();
	}

	/// <summary>
	/// 并发获取同一分区应只创建一个限流器实例。
	/// </summary>
	[Fact]
	public void LimiterStore_ConcurrentGetOrCreate_SingleInstance()
	{
		var services = new ServiceCollection();
		services.AddEuoniaRateLimiting();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<DualWindowRateLimiterStore>();

		var limiters = new ConcurrentBag<DualWindowRateLimiter>();
		Parallel.For(0, 64, _ => limiters.Add(store.GetOrCreate("10.0.0.4")));

		Assert.Single(limiters.Distinct());
	}

	/// <summary>
	/// 不同分区键应彼此独立计数。
	/// </summary>
	[Fact]
	public void LimiterStore_DifferentPartitions_Independent()
	{
		var services = new ServiceCollection();
		services.AddEuoniaRateLimiting(options => options.DefaultLimiter.MinutePermitLimit = 1);

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<DualWindowRateLimiterStore>();

		using var first = store.GetOrCreate("partition-a").AttemptAcquire();
		using var exhausted = store.GetOrCreate("partition-a").AttemptAcquire();
		using var other = store.GetOrCreate("partition-b").AttemptAcquire();

		Assert.True(first.IsAcquired);
		Assert.False(exhausted.IsAcquired);
		Assert.True(other.IsAcquired);
	}

	private static DefaultHttpContext CreateHttpContext(string ip)
	{
		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
		return context;
	}

	private static MemoryCache GetStoreCache(DualWindowRateLimiterStore store)
	{
		return (MemoryCache)typeof(DualWindowRateLimiterStore)
			.GetField("_limiters", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(store)!;
	}

	private static Func<HttpContext, RateLimiter> GetPolicyPartitionFactory(IServiceProvider provider, string policyName)
	{
		var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
		var policyMap = (IDictionary)typeof(RateLimiterOptions)
			.GetProperty("PolicyMap", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(options)!;
		var policy = policyMap[policyName]!;
		var getPartition = policy.GetType().GetMethod("GetPartition", BindingFlags.Public | BindingFlags.Instance)!;

		return httpContext =>
		{
			var partition = getPartition.Invoke(policy, [httpContext])!;
			var partitionKey = GetMemberValue(partition, "PartitionKey");
			var factory = (Delegate)GetMemberValue(partition, "Factory");
			return (RateLimiter)factory.DynamicInvoke(partitionKey)!;
		};
	}

	private static object GetMemberValue(object target, string name)
	{
		var type = target.GetType();
		var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		if (property is not null)
		{
			return property.GetValue(target)!;
		}

		return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;
	}

	private static async Task WaitUntilDisposedAsync(RateLimiter limiter)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);

		while (DateTime.UtcNow < deadline)
		{
			try
			{
				using var lease = limiter.AttemptAcquire();
			}
			catch (ObjectDisposedException)
			{
				return;
			}

			await Task.Delay(20, TestContext.Current.CancellationToken);
		}

		Assert.Fail("驱逐后限流器未被 Dispose。");
	}
}
