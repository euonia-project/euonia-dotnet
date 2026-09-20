using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

public class IdempotentInterceptorTests
{
	[Fact]
	public void SyncCommand_SecondCallWithinWindow_ShouldExecuteOnce()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		proxy.SubmitOrder("o-1");
		proxy.SubmitOrder("o-1");

		Assert.Equal(1, probe.SyncCalls);
	}

	[Fact]
	public void SyncCommand_AfterWindowExpiry_ShouldExecuteAgain()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out var cache);

		proxy.SubmitOrder("o-1");
		Assert.Equal(1, probe.SyncCalls);

		cache.Advance(TimeSpan.FromSeconds(120));

		proxy.SubmitOrder("o-1");
		Assert.Equal(2, probe.SyncCalls);
	}

	[Fact]
	public async Task ValueMethod_Duplicate_ShouldReturnFirstResult()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		var first = await proxy.Charge("c-1");
		var second = await proxy.Charge("c-1");

		Assert.Equal(10m, first);
		Assert.Equal(10m, second);
		Assert.Equal(1, probe.AsyncCalls);
	}

	[Fact]
	public async Task ValueMethod_DifferentArguments_ShouldExecuteTwice()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		_ = await proxy.Charge("c-1");
		_ = await proxy.Charge("c-2");

		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public async Task RequestIdempotencyKey_ShouldScopeFingerprintRegardlessOfArguments()
	{
		var probe = new IdempotentProbe();
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext { Headers = new Dictionary<string, string> { ["Idempotency-Key"] = "req-1" } }
		};
		var proxy = CreateProxy(probe, out _, accessor);

		_ = await proxy.Charge("c-1");
		_ = await proxy.Charge("c-2");

		// 相同的 Idempotency-Key（即便实参不同）视为重复，方法体只执行一次。
		Assert.Equal(1, probe.AsyncCalls);

		accessor.Context.Headers["Idempotency-Key"] = "req-2";
		_ = await proxy.Charge("c-3");

		Assert.Equal(2, probe.AsyncCalls);
	}

	[Fact]
	public void SyncCommand_WithCustomKeyTemplate_ShouldDedup()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out var cache);

		proxy.SendCoupon("u-7", "XMAS");

		Assert.True(cache.TryGet("coupon-u-7-XMAS", out byte _));
		Assert.Equal(1, probe.SyncCalls);
	}

	[Fact]
	public async Task TaskMethod_SecondCall_ShouldSkip()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		await proxy.Notify("n-1");
		await proxy.Notify("n-1");

		Assert.Equal(1, probe.AsyncCalls);
	}

	[Fact]
	public async Task WithoutCacheServiceRegistered_ShouldExecuteAlways()
	{
		var probe = new IdempotentProbe();
		var interceptor = new IdempotentInterceptor(new EmptyServiceProvider());
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(IIdempotentProbe), probe, interceptor) as IIdempotentProbe;

		await proxy!.Charge("c-1");
		await proxy.Charge("c-1");

		Assert.Equal(2, probe.AsyncCalls);
	}

	private static IIdempotentProbe CreateProxy(IdempotentProbe probe, out FakeCacheService cache, IRequestContextAccessor accessor = null)
	{
		cache = new FakeCacheService();
		var interceptor = new IdempotentInterceptor(new ServiceProviderStub(cache), accessor);
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(IIdempotentProbe), probe, interceptor) as IIdempotentProbe;
	}

	public interface IIdempotentProbe
	{
		void SubmitOrder(string id);

		Task<decimal> Charge(string id);

		Task Notify(string id);

		void SendCoupon(string userId, string offer);
	}

	public class IdempotentProbe : IIdempotentProbe
	{
		public int SyncCalls;

		public int AsyncCalls;

		[Idempotent(TimeoutSeconds = 60)]
		public virtual void SubmitOrder(string id)
		{
			SyncCalls++;
		}

		[Idempotent(TimeoutSeconds = 60)]
		public virtual async Task<decimal> Charge(string id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 10m;
		}

		[Idempotent(TimeoutSeconds = 60)]
		public virtual async Task Notify(string id)
		{
			AsyncCalls++;
			await Task.Yield();
		}

		[Idempotent(Key = "coupon-{0}-{1}")]
		public virtual void SendCoupon(string userId, string offer)
		{
			SyncCalls++;
		}
	}

	private sealed class ServiceProviderStub : IServiceProvider
	{
		private readonly ICacheService _cache;

		public ServiceProviderStub(ICacheService cache)
		{
			_cache = cache;
		}

		public object GetService(Type serviceType) => serviceType == typeof(ICacheService) ? _cache : null;
	}

	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object GetService(Type serviceType) => null;
	}
}