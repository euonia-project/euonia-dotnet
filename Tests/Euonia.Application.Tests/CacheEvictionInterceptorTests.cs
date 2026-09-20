using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

public class CacheEvictionInterceptorTests
{
	[Fact]
	public async Task Evict_AfterAsyncTask_ShouldRemoveGroupKeysOnCompletion()
	{
		var probe = new EvictionProbe();
		var cache = new FakeCacheService();
		var (manager, proxy) = CreateProxy(probe, cache);

		await proxy.GetGroupedAsync(1);
		await WaitUntilRegisteredAsync(manager, "cart", "GetGroupedAsync");
		Assert.Equal(1, probe.AsyncCalls);

		await proxy.SaveAsync(); // [CacheEvict("cart")] Task → 完成后失效
		await WaitUntilRemovedAsync(manager, "cart");

		Assert.Empty(manager.GetKeys("cart"));

		await proxy.GetGroupedAsync(1);
		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public void Evict_AfterSyncWrite_ShouldRemoveGroupKeysAndReexecute()
	{
		var probe = new EvictionProbe();
		var cache = new FakeCacheService();
		var (manager, proxy) = CreateProxy(probe, cache);

		_ = proxy.GetGrouped(1); // 写回并登记到组
		Assert.Equal(1, probe.SyncCalls);
		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGrouped"));

		proxy.Save(); // [CacheEvict("cart")] void → 同步失效

		Assert.Empty(manager.GetKeys("cart"));

		_ = proxy.GetGrouped(1); // 组已失效 → 重新执行
		Assert.Equal(2, probe.SyncCalls);
	}

	[Fact]
	public async Task Evict_WithoutAttribute_ShouldNotTouchCache()
	{
		var probe = new EvictionProbe();
		var cache = new FakeCacheService();
		var (manager, proxy) = CreateProxy(probe, cache);

		await proxy.GetGroupedAsync(1);
		await WaitUntilRegisteredAsync(manager, "cart", "GetGroupedAsync");
		await proxy.PlainAsync();

		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGroupedAsync"));
	}

	[Fact]
	public void GroupManager_RegisterAndEvict_ShouldRemoveKeysFromCache()
	{
		var cache = new FakeCacheService();
		var manager = new CacheGroupManager(new StubProvider(cache));

		manager.Register("cart:a", ["cart"]);
		manager.Register("cart:b", ["cart"]);
		Assert.Equal(2, manager.GetKeys("cart").Count);

		var removed = manager.Evict(["cart"]);

		Assert.Equal(2, removed);
		Assert.Empty(manager.GetKeys("cart"));
		Assert.Empty(cache.GetKeys());
	}

	[Fact]
	public void GroupManager_EvictWithoutCacheService_ShouldClearIndexOnly()
	{
		var manager = new CacheGroupManager(new StubProvider(null));

		manager.Register("cart:a", ["cart"]);

		var removed = manager.Evict(["cart"]);

		Assert.Equal(1, removed);
		Assert.Empty(manager.GetKeys("cart"));
	}

	private static (CacheGroupManager Manager, IEvictionProbe Proxy) CreateProxy(EvictionProbe probe, FakeCacheService cache)
	{
		var manager = new CacheGroupManager(new StubProvider(cache));
		var stub = new StubProvider(cache, manager);
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(
			                  typeof(IEvictionProbe),
			                  probe,
			                  new Castle.DynamicProxy.IInterceptor[]
			                  {
				                  new CacheInterceptor(stub),
				                  new CacheEvictionInterceptor(stub)
			                  }) as IEvictionProbe;
		return (manager, proxy!);
	}

	private static async Task WaitUntilRegisteredAsync(CacheGroupManager manager, string group, string keyFragment)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (manager.GetKeys(group).Any(key => key.Contains(keyFragment, StringComparison.Ordinal)))
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Fail("Cache key was not registered into the group within the timeout.");
	}

	private static async Task WaitUntilRemovedAsync(CacheGroupManager manager, string group)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (manager.GetKeys(group).Count == 0)
			{
				return;
			}

			await Task.Delay(10);
		}

		Assert.Fail("Cache group was not evicted within the timeout.");
	}

	public interface IEvictionProbe
	{
		string GetGrouped(int id);

		Task<string> GetGroupedAsync(int id);

		void Save();

		Task SaveAsync();

		Task PlainAsync();
	}

	public class EvictionProbe : IEvictionProbe
	{
		public int SyncCalls;

		public int AsyncCalls;

		[Cache(Groups = ["cart"])]
		public virtual string GetGrouped(int id)
		{
			SyncCalls++;
			return "grouped-" + id;
		}

		[Cache(Groups = ["cart"])]
		public virtual async Task<string> GetGroupedAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return "grouped-" + id;
		}

		[CacheEvict("cart")]
		public virtual void Save()
		{
		}

		[CacheEvict("cart")]
		public virtual Task SaveAsync()
		{
			return Task.CompletedTask;
		}

		public virtual Task PlainAsync() => Task.CompletedTask;
	}

	private sealed class StubProvider : IServiceProvider
	{
		private readonly ICacheService _cache;

		private readonly ICacheGroupManager _manager;

		public StubProvider(ICacheService cache, ICacheGroupManager manager = null)
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
}