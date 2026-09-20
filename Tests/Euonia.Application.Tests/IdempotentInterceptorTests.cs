using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Threading;

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

	[Fact]
	public async Task WithLockFactoryRegistered_ShouldRouteThroughFactoryAndDedup()
	{
		var probe = new IdempotentProbe();
		var factory = new InMemoryLockFactory();
		var proxy = CreateProxy(probe, out _, lockFactory: factory);

		var first = await proxy.Charge("c-1");
		var second = await proxy.Charge("c-1");

		Assert.Equal(10m, first);
		Assert.Equal(10m, second);
		Assert.Equal(1, probe.AsyncCalls);
		Assert.True(factory.AnyProvider(key => key.Contains(".Charge", StringComparison.Ordinal)));

		// 同步路径同样走锁工厂。
		proxy.SubmitOrder("o-1");
		proxy.SubmitOrder("o-1");
		Assert.Equal(1, probe.SyncCalls);
		Assert.True(factory.AnyProvider(key => key.Contains(".SubmitOrder", StringComparison.Ordinal)));
	}

	[Fact]
	public async Task WithLockFactory_ConcurrentDuplicate_ShouldCoalesceToSingleExecution()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _, lockFactory: new InMemoryLockFactory());

		var results = await Task.WhenAll(proxy.Charge(delay: true), proxy.Charge(delay: true));

		Assert.Equal(10m, results[0]);
		Assert.Equal(10m, results[1]);
		Assert.Equal(1, probe.AsyncCalls);
	}

	private static IIdempotentProbe CreateProxy(IdempotentProbe probe, out FakeCacheService cache, IRequestContextAccessor accessor = null, InMemoryLockFactory lockFactory = null)
	{
		cache = new FakeCacheService();
		var interceptor = new IdempotentInterceptor(new ServiceProviderStub(cache, lockFactory), accessor);
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(IIdempotentProbe), probe, interceptor) as IIdempotentProbe;
	}

	public interface IIdempotentProbe
	{
		void SubmitOrder(string id);

		Task<decimal> Charge(string id);

		Task<decimal> Charge(bool delay);

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
		public virtual async Task<decimal> Charge(bool delay)
		{
			AsyncCalls++;
			if (delay)
			{
				await Task.Delay(200);
			}

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

	/// <summary>
	/// 模拟分布式锁工厂（进程内信号量实现），用以验证 <see cref="IdempotentInterceptor"/> 走工厂路由。
	/// </summary>
	private sealed class InMemoryLockFactory : ILockFactory
	{
		private readonly ConcurrentDictionary<string, InMemoryLockProvider> _providers = new();

		public bool AnyProvider(Func<string, bool> predicate) => _providers.Keys.Any(predicate);

		public ILockProvider Create(string name)
		{
			return _providers.GetOrAdd(name, _ => new InMemoryLockProvider(name));
		}
	}

	private sealed class InMemoryLockProvider : ILockProvider
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);

		public InMemoryLockProvider(string name)
		{
			Name = name;
		}

		public string Name { get; }

		public ISynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			if (!_semaphore.Wait(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken))
			{
				throw new TimeoutException($"Failed to acquire the distributed lock '{Name}'.");
			}

			return new Handle(_semaphore);
		}

		public ISynchronizationHandle TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			return _semaphore.Wait(timeout, cancellationToken) ? new Handle(_semaphore) : null;
		}

		public ValueTask<ISynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		{
			return AcquireAsyncCore(timeout, cancellationToken);
		}

		private async ValueTask<ISynchronizationHandle> AcquireAsyncCore(TimeSpan? timeout, CancellationToken cancellationToken)
		{
			if (!await _semaphore.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false))
			{
				throw new TimeoutException($"Failed to acquire the distributed lock '{Name}'.");
			}

			return new Handle(_semaphore);
		}

		public ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			return TryAcquireAsyncCore(timeout, cancellationToken);
		}

		private async ValueTask<ISynchronizationHandle> TryAcquireAsyncCore(TimeSpan timeout, CancellationToken cancellationToken)
		{
			return await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false) ? new Handle(_semaphore) : null;
		}

		public ValueTask<ISynchronizationHandle> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default)
		{
			return TryAcquireAsync(timeout.IsInfinite ? Timeout.InfiniteTimeSpan : timeout.TimeSpan, cancellationToken);
		}

		private sealed class Handle : ISynchronizationHandle
		{
			private readonly SemaphoreSlim _semaphore;

			public Handle(SemaphoreSlim semaphore)
			{
				_semaphore = semaphore;
			}

			public CancellationToken HandleCancellationToken => CancellationToken.None;

			public void Dispose() => _semaphore.Release();

			public ValueTask DisposeAsync()
			{
				_semaphore.Release();
				return ValueTask.CompletedTask;
			}
		}
	}

	private sealed class ServiceProviderStub : IServiceProvider
	{
		private readonly ICacheService _cache;

		private readonly ILockFactory _lockFactory;

		public ServiceProviderStub(ICacheService cache, InMemoryLockFactory lockFactory = null)
		{
			_cache = cache;
			_lockFactory = lockFactory;
		}

		public object GetService(Type serviceType)
		{
			if (serviceType == typeof(ICacheService))
			{
				return _cache;
			}

			return serviceType == typeof(ILockFactory) ? _lockFactory : null;
		}
	}

	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object GetService(Type serviceType) => null;
	}
}