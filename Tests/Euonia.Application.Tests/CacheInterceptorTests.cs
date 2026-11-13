using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证 <see cref="CacheInterceptor"/> 对同步与异步方法的缓存写入、键模板、过期和分组登记等行为。
/// </summary>
public class CacheInterceptorTests
{
	/// <summary>
	/// 首次调用应执行方法并写入缓存，参数相同的第二次调用应直接命中缓存，方法体只执行一次。
	/// </summary>
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

	/// <summary>
	/// 参数不同的调用应生成不同的缓存键，因此各自执行一次方法体。
	/// </summary>
	[Fact]
	public async Task Cache_DifferentArguments_ShouldYieldDistinctKeys()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out _);

		await proxy.GetPriceAsync(1);
		await proxy.GetPriceAsync(2);

		Assert.Equal(2, probe.AsyncCalls);
	}

	/// <summary>
	/// 同步方法的返回值也应被缓存，重复调用不再执行方法体。
	/// </summary>
	[Fact]
	public void SyncMethod_ShouldBeCached()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out _);

		_ = proxy.GetName(1);
		_ = proxy.GetName(1);

		Assert.Equal(1, probe.SyncCalls);
	}

	/// <summary>
	/// 配置自定义键模板时，应按模板格式化后的键把结果写入缓存。
	/// </summary>
	[Fact]
	public void Cache_WithCustomKey_ShouldUseTemplate()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		_ = proxy.GetCustomValue(7, "k");
		_ = proxy.GetCustomValue(7, "k");

		Assert.True(cache.TryGet("price-7-k", out decimal _));
	}

	/// <summary>
	/// 键模板中的 <c>{service}</c> 与 <c>{method}</c> 占位符应分别替换为服务全名与方法名。
	/// </summary>
	[Fact]
	public void Cache_KeyTemplate_ShouldReplaceServiceAndMethodPlaceholders()
	{
		var probe = new CachedProbe();
		var proxy = CreateProxy(probe, out var cache);

		_ = proxy.GetNameTemplate();

		Assert.True(cache.TryGet($"{typeof(CachedProbe).FullName}.GetNameTemplate", out string _));
	}

	/// <summary>
	/// 未注册 <see cref="ICacheService"/> 时应跳过缓存直接执行方法，且不抛出异常。
	/// </summary>
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

	/// <summary>
	/// 返回 void 的方法既不应写入缓存，也不应因缓存逻辑抛出异常，重复调用均执行方法体。
	/// </summary>
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

	/// <summary>
	/// 缓存项的绝对到期时间过后应失效，下一次调用重新执行方法体。
	/// </summary>
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

	/// <summary>
	/// <see cref="CacheAttribute.IsUtc"/> 为 <c>false</c> 时按本地时间计算绝对到期，过期后同样重新执行方法体。
	/// </summary>
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

	/// <summary>
	/// 标记缓存分组后，写入的缓存键应登记到对应的组索引中。
	/// </summary>
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

	/// <summary>
	/// 缓存过期后方法返回 <c>null</c> 时不应写回缓存，并应清除组索引中残留的键。
	/// </summary>
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

	private static ICachedProbe CreateProxy(CachedProbe probe, out FakeCacheService cache, ICacheGroupManager? manager = null)
	{
		cache = new FakeCacheService();
		var interceptor = new CacheInterceptor(new ServiceProviderStub(cache, manager));
		var generator = new ProxyGenerator();
		return (generator.CreateInterfaceProxyWithTarget(typeof(ICachedProbe), probe, interceptor) as ICachedProbe)!;
	}

	/// <summary>
	/// 缓存拦截器测试使用的探针契约，声明带不同缓存配置的方法。
	/// </summary>
	public interface ICachedProbe
	{
		/// <summary>
		/// 异步读取价格，结果按默认键缓存。
		/// </summary>
		/// <param name="id">价格标识。</param>
		/// <returns>价格值。</returns>
		Task<decimal> GetPriceAsync(int id);

		/// <summary>
		/// 同步读取名称，结果按默认键缓存。
		/// </summary>
		/// <param name="id">名称标识。</param>
		/// <returns>名称值。</returns>
		string GetName(int id);

		/// <summary>
		/// 按自定义键模板读取计算结果。
		/// </summary>
		/// <param name="id">数值标识。</param>
		/// <param name="suffix">键模板使用的后缀。</param>
		/// <returns>计算结果。</returns>
		decimal GetCustomValue(int id, string suffix);

		/// <summary>
		/// 读取使用服务与方法占位符键模板的值。
		/// </summary>
		/// <returns>固定字符串结果。</returns>
		string GetNameTemplate();

		/// <summary>
		/// 执行无返回值的操作，用于验证 void 方法不写缓存。
		/// </summary>
		/// <param name="id">操作标识。</param>
		void DoWork(int id);

		/// <summary>
		/// 按 UTC 绝对到期时间读取异步结果。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>结果值。</returns>
		Task<decimal> GetAbsoluteAsync(int id);

		/// <summary>
		/// 按本地时间绝对到期读取同步结果。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>结果值。</returns>
		decimal GetAbsoluteLocal(int id);

		/// <summary>
		/// 读取并登记到 <c>cart</c> 组的结果。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		string GetGrouped(int id);

		/// <summary>
		/// 读取可能为 <c>null</c> 并登记到 <c>cart</c> 组的结果。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>带标识的字符串结果，可能为 <c>null</c>。</returns>
		string? GetGroupedNullable(int id);
	}

	/// <summary>
	/// <see cref="ICachedProbe"/> 的测试实现，通过调用计数与返回值开关观察缓存命中情况。
	/// </summary>
	public class CachedProbe : ICachedProbe
	{
		/// <summary>
		/// 异步方法体的执行次数。
		/// </summary>
		public int AsyncCalls;

		/// <summary>
		/// 同步方法体的执行次数。
		/// </summary>
		public int SyncCalls;

		/// <summary>
		/// 控制分组可空方法是否返回 <c>null</c>。
		/// </summary>
		public bool ReturnNull;

		/// <summary>
		/// 异步返回固定价格 10，并累计异步调用次数，结果按默认键缓存。
		/// </summary>
		/// <param name="id">价格标识。</param>
		/// <returns>固定价格 10。</returns>
		[Cache(TimeoutSeconds = 60)]
		public virtual async Task<decimal> GetPriceAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 10m;
		}

		/// <summary>
		/// 同步返回 <c>name-{id}</c>，并累计同步调用次数，结果按默认键缓存。
		/// </summary>
		/// <param name="id">名称标识。</param>
		/// <returns>带标识的名称值。</returns>
		[Cache(TimeoutSeconds = 60)]
		public virtual string GetName(int id)
		{
			SyncCalls++;
			return "name-" + id;
		}

		/// <summary>
		/// 返回 <paramref name="id"/> 加 0.5 的计算结果，按 <c>price-{0}-{1}</c> 模板键缓存。
		/// </summary>
		/// <param name="id">数值标识。</param>
		/// <param name="suffix">键模板使用的后缀。</param>
		/// <returns>计算结果。</returns>
		[Cache(Key = "price-{0}-{1}")]
		public virtual decimal GetCustomValue(int id, string suffix) => id + 0.5m;

		/// <summary>
		/// 返回固定字符串，用于验证键模板中的服务与方法占位符替换。
		/// </summary>
		/// <returns>固定字符串 <c>template</c>。</returns>
		[Cache(Key = "{service}.{method}")]
		public virtual string GetNameTemplate() => "template";

		/// <summary>
		/// 无返回值的同步操作，用于验证 void 方法不写缓存。
		/// </summary>
		/// <param name="id">操作标识。</param>
		[Cache(TimeoutSeconds = 60)]
		public virtual void DoWork(int id)
		{
			SyncCalls++;
		}

		/// <summary>
		/// 以 UTC 绝对到期时间缓存异步读取结果，返回固定价格 20。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>固定价格 20。</returns>
		[Cache(AbsoluteExpirationSeconds = 3600)]
		public virtual async Task<decimal> GetAbsoluteAsync(int id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 20m;
		}

		/// <summary>
		/// 以本地时间绝对到期缓存同步读取结果，返回固定值 30。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>固定值 30。</returns>
		[Cache(AbsoluteExpirationSeconds = 3600, IsUtc = false)]
		public virtual decimal GetAbsoluteLocal(int id)
		{
			SyncCalls++;
			return 30m;
		}

		/// <summary>
		/// 返回 <c>grouped-{id}</c>，并把缓存键登记到 <c>cart</c> 组。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>带标识的字符串结果。</returns>
		[Cache(Groups = ["cart"])]
		public virtual string GetGrouped(int id)
		{
			SyncCalls++;
			return "grouped-" + id;
		}

		/// <summary>
		/// 返回可能为 <c>null</c> 的字符串，并把缓存键登记到 <c>cart</c> 组，用于验证空结果的索引清理。
		/// </summary>
		/// <param name="id">结果标识。</param>
		/// <returns>带标识的字符串结果；<see cref="ReturnNull"/> 为 <c>true</c> 时返回 <c>null</c>。</returns>
		[Cache(Groups = ["cart"], AbsoluteExpirationSeconds = 3600)]
		public virtual string? GetGroupedNullable(int id)
		{
			SyncCalls++;
			return ReturnNull ? null : "v-" + id;
		}
	}

	/// <summary>
	/// 测试用服务提供程序：按请求类型返回预置的缓存服务与缓存组管理器，其余类型返回 <see langword="null"/>。
	/// </summary>
	private sealed class ServiceProviderStub : IServiceProvider
	{
		private readonly ICacheService _cache;

		private readonly ICacheGroupManager? _manager;

		public ServiceProviderStub(ICacheService cache, ICacheGroupManager? manager = null)
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

	/// <summary>
	/// 不包含任何服务的提供程序，用于验证拦截器在缺少缓存依赖时的回退行为。
	/// </summary>
	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object? GetService(Type serviceType) => null;
	}
}