using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证 <see cref="CacheEvictionInterceptor"/> 在方法执行前后按组失效缓存，以及 <see cref="CacheGroupManager"/> 的组索引维护行为。
/// </summary>
public class CacheEvictionInterceptorTests
{
	/// <summary>
	/// 异步方法标记 <see cref="CacheEvictAttribute"/> 时，应在任务完成后移除所在组的缓存键，随后的调用因此重新执行方法体。
	/// </summary>
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

	/// <summary>
	/// 同步方法标记 <see cref="CacheEvictAttribute"/> 时，应在返回前移除组内键，下一次读取会重新执行并再次写回缓存。
	/// </summary>
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

	/// <summary>
	/// 未标记 <see cref="CacheEvictAttribute"/> 的方法不应触发缓存失效，已登记的组内键应保持不变。
	/// </summary>
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

	/// <summary>
	/// 将多个键注册到同一组后执行失效，应返回被移除的键数量，并从组索引与缓存中一并清除这些键。
	/// </summary>
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

	/// <summary>
	/// 从组索引移除单个键时，应同时把它从所属的全部组中删除。
	/// </summary>
	[Fact]
	public void GroupManager_Remove_ShouldDropKeyFromAllGroups()
	{
		var manager = new CacheGroupManager(new StubProvider(null));

		manager.Register("cart:a", ["cart", "orders"]);

		manager.Remove("cart:a");

		Assert.Empty(manager.GetKeys("cart"));
		Assert.Empty(manager.GetKeys("orders"));
	}

	/// <summary>
	/// <see cref="CacheEvictionMode.Before"/> 模式下失效在方法执行前完成，即使方法抛出异常，组内键也应已被清除。
	/// </summary>
	[Fact]
	public void Evict_WithModeBefore_ShouldEvictEvenIfMethodThrows()
	{
		var probe = new EvictionProbe();
		var cache = new FakeCacheService();
		var (manager, proxy) = CreateProxy(probe, cache);

		_ = proxy.GetGrouped(1); // 写回并登记到组
		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGrouped"));

		// Before 模式：执行前先失效，方法抛异常也不影响已完成的失效。
		Assert.Throws<InvalidOperationException>(() => proxy.SaveBefore());

		Assert.Empty(manager.GetKeys("cart"));
	}

	/// <summary>
	/// 默认的 After 模式下，方法抛出异常时不应执行缓存失效，组内键应继续保留。
	/// </summary>
	[Fact]
	public void Evict_DefaultAfter_ShouldNotEvictWhenMethodThrows()
	{
		var probe = new EvictionProbe();
		var cache = new FakeCacheService();
		var (manager, proxy) = CreateProxy(probe, cache);

		_ = proxy.GetGrouped(1); // 写回并登记到组
		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGrouped"));

		// After 模式：失败不失效 → 组索引保持完好。
		Assert.Throws<InvalidOperationException>(() => proxy.SaveAfterThrow());

		Assert.Contains(manager.GetKeys("cart"), key => key.Contains("GetGrouped"));
	}

	/// <summary>
	/// 未提供缓存服务时，失效操作应只清理组索引，并返回已移除的键数量。
	/// </summary>
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

	/// <summary>
	/// 缓存失效拦截器测试使用的探针契约，声明带不同缓存与失效配置的方法。
	/// </summary>
	public interface IEvictionProbe
	{
		/// <summary>
		/// 读取并缓存到 <c>cart</c> 组的数据；调用次数由实现统计。
		/// </summary>
		/// <param name="id">数据标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		string GetGrouped(int id);

		/// <summary>
		/// 异步读取并缓存到 <c>cart</c> 组的数据；调用次数由实现统计。
		/// </summary>
		/// <param name="id">数据标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		Task<string> GetGroupedAsync(int id);

		/// <summary>
		/// 同步保存并失效 <c>cart</c> 组。
		/// </summary>
		void Save();

		/// <summary>
		/// 异步保存并失效 <c>cart</c> 组。
		/// </summary>
		/// <returns>表示异步保存操作的任务。</returns>
		Task SaveAsync();

		/// <summary>
		/// 不参与缓存的普通异步方法。
		/// </summary>
		/// <returns>表示已完成操作的任务。</returns>
		Task PlainAsync();

		/// <summary>
		/// 在方法执行前失效 <c>cart</c> 组，随后抛出异常。
		/// </summary>
		void SaveBefore();

		/// <summary>
		/// 在方法执行后失效 <c>cart</c> 组，但方法会抛出异常。
		/// </summary>
		void SaveAfterThrow();
	}

	/// <summary>
	/// <see cref="IEvictionProbe"/> 的测试实现，用调用计数验证缓存与失效是否按预期触发。
	/// </summary>
	public class EvictionProbe : IEvictionProbe
	{
		/// <summary>
		/// 同步方法体的执行次数。
		/// </summary>
		public int SyncCalls;

		/// <summary>
		/// 异步方法体的执行次数。
		/// </summary>
		public int AsyncCalls;

		/// <summary>
		/// 返回 <c>grouped-{id}</c> 并累计同步调用次数，结果按 <c>cart</c> 组缓存。
		/// </summary>
		/// <param name="id">数据标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		[Cache(Groups = ["cart"])]
		public virtual string GetGrouped(int id)
		{
			SyncCalls++;
			return "grouped-" + id;
		}

		/// <summary>
		/// 异步返回 <c>grouped-{id}</c> 并累计异步调用次数，结果按 <c>cart</c> 组缓存。
		/// </summary>
		/// <param name="id">数据标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		[Cache(Groups = ["cart"])]
		public virtual async Task<string> GetGroupedAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return "grouped-" + id;
		}

		/// <summary>
		/// 空实现的同步保存操作，仅用于触发 <c>cart</c> 组失效。
		/// </summary>
		[CacheEvict("cart")]
		public virtual void Save()
		{
		}

		/// <summary>
		/// 返回已完成任务的异步保存操作，仅用于触发 <c>cart</c> 组失效。
		/// </summary>
		/// <returns>表示已完成操作的任务。</returns>
		[CacheEvict("cart")]
		public virtual Task SaveAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// 在失效完成后抛出 <see cref="InvalidOperationException"/>，用于验证 Before 模式的失效时机。
		/// </summary>
		/// <exception cref="InvalidOperationException">始终抛出，用于模拟业务失败。</exception>
		[CacheEvict("cart", Mode = CacheEvictionMode.Before)]
		public virtual void SaveBefore()
		{
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 抛出 <see cref="InvalidOperationException"/>，用于验证默认 After 模式在方法失败时不执行失效。
		/// </summary>
		/// <exception cref="InvalidOperationException">始终抛出，用于模拟业务失败。</exception>
		[CacheEvict("cart")]
		public virtual void SaveAfterThrow()
		{
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 返回已完成任务的普通异步方法，用于验证未标记 <see cref="CacheEvictAttribute"/> 时不触碰缓存。
		/// </summary>
		/// <returns>表示已完成操作的任务。</returns>
		public virtual Task PlainAsync() => Task.CompletedTask;
	}

	/// <summary>
	/// 测试用服务提供程序：按请求类型返回预置的缓存服务与缓存组管理器，其余类型返回 <see langword="null"/>。
	/// </summary>
	private sealed class StubProvider : IServiceProvider
	{
		private readonly ICacheService? _cache;

		private readonly ICacheGroupManager? _manager;

		public StubProvider(ICacheService? cache, ICacheGroupManager? manager = null)
		{
			_cache = cache;
			_manager = manager;
		}

		public object? GetService(Type serviceType)
		{
			if (serviceType == typeof(ICacheService))
			{
				return _cache;
			}

			return serviceType == typeof(ICacheGroupManager) ? _manager : null;
		}
	}
}