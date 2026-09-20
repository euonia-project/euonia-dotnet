using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application.Tests;

public class RetryInterceptorTests
{
	[Fact]
	public void SyncMethod_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = proxy.EchoFlaky("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public void SyncMethod_ExhaustsRetries_ShouldThrowOriginal()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		Assert.Throws<InvalidOperationException>(() => proxy.EchoAlwaysFails());
		Assert.Equal(3, probe.Calls);
	}

	[Fact]
	public void SyncMethod_NonRetryableException_ShouldNotRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		Assert.Throws<ArgumentException>(() => proxy.DoNonRetryable());
		Assert.Equal(1, probe.Calls);
	}

	[Fact]
	public async Task AsyncTask_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = await proxy.EchoFlakyAsync("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public async Task AsyncTask_FailsTwice_ThenCompletes()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await proxy.FlakyVoidAsync();

		Assert.Equal(3, probe.Calls);
	}

	[Fact]
	public async Task AsyncMethod_ExhaustsRetries_ShouldThrow()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.EchoAlwaysFailsAsync());
		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public async Task AsyncMethod_NonRetryableException_ShouldNotRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await Assert.ThrowsAsync<ArgumentException>(() => proxy.EchoNonRetryableAsync());
		Assert.Equal(1, probe.Calls);
	}

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

	[Fact]
	public void WithoutAttribute_ShouldExecuteOnlyOnce()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		_ = proxy.Echo("hello");

		Assert.Equal(1, probe.Calls);
	}

	[Fact]
	public async Task ValueTask_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		var result = await proxy.EchoFlakyValueTask("ok");

		Assert.Equal("ok", result);
		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public async Task ValueTask_Untyped_FailsThenSucceeds_ShouldRetry()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		await proxy.DoFlakyValueTask();

		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public void Retry_WithJitter_ShouldStillSucceed()
	{
		var probe = new RetryProbe();
		var proxy = CreateProxy(probe);

		proxy.DoDelayedJitter();

		Assert.Equal(3, probe.Calls);
	}

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

	private static IRetryProbe CreateProxy(RetryProbe probe)
	{
		var interceptor = new RetryInterceptor();
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(IRetryProbe), probe, interceptor) as IRetryProbe;
	}

	public interface IRetryProbe
	{
		string Echo(string value);

		string EchoFlaky(string value);

		string EchoAlwaysFails();

		void DoNonRetryable();

		Task<string> EchoFlakyAsync(string value);

		Task FlakyVoidAsync();

		Task<string> EchoAlwaysFailsAsync();

		Task<string> EchoNonRetryableAsync();

		void DoDelayed();

		void DoDelayedJitter();

		void DoExponential();

		ValueTask<string> EchoFlakyValueTask(string value);

		ValueTask DoFlakyValueTask();

		Task<string> GetFlakyAndCached();
	}

	public class RetryProbe : IRetryProbe
	{
		public int Calls;

		public List<DateTime> AttemptTimes { get; } = [];

		public virtual string Echo(string value)
		{
			Calls++;
			return value;
		}

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

		[Retry(MaxRetries = 2)]
		public virtual string EchoAlwaysFails()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		[Retry(MaxRetries = 3, RetryableExceptions = [typeof(InvalidOperationException)])]
		public virtual void DoNonRetryable()
		{
			Calls++;
			throw new ArgumentException("not retryable");
		}

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

		[Retry(MaxRetries = 1)]
		public virtual async Task<string> EchoAlwaysFailsAsync()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		[Retry(MaxRetries = 3, RetryableExceptions = [typeof(InvalidOperationException)])]
		public virtual async Task<string> EchoNonRetryableAsync()
		{
			Calls++;
			await Task.Yield();
			throw new ArgumentException("not retryable");
		}

		[Retry(MaxRetries = 2, DelayMs = 150)]
		public virtual void DoDelayed()
		{
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}
		}

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

		[Retry(MaxRetries = 2, DelayMs = 120, Jitter = true)]
		public virtual void DoDelayedJitter()
		{
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}
		}

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
	}

	private sealed class CacheStubProvider : IServiceProvider
	{
		private readonly Nerosoft.Euonia.Caching.ICacheService _cache;

		public CacheStubProvider(Nerosoft.Euonia.Caching.ICacheService cache)
		{
			_cache = cache;
		}

		public object GetService(Type serviceType) => serviceType == typeof(Nerosoft.Euonia.Caching.ICacheService) ? _cache : null;
	}
}