using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application.Tests;

public class CircuitBreakerInterceptorTests
{
	private static string KeyOf(string method) => $"{typeof(CircuitProbe).FullName}.{method}";

	private static ICircuitProbe CreateProxy(CircuitProbe probe)
	{
		var interceptor = new CircuitBreakerInterceptor();
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, interceptor) as ICircuitProbe;
	}

	[Fact]
	public void Closed_NoFailures_ShouldExecuteEveryTime()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);

		_ = proxy.Echo("a");
		_ = proxy.Echo("b");
		_ = proxy.Echo("c");

		Assert.Equal(3, probe.Calls);
	}

	[Fact]
	public void Closed_FailuresBelowThreshold_ShouldContinueExecuting()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);

		// 前两次失败（各计 1 次，未达 3），第三次成功。
		Assert.Throws<InvalidOperationException>(() => proxy.FailTwiceThenOk());
		Assert.Throws<InvalidOperationException>(() => proxy.FailTwiceThenOk());
		Assert.Equal("ok", proxy.FailTwiceThenOk());

		Assert.Equal(3, probe.Calls);
	}

	[Fact]
	public void Open_AfterMaxFailures_ShouldFailFastWithoutExecuting()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);

		Assert.Throws<InvalidOperationException>(() => proxy.AlwaysFails());
		Assert.Throws<InvalidOperationException>(() => proxy.AlwaysFails());
		Assert.Throws<CircuitBreakerOpenException>(() => proxy.AlwaysFails());

		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public void Open_AfterReset_ProbeSuccess_ShouldClose()
	{
		CircuitStateStore.Clear();
		var clock = new MutableClock();
		CircuitStateStore.UtcNowProvider = () => clock.Now;
		try
		{
			var probe = new CircuitProbe();
			var proxy = CreateProxy(probe);

			// 首次调用失败，MaxFailures=1，熔断立即打开。
			Assert.Throws<InvalidOperationException>(() => proxy.FailFirstOnly());
			// 打开状态下快速失败。
			Assert.Throws<CircuitBreakerOpenException>(() => proxy.FailFirstOnly());

			// 超时后转入半开放行探测，探测成功关闭熔断。
			clock.Advance(TimeSpan.FromSeconds(61));
			Assert.Equal("ok", proxy.FailFirstOnly());

			// 已关闭，继续正常执行。
			Assert.Equal("ok", proxy.FailFirstOnly());

			Assert.Equal(3, probe.Calls);
		}
		finally
		{
			CircuitStateStore.UtcNowProvider = () => DateTime.UtcNow;
		}
	}

	[Fact]
	public void HalfOpen_ProbeFailure_ShouldReopenAndFailFast()
	{
		CircuitStateStore.Clear();
		var clock = new MutableClock();
		CircuitStateStore.UtcNowProvider = () => clock.Now;
		try
		{
			var probe = new CircuitProbe();
			var proxy = CreateProxy(probe);

			// 首次失败打开熔断。
			Assert.Throws<InvalidOperationException>(() => proxy.AlwaysFailsReopen());
			// 超时后转入半开，探测再次失败 → 立即重新打开。
			clock.Advance(TimeSpan.FromSeconds(61));
			Assert.Throws<InvalidOperationException>(() => proxy.AlwaysFailsReopen());
			// 尚未超时，再次快速失败。
			Assert.Throws<CircuitBreakerOpenException>(() => proxy.AlwaysFailsReopen());

			Assert.Equal(2, probe.Calls);
		}
		finally
		{
			CircuitStateStore.UtcNowProvider = () => DateTime.UtcNow;
		}
	}

	[Fact]
	public void Closed_Success_ShouldResetFailureCount()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);

		// 失败 → 成功 → 失败 → 成功：成功会重置失败计数，熔断始终不打开发。
		Assert.Throws<InvalidOperationException>(() => proxy.FailOnOddCalls());
		Assert.Equal("ok", proxy.FailOnOddCalls());
		Assert.Throws<InvalidOperationException>(() => proxy.FailOnOddCalls());
		Assert.Equal("ok", proxy.FailOnOddCalls());

		Assert.Equal(4, probe.Calls);
	}

	[Fact]
	public async Task AsyncTask_Open_ShouldReturnFaultedTaskWithoutExecuting()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);
		var key = KeyOf(nameof(CircuitProbe.AlwaysFailsAsync));

		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.AlwaysFailsAsync());
		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.AlwaysFailsAsync());
		await WaitUntilAsync(() => CircuitStateStore.GetStateKind(key) == CircuitStateKind.Open);

		await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => proxy.AlwaysFailsAsync());

		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public async Task ValueTask_Untyped_Open_ShouldFailFast()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);
		var key = KeyOf(nameof(CircuitProbe.AlwaysValueTask));

		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.AlwaysValueTask().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.AlwaysValueTask().AsTask());
		await WaitUntilAsync(() => CircuitStateStore.GetStateKind(key) == CircuitStateKind.Open);

		await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => proxy.AlwaysValueTask().AsTask());

		Assert.Equal(2, probe.Calls);
	}

	[Fact]
	public void WithoutAttribute_ShouldAlwaysExecute()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var proxy = CreateProxy(probe);

		_ = proxy.Plain("x");
		_ = proxy.Plain("y");
		_ = proxy.Plain("z");

		Assert.Equal(3, probe.Calls);
	}

	[Fact]
	public void HalfOpen_SingleFlight_ConcurrentProbeShouldFailFast()
	{
		CircuitStateStore.Clear();
		var clock = new MutableClock();
		CircuitStateStore.UtcNowProvider = () => clock.Now;
		try
		{
			var probe = new CircuitProbe();
			var proxy = CreateProxy(probe);
			var key = KeyOf(nameof(CircuitProbe.SingleFlightProbe));

			// 首次调用失败，熔断打开。
			Assert.Throws<InvalidOperationException>(() => proxy.SingleFlightProbe());
			clock.Advance(TimeSpan.FromSeconds(61));

			// 超时后转入半开，首个探测在途阻塞。
			var thread = new Thread(() => proxy.SingleFlightProbe());
			thread.Start();
			Assert.True(probe.Entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken), "Probe did not enter.");
			// 同一时刻只允许一个在途探测，并发探测快速失败（不执行）。
			Assert.Throws<CircuitBreakerOpenException>(() => proxy.SingleFlightProbe());

			// 放行探测，成功后关闭熔断。
			probe.Release.Set();
			Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "Probe did not complete.");
			Assert.True(WaitUntil(() => CircuitStateStore.GetStateKind(key) == CircuitStateKind.Closed), "Circuit did not close.");

			// 关闭后照常执行。
			proxy.SingleFlightProbe();
			Assert.Equal(3, probe.Calls);
		}
		finally
		{
			CircuitStateStore.UtcNowProvider = () => DateTime.UtcNow;
		}
	}

	[Fact]
	public void HalfOpen_SequentialSuccesses_ShouldCloseAfterThreshold()
	{
		CircuitStateStore.Clear();
		var clock = new MutableClock();
		CircuitStateStore.UtcNowProvider = () => clock.Now;
		try
		{
			var probe = new CircuitProbe();
			var proxy = CreateProxy(probe);
			var key = KeyOf(nameof(CircuitProbe.HalfOpenThreshold));

			// 首次失败打开，超时后进入半开。
			Assert.Throws<InvalidOperationException>(() => proxy.HalfOpenThreshold());
			clock.Advance(TimeSpan.FromSeconds(61));

			// 探测 1 成功：仍半开（累计 1/2）。
			Assert.Equal("ok", proxy.HalfOpenThreshold());
			Assert.Equal(CircuitStateKind.HalfOpen, CircuitStateStore.GetStateKind(key));
			// 探测 2 成功：达到 SuccessThreshold 关闭。
			Assert.Equal("ok", proxy.HalfOpenThreshold());
			Assert.Equal(CircuitStateKind.Closed, CircuitStateStore.GetStateKind(key));

			Assert.Equal(3, probe.Calls);
		}
		finally
		{
			CircuitStateStore.UtcNowProvider = () => DateTime.UtcNow;
		}
	}

	[Fact]
	public void BreakerOuter_RetryInner_ShouldCountSurfaceFailuresOnly()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, new CircuitBreakerInterceptor(), new RetryInterceptor()) as ICircuitProbe;

		// 每个调用：重试内部消耗 2 次执行后耗尽并冒泡原始异常，熔断对「冒泡」计 1 次失败。
		Assert.Throws<InvalidOperationException>(() => proxy.BreakerOutRetryIn());
		Assert.Throws<InvalidOperationException>(() => proxy.BreakerOutRetryIn());
		// 累计 2 次失败 → 打开，第三次快速失败不再执行。
		Assert.Throws<CircuitBreakerOpenException>(() => proxy.BreakerOutRetryIn());

		Assert.Equal(4, probe.Calls);
	}

	[Fact]
	public void RetryOuter_BreakerInner_ShouldOpenOnFirstAttemptAndFastFailRemaining()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, new RetryInterceptor(), new CircuitBreakerInterceptor()) as ICircuitProbe;

		// 第 1 次尝试失败即打开；后续每次重试都被熔断快速失败拦截（不再执行），耗尽重试后抛出 CircuitBreakerOpenException。
		Assert.Throws<CircuitBreakerOpenException>(() => proxy.RetryOutBreakerIn());

		Assert.Equal(1, probe.Calls);
	}

	private static bool WaitUntil(Func<bool> condition, int timeoutMs = 5000)
	{
		return System.Threading.SpinWait.SpinUntil(condition, TimeSpan.FromMilliseconds(timeoutMs));
	}

	private static async Task WaitUntilAsync(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline)
		{
			if (condition())
			{
				return;
			}

			await Task.Delay(10);
		}

		throw new TimeoutException($"Condition was not met within 5 seconds: {condition.Method?.Name}");
	}

	private sealed class MutableClock
	{
		public DateTime Now { get; set; } = DateTime.UtcNow;

		public void Advance(TimeSpan step)
		{
			Now = Now.Add(step);
		}
	}

	public interface ICircuitProbe
	{
		string Echo(string value);

		string FailTwiceThenOk();

		void AlwaysFails();

		string FailFirstOnly();

		void AlwaysFailsReopen();

		string FailOnOddCalls();

		Task<string> AlwaysFailsAsync();

		ValueTask AlwaysValueTask();

		string Plain(string value);

		void SingleFlightProbe();

		string HalfOpenThreshold();

		void BreakerOutRetryIn();

		void RetryOutBreakerIn();
	}

	public class CircuitProbe : ICircuitProbe
	{
		public int Calls;

		public ManualResetEventSlim Entered { get; } = new(false);

		public ManualResetEventSlim Release { get; } = new(false);

		[CircuitBreaker(MaxFailures = 3)]
		public virtual string Echo(string value)
		{
			Calls++;
			return value;
		}

		[CircuitBreaker(MaxFailures = 3)]
		public virtual string FailTwiceThenOk()
		{
			Calls++;
			if (Calls < 3)
			{
				throw new InvalidOperationException("transient");
			}

			return "ok";
		}

		[CircuitBreaker(MaxFailures = 2)]
		public virtual void AlwaysFails()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		[CircuitBreaker(MaxFailures = 1, ResetTimeoutSeconds = 60)]
		public virtual string FailFirstOnly()
		{
			Calls++;
			if (Calls == 1)
			{
				throw new InvalidOperationException("boom");
			}

			return "ok";
		}

		[CircuitBreaker(MaxFailures = 1, ResetTimeoutSeconds = 60)]
		public virtual void AlwaysFailsReopen()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		[CircuitBreaker(MaxFailures = 2)]
		public virtual string FailOnOddCalls()
		{
			Calls++;
			if (Calls % 2 == 1)
			{
				throw new InvalidOperationException("boom");
			}

			return "ok";
		}

		[CircuitBreaker(MaxFailures = 2)]
		public virtual async Task<string> AlwaysFailsAsync()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		[CircuitBreaker(MaxFailures = 2)]
		public virtual async ValueTask AlwaysValueTask()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		public virtual string Plain(string value)
		{
			Calls++;
			return value;
		}

		[CircuitBreaker(MaxFailures = 1, ResetTimeoutSeconds = 60)]
		public virtual void SingleFlightProbe()
		{
			Calls++;
			if (Calls == 1)
			{
				throw new InvalidOperationException("boom");
			}

			Entered.Set();
			if (!Release.Wait(TimeSpan.FromSeconds(10)))
			{
				throw new TimeoutException("release not signalled");
			}
		}

		[CircuitBreaker(MaxFailures = 1, ResetTimeoutSeconds = 60, SuccessThreshold = 2)]
		public virtual string HalfOpenThreshold()
		{
			Calls++;
			if (Calls == 1)
			{
				throw new InvalidOperationException("boom");
			}

			return "ok";
		}

		[CircuitBreaker(MaxFailures = 2)]
		[Retry(MaxRetries = 1)]
		public virtual void BreakerOutRetryIn()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		[Retry(MaxRetries = 2)]
		[CircuitBreaker(MaxFailures = 1)]
		public virtual void RetryOutBreakerIn()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}
	}
}