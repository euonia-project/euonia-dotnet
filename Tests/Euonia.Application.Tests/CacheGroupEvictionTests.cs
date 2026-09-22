using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 端到端验证缓存组失效在**真实**的内存缓存上确实生效。
/// </summary>
/// <remarks>
/// <see cref="CacheGroupManager.Evict"/> 通过 <c>ICacheService.Remove&lt;object&gt;(key)</c> 失效条目，
/// 而写入时用的是方法返回类型（<c>CacheInterceptor.WriteToCache&lt;TValue&gt;</c>）。
/// 这要求缓存的键空间不按值类型分区。
/// <para>
/// 此前 <c>MemoryCacheHandle</c> 为每个 <c>TCacheValue</c> 各建一个 <c>MemoryCache</c>，
/// 于是 <c>Remove&lt;object&gt;</c> 落到另一个存储上、静默返回 false，<c>[CacheEvict]</c> 完全失效。
/// 既有测试用单一扁平字典的 <c>FakeCacheService</c>，因此一直未被发现。
/// </para>
/// </remarks>
public class CacheGroupEvictionTests
{
	[Fact]
	public void Evict_WithRealMemoryCache_RemovesCachedEntry()
	{
		using var provider = BuildProvider();
		var cache = provider.GetRequiredService<ICacheService>();
		var manager = provider.GetRequiredService<ICacheGroupManager>();

		var key = "group-evict-1";
		cache.AddOrUpdate(key, new SampleDto { Name = "cached" });
		manager.Register(key, ["group-a"]);

		var removed = manager.Evict(["group-a"]);

		Assert.Equal(1, removed);
		Assert.False(cache.TryGet<SampleDto>(key, out _));
	}

	[Fact]
	public void Evict_LeavesEntriesOfOtherGroupsIntact()
	{
		using var provider = BuildProvider();
		var cache = provider.GetRequiredService<ICacheService>();
		var manager = provider.GetRequiredService<ICacheGroupManager>();

		cache.AddOrUpdate("group-evict-2a", new SampleDto { Name = "a" });
		cache.AddOrUpdate("group-evict-2b", new SampleDto { Name = "b" });
		manager.Register("group-evict-2a", ["group-x"]);
		manager.Register("group-evict-2b", ["group-y"]);

		manager.Evict(["group-x"]);

		Assert.False(cache.TryGet<SampleDto>("group-evict-2a", out _));
		Assert.True(cache.TryGet<SampleDto>("group-evict-2b", out _));
	}

	private static ServiceProvider BuildProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MemoryCacheOptions>(options => options.InstanceName = "cache-group-tests");
		services.AddSingleton<ICacheService, MemoryCacheService>();
		services.AddSingleton<ICacheGroupManager, CacheGroupManager>();
		return services.BuildServiceProvider();
	}

	private sealed class SampleDto
	{
		public string Name { get; set; }
	}
}
