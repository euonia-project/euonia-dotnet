using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

public class CacheInterceptorTests
{
	[Fact]
	public async Task Cache_FirstCallExecutes_SecondCallServedFromCache()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		var first = await proxy.GetPriceAsync(1);
		await WaitUntilCachedAsync(cache, key => key.StartsWith(typeof(CachedProbe).FullName!));
		var second = await proxy.GetPriceAsync(1);

		Assert.Equal(10m, first);
		Assert.Equal(10m, second);
		Assert.Equal(1, probe.AsyncCalls);
	}

	private static async Task WaitUntilCachedAsync(FakeCacheService cache, Func<string, bool> predicate)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (cache.GetKeys().Any(predicate))
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Fail("Cache entry was not written within the timeout.");
	}

	[Fact]
	public async Task Cache_DifferentArguments_ShouldYieldDistinctKeys()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out _);

		await proxy.GetPriceAsync(1);
		await proxy.GetPriceAsync(2);

		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public void SyncMethod_ShouldBeCached()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out _);

		_ = proxy.GetName(1);
		_ = proxy.GetName(1);

		Assert.Equal(1, probe.SyncCalls);
	}

	[Fact]
	public void Cache_WithCustomKey_ShouldUseTemplate()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		_ = proxy.GetCustomValue(7, "k");
		_ = proxy.GetCustomValue(7, "k");

		Assert.True(cache.TryGet("price-7-k", out decimal _));
	}

	[Fact]
	public void Cache_KeyTemplate_ShouldReplaceServiceAndMethodPlaceholders()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		_ = proxy.GetNameTemplate();

		Assert.True(cache.TryGet($"{typeof(CachedProbe).FullName}.GetNameTemplate", out string _));
	}

	[Fact]
	public async Task Cache_WithoutCacheServiceRegistered_ShouldExecuteMethod()
	{
		var probe = new CachedProbe();
		var interceptor = new CacheInterceptor(new EmptyServiceProvider());
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(ICachedProbe), probe, interceptor) as ICachedProbe;

		await proxy!.GetPriceAsync(1);
		await proxy.GetPriceAsync(1);

		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public void Cache_VoidMethod_ShouldNotBeCachedNorThrow()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		proxy.DoWork(1);
		proxy.DoWork(1);

		Assert.Equal(2, probe.SyncCalls);
		Assert.Empty(cache.GetKeys());
	}

	[Fact]
	public async Task Cache_AbsoluteExpiration_ShouldReexecuteAfterDeadline()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		await proxy.GetAbsoluteAsync(1);
		Assert.Equal(1, probe.AsyncCalls);

		// 前进到绝对到期之后：缓存项应已过期 → 第 2 次调用重新执行方法体。
		cache.Advance(TimeSpan.FromHours(2));

		_ = await proxy.GetAbsoluteAsync(1);
		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public void Cache_AbsoluteExpiration_WithIsUtcFalse_ShouldReexecuteAfterDeadline()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		_ = proxy.GetAbsoluteLocal(1);
		Assert.Equal(1, probe.SyncCalls);

		cache.Advance(TimeSpan.FromHours(2));

		_ = proxy.GetAbsoluteLocal(1);
		Assert.Equal(2, probe.SyncCalls);
	}

	[Fact]
	public async Task Cache_WithGroups_ShouldRegisterKeysInGroup()
	{
		var probe = new CachedProbe();
		var manager = new CacheGroupManager(new ServiceProviderStub(new FakeCacheService()));
		var proxy = CreateProxy(probe, out _, manager);

		_ = proxy.GetGrouped(1);

		Assert.NotNull(manager.GetKeys("cart"));
		Assert.Contains(manager.GetKeys("cart"), key => key.StartsWith(typeof(CachedProbe).FullName + ".GetGrouped", StringComparison.Ordinal));
	}

	[Fact]
	public void Cache_NullResult_ShouldRemoveStaleGroupEntry()
	{
		var probe = new CachedProbe();
		var manager = new CacheGroupManager(new ServiceProviderStub(new FakeCacheService()));
		var proxy = CreateProxy(probe, out var cache, manager);

		_ = proxy.GetGroupedNullable(1);
		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGroupedNullable"));

		// 绝对到期后重新执行，但本次返回 null（不写回缓存）→ 组索引中的残留键应被清理。
		probe.ReturnNull = true;
		cache.Advance(TimeSpan.FromHours(2));

		_ = proxy.GetGroupedNullable(1);

		Assert.Equal(2, probe.SyncCalls);
		Assert.Empty(manager.GetKeys("cart"));
	}

	private static ICachedProbe CreateProxy(CachedProbe probe, out FakeCacheService cache, ICacheGroupManager manager = null)
	{
		cache = new FakeCacheService();
		var interceptor = new CacheInterceptor(new ServiceProviderStub(cache, manager));
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(ICachedProbe), probe, interceptor) as ICachedProbe;
	}

	public interface ICachedProbe
	{
		Task<decimal> GetPriceAsync(int id);

		string GetName(int id);

		decimal GetCustomValue(int id, string suffix);

		string GetNameTemplate();

		void DoWork(int id);

		Task<decimal> GetAbsoluteAsync(int id);

		decimal GetAbsoluteLocal(int id);

		string GetGrouped(int id);

		string GetGroupedNullable(int id);
	}

	public class CachedProbe : ICachedProbe
	{
		public int AsyncCalls;

		public int SyncCalls;

		public bool ReturnNull;

		[Cache(TimeoutSeconds = 60)]
		public virtual async Task<decimal> GetPriceAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 10m;
		}

		[Cache(TimeoutSeconds = 60)]
		public virtual string GetName(int id)
		{
			SyncCalls++;
			return "name-" + id;
		}

		[Cache(Key = "price-{0}-{1}")]
		public virtual decimal GetCustomValue(int id, string suffix) => id + 0.5m;

		[Cache(Key = "{service}.{method}")]
		public virtual string GetNameTemplate() => "template";

		[Cache(TimeoutSeconds = 60)]
		public virtual void DoWork(int id)
		{
			SyncCalls++;
		}

		[Cache(AbsoluteExpirationSeconds = 3600)]
		public virtual async Task<decimal> GetAbsoluteAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 20m;
		}

		[Cache(AbsoluteExpirationSeconds = 3600, IsUtc = false)]
		public virtual decimal GetAbsoluteLocal(int id)
		{
			SyncCalls++;
			return 30m;
		}

		[Cache(Groups = ["cart"])]
		public virtual string GetGrouped(int id)
		{
			SyncCalls++;
			return "grouped-" + id;
		}

		[Cache(Groups = ["cart"], AbsoluteExpirationSeconds = 3600)]
		public virtual string GetGroupedNullable(int id)
		{
			SyncCalls++;
			return ReturnNull ? null : "v-" + id;
		}
	}

	private sealed class ServiceProviderStub : IServiceProvider
	{
		private readonly ICacheService _cache;

		private readonly ICacheGroupManager _manager;

		public ServiceProviderStub(ICacheService cache, ICacheGroupManager manager = null)
		{
			_cache = cache;
			_manager = manager;
		}

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(ICacheService))
			{
				return _cache;
			}

			return serviceType == typeof(ICacheGroupManager) ? _manager : null;
		}
	}

	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object GetService(Type serviceType) => null;
	}
}