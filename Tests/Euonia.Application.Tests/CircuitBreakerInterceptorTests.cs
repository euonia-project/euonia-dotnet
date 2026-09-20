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
	}

	public class CircuitProbe : ICircuitProbe
	{
		public int Calls;

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
	}
}