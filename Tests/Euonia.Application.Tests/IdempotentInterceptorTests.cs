using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// <see cref="IdempotentInterceptor"/> 的测试，覆盖同步与异步方法的幂等窗口、缓存过期后的重新执行、
/// 请求幂等键、自定义键模板、缺少缓存服务以及经由锁工厂去重的场景。
/// </summary>
public class IdempotentInterceptorTests
{
	/// <summary>
	/// 同一实参在幂等窗口内重复调用同步命令时，方法体只执行一次。
	/// </summary>
	[Fact]
	public void SyncCommand_SecondCallWithinWindow_ShouldExecuteOnce()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		proxy.SubmitOrder("o-1");
		proxy.SubmitOrder("o-1");

		Assert.Equal(1, probe.SyncCalls);
	}

	/// <summary>
	/// 幂等窗口过期后，相同实参的调用应再次执行方法体。
	/// </summary>
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

	/// <summary>
	/// 重复调用有返回值的异步方法时返回首次执行结果，且方法体只执行一次。
	/// </summary>
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

	/// <summary>
	/// 实参不同会生成不同指纹，因此两次调用各自执行一次。
	/// </summary>
	[Fact]
	public async Task ValueMethod_DifferentArguments_ShouldExecuteTwice()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		_ = await proxy.Charge("c-1");
		_ = await proxy.Charge("c-2");

		Assert.Equal(2, probe.AsyncCalls);
	}

	/// <summary>
	/// 存在 <c>Idempotency-Key</c> 请求头时以该键界定去重范围，即使实参不同也只执行一次；
	/// 键变化后新的调用会重新执行。
	/// </summary>
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

	/// <summary>
	/// 配置了自定义键模板时按模板格式化实参生成缓存键，并据此完成去重。
	/// </summary>
	[Fact]
	public void SyncCommand_WithCustomKeyTemplate_ShouldDedup()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out var cache);

		proxy.SendCoupon("u-7", "XMAS");

		Assert.True(cache.TryGet("coupon-u-7-XMAS", out byte _));
		Assert.Equal(1, probe.SyncCalls);
	}

	/// <summary>
	/// 无返回值的 <see cref="Task"/> 方法在同一实参下重复调用时，第二次应被跳过。
	/// </summary>
	[Fact]
	public async Task TaskMethod_SecondCall_ShouldSkip()
	{
		var probe = new IdempotentProbe();
		var proxy = CreateProxy(probe, out _);

		await proxy.Notify("n-1");
		await proxy.Notify("n-1");

		Assert.Equal(1, probe.AsyncCalls);
	}

	/// <summary>
	/// 未注册 <see cref="ICacheService"/> 时拦截器退化为直接放行，重复调用每次都执行。
	/// </summary>
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

	/// <summary>
	/// 注册锁工厂后异步与同步调用都经由工厂创建锁，并保证重复调用只执行一次。
	/// </summary>
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

	/// <summary>
	/// 并发重复调用经锁工厂合并为一次执行，两个调用者都得到相同结果。
	/// </summary>
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

	private static IIdempotentProbe CreateProxy(IdempotentProbe probe, out FakeCacheService cache, IRequestContextAccessor? accessor = null, InMemoryLockFactory? lockFactory = null)
	{
		cache = new FakeCacheService();
		var interceptor = new IdempotentInterceptor(new ServiceProviderStub(cache, lockFactory), accessor);
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(IIdempotentProbe), probe, interceptor) as IIdempotentProbe
		       ?? throw new InvalidOperationException("Failed to create the idempotent proxy.");
	}

	/// <summary>
	/// 幂等拦截器测试使用的探测接口，声明同步、异步、带返回值以及带自定义键模板的被拦截方法。
	/// </summary>
	public interface IIdempotentProbe
	{
		/// <summary>
		/// 同步提交订单，用于验证窗口内以相同标识重复提交只执行一次。
		/// </summary>
		void SubmitOrder(string id);

		/// <summary>
		/// 异步扣款并返回金额，用于验证重复调用返回首次结果。
		/// </summary>
		Task<decimal> Charge(string id);

		/// <summary>
		/// 可延迟完成的异步扣款，用于验证并发重复调用被合并为一次执行。
		/// </summary>
		Task<decimal> Charge(bool delay);

		/// <summary>
		/// 无返回值的异步通知，用于验证 <see cref="Task"/> 方法的去重。
		/// </summary>
		Task Notify(string id);

		/// <summary>
		/// 使用自定义键模板的同步发券方法，用于验证模板生成的缓存键可被去重命中。
		/// </summary>
		void SendCoupon(string userId, string offer);
	}

	/// <summary>
	/// <see cref="IIdempotentProbe"/> 的测试实现，统计同步与异步方法体的真实执行次数。
	/// </summary>
	public class IdempotentProbe : IIdempotentProbe
	{
		/// <summary>
		/// 同步方法体被执行的总次数，用于断言去重是否生效。
		/// </summary>
		public int SyncCalls;

		/// <summary>
		/// 异步方法体被执行的总次数，用于断言去重是否生效。
		/// </summary>
		public int AsyncCalls;

		/// <summary>
		/// 记录一次同步调用，模拟提交订单。
		/// </summary>
		[Idempotent(TimeoutSeconds = 60)]
		public virtual void SubmitOrder(string id)
		{
			SyncCalls++;
		}

		/// <summary>
		/// 记录一次异步调用并返回固定金额 <c>10</c>。
		/// </summary>
		[Idempotent(TimeoutSeconds = 60)]
		public virtual async Task<decimal> Charge(string id)
		{
			AsyncCalls++;
			await Task.Yield();
			return 10m;
		}

		/// <summary>
		/// 记录一次异步调用，按 <paramref name="delay"/> 决定是否延迟完成后返回固定金额 <c>10</c>。
		/// </summary>
		/// <param name="delay">是否延迟 200 毫秒以模拟耗时操作。</param>
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

		/// <summary>
		/// 记录一次异步调用，模拟发送通知。
		/// </summary>
		[Idempotent(TimeoutSeconds = 60)]
		public virtual async Task Notify(string id)
		{
			AsyncCalls++;
			await Task.Yield();
		}

		/// <summary>
		/// 记录一次同步调用，缓存键由 <c>coupon-{userId}-{offer}</c> 模板生成。
		/// </summary>
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

	/// <summary>
	/// 进程内锁提供程序：以 <see cref="SemaphoreSlim"/> 模拟分布式锁，供幂等拦截器在并发测试中使用。
	/// </summary>
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

		public ISynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
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

		public ValueTask<ISynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
		{
			return TryAcquireAsyncCore(timeout, cancellationToken);
		}

		private async ValueTask<ISynchronizationHandle?> TryAcquireAsyncCore(TimeSpan timeout, CancellationToken cancellationToken)
		{
			return await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false) ? new Handle(_semaphore) : null;
		}

		public ValueTask<ISynchronizationHandle?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken = default)
		{
			return TryAcquireAsync(timeout.IsInfinite ? Timeout.InfiniteTimeSpan : timeout.TimeSpan, cancellationToken);
		}

		/// <summary>
		/// 锁句柄：释放时归还信号量，使其他等待者可以继续获取同一把锁。
		/// </summary>
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

	/// <summary>
	/// 测试用服务提供程序：按请求类型返回预置的缓存服务与锁工厂，其余类型返回 <see langword="null"/>。
	/// </summary>
	private sealed class ServiceProviderStub : IServiceProvider
	{
		private readonly ICacheService _cache;

		private readonly ILockFactory? _lockFactory;

		public ServiceProviderStub(ICacheService cache, InMemoryLockFactory? lockFactory = null)
		{
			_cache = cache;
			_lockFactory = lockFactory;
		}

		public object? GetService(Type serviceType)
		{
			if (serviceType == typeof(ICacheService))
			{
				return _cache;
			}

			return serviceType == typeof(ILockFactory) ? _lockFactory : null;
		}
	}

	/// <summary>
	/// 不包含任何服务的提供程序，用于验证拦截器在缺少锁工厂时的回退行为。
	/// </summary>
	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object? GetService(Type serviceType) => null;
	}
}