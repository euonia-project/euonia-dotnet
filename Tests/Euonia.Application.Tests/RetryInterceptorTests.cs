using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证 <see cref="RetryInterceptor"/> 对同步、异步与 <see cref="ValueTask"/> 方法的重试行为，包括异常过滤、退避策略以及与其他拦截器的组合。
/// </summary>
public class RetryInterceptorTests
{
	/// <summary>
	/// 同步方法首次抛出瞬时异常、第二次成功时，应自动重试并返回方法结果，共执行 2 次。
	/// </summary>
	[Fact]
	public void SyncMethod_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = proxy.EchoFlaky("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 同步方法在重试次数耗尽后仍失败时，应抛出最初的 <see cref="InvalidOperationException"/>；<c>MaxRetries=2</c> 时共执行 3 次。
	/// </summary>
	[Fact]
	public void SyncMethod_ExhaustsRetries_ShouldThrowOriginal()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		Assert.Throws<InvalidOperationException>(() => proxy.EchoAlwaysFails());
		Assert.Equal(3, probe.Calls);
	}

	/// <summary>
	/// 仅 <see cref="InvalidOperationException"/> 被声明为可重试时，<see cref="ArgumentException"/> 不应触发重试，方法只执行 1 次。
	/// </summary>
	[Fact]
	public void SyncMethod_NonRetryableException_ShouldNotRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		Assert.Throws<ArgumentException>(() => proxy.DoNonRetryable());
		Assert.Equal(1, probe.Calls);
	}

	/// <summary>
	/// 返回 Task 的异步方法首次失败后重试成功，应返回方法结果且共执行 2 次。
	/// </summary>
	[Fact]
	public async Task AsyncTask_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = await proxy.EchoFlakyAsync("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 无返回值的异步方法前两次失败、第三次成功时，重试后调用应正常完成，共执行 3 次。
	/// </summary>
	[Fact]
	public async Task AsyncTask_FailsTwice_ThenCompletes()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await proxy.FlakyVoidAsync();

		Assert.Equal(3, probe.Calls);
	}

	/// <summary>
	/// 异步方法在重试耗尽后仍失败时，应向上抛出 <see cref="InvalidOperationException"/>；<c>MaxRetries=1</c> 时共执行 2 次。
	/// </summary>
	[Fact]
	public async Task AsyncMethod_ExhaustsRetries_ShouldThrow()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.EchoAlwaysFailsAsync());
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 异步方法抛出未列入可重试类型的 <see cref="ArgumentException"/> 时不应重试，只执行 1 次。
	/// </summary>
	[Fact]
	public async Task AsyncMethod_NonRetryableException_ShouldNotRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await Assert.ThrowsAsync<ArgumentException>(() => proxy.EchoNonRetryableAsync());
		Assert.Equal(1, probe.Calls);
	}

	/// <summary>
	/// 固定延迟策略下，两次重试之间应按 <c>DelayMs=150</c> 等待，累计耗时不少于 250 毫秒且共执行 3 次。
	/// </summary>
	[Fact]
	public void Retry_WithFixedDelay_ShouldDelayBetweenAttempts()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		proxy.DoDelayed();
		stopwatch.Stop();

		Assert.Equal(3, probe.Calls);
		Assert.True(stopwatch.ElapsedMilliseconds >= 250, $"Expected at least 250ms of retry delay, got {stopwatch.ElapsedMilliseconds}ms.");
	}

	/// <summary>
	/// 指数退避策略下，相邻两次尝试的间隔应逐次增大，且共执行 3 次。
	/// </summary>
	[Fact]
	public void Retry_ExponentialBackoff_ShouldGrowDelay()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		proxy.DoExponential();

		Assert.Equal(3, probe.Calls);
		Assert.True(probe.AttemptTimes.Count >= 3);

		var firstGap = (probe.AttemptTimes[1] - probe.AttemptTimes[0]).TotalMilliseconds;
		var secondGap = (probe.AttemptTimes[2] - probe.AttemptTimes[1]).TotalMilliseconds;
		Assert.True(secondGap > firstGap, $"Expected exponential backoff to grow the delay ({firstGap}ms then {secondGap}ms).");
	}

	/// <summary>
	/// 未标注 <see cref="RetryAttribute"/> 的方法不应被重试，只执行 1 次。
	/// </summary>
	[Fact]
	public void WithoutAttribute_ShouldExecuteOnlyOnce()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		_ = proxy.Echo("hello");

		Assert.Equal(1, probe.Calls);
	}

	/// <summary>
	/// 返回 <see cref="ValueTask{TResult}"/> 的方法首次失败后重试成功，应返回结果且共执行 2 次。
	/// </summary>
	[Fact]
	public async Task ValueTask_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = await proxy.EchoFlakyValueTask("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 无返回值的 <see cref="ValueTask"/> 方法首次失败后重试成功，共执行 2 次。
	/// </summary>
	[Fact]
	public async Task ValueTask_Untyped_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await proxy.DoFlakyValueTask();

		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 启用抖动（jitter）后重试仍应成功完成，共执行 3 次。
	/// </summary>
	[Fact]
	public void Retry_WithJitter_ShouldStillSucceed()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		proxy.DoDelayedJitter();

		Assert.Equal(3, probe.Calls);
	}

	/// <summary>
	/// 缓存拦截器与重试拦截器组合时，首次调用应重试成功并写入缓存，第二次调用直接命中缓存，目标方法共执行 2 次。
	/// </summary>
	[Fact]
	public async Task Retry_CombinedWithCache_ShouldRetryThenServeFromCache()
	{
		var probe = new RetryProbe();
		var cache = new FakeCacheService();
		var cacheInterceptor = new CacheInterceptor(new CacheStubProvider(cache));
		var retryInterceptor = new RetryInterceptor();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(IRetryProbe), probe, cacheInterceptor, retryInterceptor) as IRetryProbe;

		var first = await proxy!.GetFlakyAndCached();
		var second = await proxy.GetFlakyAndCached();

		Assert.Equal("v", first);
		Assert.Equal("v", second);
		// 首次调用先重试成功（2 次执行），第二次调用命中缓存不再执行。
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 幂等拦截器与重试拦截器组合时，首次调用应重试成功并写入幂等印记，第二次调用被去重跳过，目标方法共执行 2 次。
	/// </summary>
	[Fact]
	public void Retry_CombinedWithIdempotent_ShouldRetryThenDedup()
	{
		var probe = new RetryProbe();
		var cache = new FakeCacheService();
		var idempotentInterceptor = new IdempotentInterceptor(new CacheStubProvider(cache));
		var retryInterceptor = new RetryInterceptor();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(IRetryProbe), probe, idempotentInterceptor, retryInterceptor) as IRetryProbe;

		proxy!.SubmitFlakyOrder();
		proxy.SubmitFlakyOrder();

		// 首次调用重试成功（执行 2 次）并写入幂等印记，第二次调用被幂等拦截跳过。
		Assert.Equal(2, probe.Calls);
	}

	/// <summary>
	/// 锁拦截器与重试拦截器组合时，首次调用应在持锁状态下重试成功（执行 2 次），第二次调用重新加锁执行（1 次），共 3 次。
	/// </summary>
	[Fact]
	public void Retry_CombinedWithLock_ShouldRetryWhileHoldingLock()
	{
		var probe = new RetryProbe();
		var lockInterceptor = new LockInterceptor(new EmptyServiceProvider());
		var retryInterceptor = new RetryInterceptor();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(IRetryProbe), probe, lockInterceptor, retryInterceptor) as IRetryProbe;

		proxy!.SubmitLockedFlaky();
		proxy.SubmitLockedFlaky();

		// 首次调用在锁内先失败后重试成功（执行 2 次）；第二次调用重新获取锁执行 1 次。
		Assert.Equal(3, probe.Calls);
	}

	private static IRetryProbe CreateProxy(RetryProbe probe)
	{
		var interceptor = new RetryInterceptor();
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(IRetryProbe), probe, interceptor) as IRetryProbe
		       ?? throw new InvalidOperationException("Failed to create the retry proxy.");
	}

	/// <summary>
	/// 供 <see cref="RetryInterceptor"/> 拦截的探针契约，覆盖同步、Task 与 ValueTask 形态的方法。
	/// </summary>
	public interface IRetryProbe
	{
		/// <summary>
		/// 记录一次调用并原样返回输入值，用于验证无重试特性时的单次执行。
		/// </summary>
		/// <param name="value">要返回的输入值。</param>
		/// <returns>传入的 <paramref name="value"/>。</returns>
		string Echo(string value);

		/// <summary>
		/// 首次调用抛出瞬时异常、之后返回输入值，用于验证同步重试。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后的 <paramref name="value"/>。</returns>
		string EchoFlaky(string value);

		/// <summary>
		/// 每次调用都抛出 <see cref="InvalidOperationException"/>，用于验证重试耗尽后抛出原异常。
		/// </summary>
		/// <returns>不会正常返回；调用总是抛出异常。</returns>
		/// <exception cref="InvalidOperationException">每次调用都会抛出。</exception>
		string EchoAlwaysFails();

		/// <summary>
		/// 抛出未列入可重试类型的 <see cref="ArgumentException"/>，用于验证异常过滤。
		/// </summary>
		/// <exception cref="ArgumentException">每次调用都会抛出。</exception>
		void DoNonRetryable();

		/// <summary>
		/// 异步版本：首次调用失败、之后返回输入值，用于验证 Task 方法的重试。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后包含 <paramref name="value"/> 的任务。</returns>
		Task<string> EchoFlakyAsync(string value);

		/// <summary>
		/// 无返回值的异步方法：前两次调用失败、第三次成功，用于验证重试后正常完成。
		/// </summary>
		/// <returns>重试成功后完成的任务。</returns>
		Task FlakyVoidAsync();

		/// <summary>
		/// 异步方法每次调用都失败，用于验证重试耗尽后异常向上传播。
		/// </summary>
		/// <returns>不会正常完成；任务总是以异常结束。</returns>
		/// <exception cref="InvalidOperationException">每次调用都会抛出。</exception>
		Task<string> EchoAlwaysFailsAsync();

		/// <summary>
		/// 异步方法抛出未列入可重试类型的异常，用于验证异步路径的异常过滤。
		/// </summary>
		/// <returns>不会正常完成；任务总是以异常结束。</returns>
		/// <exception cref="ArgumentException">每次调用都会抛出。</exception>
		Task<string> EchoNonRetryableAsync();

		/// <summary>
		/// 前两次调用失败、第三次成功，配合固定 <c>DelayMs</c> 验证重试间隔。
		/// </summary>
		void DoDelayed();

		/// <summary>
		/// 与 <see cref="DoDelayed"/> 相同，但启用抖动以验证随机化退避仍能完成重试。
		/// </summary>
		void DoDelayedJitter();

		/// <summary>
		/// 记录每次尝试的时间，用于验证指数退避下相邻尝试间隔递增。
		/// </summary>
		void DoExponential();

		/// <summary>
		/// <see cref="ValueTask{TResult}"/> 版本：首次调用失败、之后返回输入值。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后包含 <paramref name="value"/> 的 ValueTask。</returns>
		ValueTask<string> EchoFlakyValueTask(string value);

		/// <summary>
		/// 无返回值的 <see cref="ValueTask"/> 版本：首次调用失败、之后成功。
		/// </summary>
		/// <returns>重试成功后完成的 ValueTask。</returns>
		ValueTask DoFlakyValueTask();

		/// <summary>
		/// 同时标注缓存与重试特性：首次调用失败后重试并返回 <c>"v"</c>，结果随后写入缓存。
		/// </summary>
		/// <returns>固定值 <c>"v"</c>。</returns>
		Task<string> GetFlakyAndCached();

		/// <summary>
		/// 同时标注幂等与重试特性：首次调用失败后重试成功，并写入幂等印记。
		/// </summary>
		void SubmitFlakyOrder();

		/// <summary>
		/// 同时标注分布式锁与重试特性：首次调用在锁内失败后重试成功。
		/// </summary>
		void SubmitLockedFlaky();
	}

	/// <summary>
	/// <see cref="IRetryProbe"/> 的默认实现：统计调用次数与尝试时间，并按特性配置制造可重试失败。
	/// </summary>
	public class RetryProbe : IRetryProbe
	{
		/// <summary>
		/// 获取或设置目标方法被实际执行的次数。
		/// </summary>
		public int Calls;

		/// <summary>
		/// 获取每次尝试的 UTC 时间，用于计算退避间隔。
		/// </summary>
		public List<DateTime> AttemptTimes { get; } = [];

		/// <summary>
		/// 记录一次调用并原样返回输入值；未标注重试特性。
		/// </summary>
		/// <param name="value">要返回的输入值。</param>
		/// <returns>传入的 <paramref name="value"/>。</returns>
		public virtual string Echo(string value)
		{
			Calls++;
			return value;
		}

		/// <summary>
		/// 首次调用抛出瞬时异常、第二次返回输入值（<c>MaxRetries=3</c>）。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后的 <paramref name="value"/>。</returns>
		[Retry(MaxRetries = 3)]
		public virtual string EchoFlaky(string value)
		{
			Calls++;
			if (Calls < 2)
			{
				throw new InvalidOperationException("transient");
			}

			return value;
		}

		/// <summary>
		/// 每次调用都抛出 <see cref="InvalidOperationException"/>（<c>MaxRetries=2</c>）。
		/// </summary>
		/// <returns>不会正常返回。</returns>
		/// <exception cref="InvalidOperationException">每次调用都会抛出。</exception>
		[Retry(MaxRetries = 2)]
		public virtual string EchoAlwaysFails()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 抛出 <see cref="ArgumentException"/>，而可重试类型仅包含 <see cref="InvalidOperationException"/>。
		/// </summary>
		/// <exception cref="ArgumentException">每次调用都会抛出。</exception>
		[Retry(MaxRetries = 3, RetryableExceptions = [typeof(InvalidOperationException)])]
		public virtual void DoNonRetryable()
		{
			Calls++;
			throw new ArgumentException("not retryable");
		}

		/// <summary>
		/// 异步方法首次失败、第二次返回输入值（<c>MaxRetries=3</c>）。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后包含 <paramref name="value"/> 的任务。</returns>
		[Retry(MaxRetries = 3)]
		public virtual async Task<string> EchoFlakyAsync(string value)
		{
			Calls++;
			if (Calls < 2)
			{
				await Task.Yield();
				throw new InvalidOperationException("transient");
			}

			await Task.Yield();
			return value;
		}

		/// <summary>
		/// 无返回值异步方法：前两次失败、第三次成功（<c>MaxRetries=3</c>）。
		/// </summary>
		/// <returns>重试成功后完成的任务。</returns>
		[Retry(MaxRetries = 3)]
		public virtual async Task FlakyVoidAsync()
		{
			Calls++;
			if (Calls < 3)
			{
				await Task.Yield();
				throw new InvalidOperationException("transient");
			}

			await Task.Yield();
		}

		/// <summary>
		/// 异步方法每次调用都抛出异常（<c>MaxRetries=1</c>）。
		/// </summary>
		/// <returns>不会正常完成。</returns>
		/// <exception cref="InvalidOperationException">每次调用都会抛出。</exception>
		[Retry(MaxRetries = 1)]
		public virtual async Task<string> EchoAlwaysFailsAsync()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 异步方法抛出未列入可重试类型的 <see cref="ArgumentException"/>。
		/// </summary>
		/// <returns>不会正常完成。</returns>
		/// <exception cref="ArgumentException">每次调用都会抛出。</exception>
		[Retry(MaxRetries = 3, RetryableExceptions = [typeof(InvalidOperationException)])]
		public virtual async Task<string> EchoNonRetryableAsync()
		{
			Calls++;
			await Task.Yield();
			throw new ArgumentException("not retryable");
		}

		/// <summary>
		/// 前两次失败、第三次成功，每次重试固定等待 150 毫秒。
		/// </summary>
		[Retry(MaxRetries = 2, DelayMs = 150)]
		public virtual void DoDelayed()
		{
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}
		}

		/// <summary>
		/// 前两次失败、第三次成功，基础间隔 25 毫秒并采用指数退避；每次尝试前记录 UTC 时间。
		/// </summary>
		[Retry(MaxRetries = 2, DelayMs = 25, Backoff = RetryBackoffMode.Exponential)]
		public virtual void DoExponential()
		{
			AttemptTimes.Add(DateTime.UtcNow);
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}
		}

		/// <summary>
		/// 前两次失败、第三次成功，基础间隔 120 毫秒并启用抖动。
		/// </summary>
		[Retry(MaxRetries = 2, DelayMs = 120, Jitter = true)]
		public virtual void DoDelayedJitter()
		{
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}
		}

		/// <summary>
		/// <see cref="ValueTask{TResult}"/> 方法首次失败、第二次返回输入值。
		/// </summary>
		/// <param name="value">成功时返回的输入值。</param>
		/// <returns>重试成功后包含 <paramref name="value"/> 的 ValueTask。</returns>
		[Retry(MaxRetries = 3)]
		public virtual async ValueTask<string> EchoFlakyValueTask(string value)
		{
			Calls++;
			if (Calls < 2)
			{
				await Task.Yield();
				throw new InvalidOperationException("transient");
			}

			await Task.Yield();
			return value;
		}

		/// <summary>
		/// 无返回值 <see cref="ValueTask"/> 方法首次失败、第二次成功。
		/// </summary>
		/// <returns>重试成功后完成的 ValueTask。</returns>
		[Retry(MaxRetries = 3)]
		public virtual async ValueTask DoFlakyValueTask()
		{
			Calls++;
			if (Calls < 2)
			{
				await Task.Yield();
				throw new InvalidOperationException("transient");
			}

			await Task.Yield();
		}

		/// <summary>
		/// 首次失败后重试并返回 <c>"v"</c>，同时由缓存拦截器缓存结果。
		/// </summary>
		/// <returns>固定值 <c>"v"</c>。</returns>
		[Cache(TimeoutSeconds = 60)]
		[Retry(MaxRetries = 3)]
		public virtual async Task<string> GetFlakyAndCached()
		{
			Calls++;
			if (Calls < 2)
			{
				await Task.Yield();
				throw new InvalidOperationException("transient");
			}

			await Task.Yield();
			return "v";
		}

		/// <summary>
		/// 首次失败后重试成功，并由幂等拦截器写入幂等印记。
		/// </summary>
		[Idempotent(TimeoutSeconds = 60)]
		[Retry(MaxRetries = 3)]
		public virtual void SubmitFlakyOrder()
		{
			Calls++;
			if (Calls < 2)
			{
				throw new InvalidOperationException("transient");
			}
		}

		/// <summary>
		/// 首次在锁内失败后重试成功，锁键为 <c>flaky-lock</c>。
		/// </summary>
		[SemaphoreLock("flaky-lock", Timeout = 2000)]
		[Retry(MaxRetries = 3)]
		public virtual void SubmitLockedFlaky()
		{
			Calls++;
			if (Calls < 2)
			{
				throw new InvalidOperationException("transient");
			}
		}
	}

	/// <summary>
	/// 测试用服务提供程序：仅返回预置的缓存服务，其余类型返回 <see langword="null"/>，用于让重试拦截器取得缓存依赖。
	/// </summary>
	private sealed class CacheStubProvider : IServiceProvider
	{
		private readonly Nerosoft.Euonia.Caching.ICacheService _cache;

		public CacheStubProvider(Nerosoft.Euonia.Caching.ICacheService cache)
		{
			_cache = cache;
		}

		public object? GetService(Type serviceType) => serviceType == typeof(Nerosoft.Euonia.Caching.ICacheService) ? _cache : null;
	}

	/// <summary>
	/// 不包含任何服务的提供程序，用于验证拦截器在无法解析缓存依赖时的行为。
	/// </summary>
	private sealed class EmptyServiceProvider : IServiceProvider
	{
		public object? GetService(Type serviceType) => null;
	}
}