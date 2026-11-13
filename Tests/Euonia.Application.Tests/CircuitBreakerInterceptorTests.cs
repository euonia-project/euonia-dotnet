using Castle.DynamicProxy;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// <see cref="CircuitBreakerInterceptor"/> 的行为测试，覆盖闭路执行、失败阈值开路、重置超时后的半开探测、
/// 单飞探测、成功阈值关闭，以及与重试拦截器不同嵌套顺序下的计数语义。
/// </summary>
public class CircuitBreakerInterceptorTests
{
	private static string KeyOf(string method) => $"{typeof(CircuitProbe).FullName}.{method}";

	private static ICircuitProbe CreateProxy(CircuitProbe probe)
	{
		var interceptor = new CircuitBreakerInterceptor();
		var generator = new ProxyGenerator();
		return generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, interceptor) as ICircuitProbe
		       ?? throw new InvalidOperationException("Failed to create the circuit breaker proxy.");
	}

	/// <summary>
	/// 熔断处于关闭状态且没有失败时，每次调用都应真实执行目标方法。
	/// </summary>
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

	/// <summary>
	/// 失败次数低于阈值（3 次）时熔断保持关闭并继续执行，第三次调用成功返回 <c>ok</c>。
	/// </summary>
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

	/// <summary>
	/// 连续两次失败达到 <c>MaxFailures</c> 后熔断打开，第三次调用快速失败且不再执行目标方法。
	/// </summary>
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

	/// <summary>
	/// 熔断打开后等待超过重置超时会进入半开状态，探测成功即关闭熔断并恢复正常执行。
	/// </summary>
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

	/// <summary>
	/// 半开状态的探测再次失败时会立即重新打开熔断，未到重置超时的后续调用快速失败。
	/// </summary>
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

	/// <summary>
	/// 失败之后的一次成功会重置累计失败计数，交替的失败与成功不会打开熔断。
	/// </summary>
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

	/// <summary>
	/// 返回 <see cref="Task{TResult}"/> 的异步方法在熔断打开后应直接返回故障任务，不执行方法体。
	/// </summary>
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

	/// <summary>
	/// 返回无结果 <see cref="ValueTask"/> 的异步方法在熔断打开后同样快速失败且不执行方法体。
	/// </summary>
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

	/// <summary>
	/// 未标注 <see cref="CircuitBreakerAttribute"/> 的方法不受熔断约束，每次调用都实际执行。
	/// </summary>
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

	/// <summary>
	/// 半开状态同一时刻只允许一个在途探测，并发探测会快速失败而不会执行目标方法。
	/// </summary>
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

	/// <summary>
	/// 半开状态下的成功探测累计达到 <c>SuccessThreshold</c> 后熔断才关闭。
	/// </summary>
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

	/// <summary>
	/// 熔断器位于重试拦截器外层时只按冒泡到外层的失败计数，重试内部消耗的执行次数不计入熔断统计。
	/// </summary>
	[Fact]
	public void BreakerOuter_RetryInner_ShouldCountSurfaceFailuresOnly()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, new CircuitBreakerInterceptor(), new RetryInterceptor()) as ICircuitProbe
		            ?? throw new InvalidOperationException("Failed to create the circuit breaker proxy.");

		// 每个调用：重试内部消耗 2 次执行后耗尽并冒泡原始异常，熔断对「冒泡」计 1 次失败。
		Assert.Throws<InvalidOperationException>(() => proxy.BreakerOutRetryIn());
		Assert.Throws<InvalidOperationException>(() => proxy.BreakerOutRetryIn());
		// 累计 2 次失败 → 打开，第三次快速失败不再执行。
		Assert.Throws<CircuitBreakerOpenException>(() => proxy.BreakerOutRetryIn());

		Assert.Equal(4, probe.Calls);
	}

	/// <summary>
	/// 重试拦截器位于熔断器外层时首次尝试失败即打开熔断，剩余重试全部被快速失败拦截。
	/// </summary>
	[Fact]
	public void RetryOuter_BreakerInner_ShouldOpenOnFirstAttemptAndFastFailRemaining()
	{
		CircuitStateStore.Clear();
		var probe = new CircuitProbe();
		var generator = new ProxyGenerator();
		var proxy = generator.CreateInterfaceProxyWithTarget(typeof(ICircuitProbe), probe, new RetryInterceptor(), new CircuitBreakerInterceptor()) as ICircuitProbe
		            ?? throw new InvalidOperationException("Failed to create the circuit breaker proxy.");

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

	/// <summary>
	/// 可手动推进的测试时钟，用于在不真实等待的情况下触发熔断器的重置超时。
	/// </summary>
	private sealed class MutableClock
	{
		public DateTime Now { get; set; } = DateTime.UtcNow;

		public void Advance(TimeSpan step)
		{
			Now = Now.Add(step);
		}
	}

	/// <summary>
	/// 熔断器测试使用的探测接口，覆盖同步、异步、<see cref="ValueTask"/> 以及并发单飞等调用形态。
	/// </summary>
	public interface ICircuitProbe
	{
		/// <summary>
		/// 原样返回传入的字符串，用于验证闭路状态下每次调用都会真实执行。
		/// </summary>
		string Echo(string value);

		/// <summary>
		/// 前两次调用抛出异常、第三次返回 <c>ok</c>，用于验证阈值以下的失败不会打开熔断。
		/// </summary>
		string FailTwiceThenOk();

		/// <summary>
		/// 每次调用都抛出异常，用于验证达到失败阈值后熔断打开。
		/// </summary>
		void AlwaysFails();

		/// <summary>
		/// 仅首次调用失败、之后返回 <c>ok</c>，用于验证重置超时后的探测恢复。
		/// </summary>
		string FailFirstOnly();

		/// <summary>
		/// 每次调用都抛出异常，用于验证半开探测失败后熔断重新打开。
		/// </summary>
		void AlwaysFailsReopen();

		/// <summary>
		/// 奇数调用抛出异常、偶数调用返回 <c>ok</c>，用于验证成功会重置失败计数。
		/// </summary>
		string FailOnOddCalls();

		/// <summary>
		/// 始终异步抛出异常，用于验证熔断打开时 <see cref="Task{TResult}"/> 调用快速失败。
		/// </summary>
		Task<string> AlwaysFailsAsync();

		/// <summary>
		/// 始终异步抛出异常且无返回值，用于验证无结果 <see cref="ValueTask"/> 的快速失败。
		/// </summary>
		ValueTask AlwaysValueTask();

		/// <summary>
		/// 未标注熔断特性的普通方法，用于验证拦截器不会对其生效。
		/// </summary>
		string Plain(string value);

		/// <summary>
		/// 首次调用失败、后续调用阻塞至放行，用于验证半开状态下的单飞探测。
		/// </summary>
		void SingleFlightProbe();

		/// <summary>
		/// 首次调用失败、后续调用返回 <c>ok</c>，用于验证累计成功达到阈值后才关闭熔断。
		/// </summary>
		string HalfOpenThreshold();

		/// <summary>
		/// 始终抛出异常，配合熔断器位于重试拦截器外层的顺序，验证只统计冒泡到外层的失败。
		/// </summary>
		void BreakerOutRetryIn();

		/// <summary>
		/// 始终抛出异常，配合重试拦截器位于熔断器外层的顺序，验证首次失败即打开熔断。
		/// </summary>
		void RetryOutBreakerIn();
	}

	/// <summary>
	/// <see cref="ICircuitProbe"/> 的测试实现，记录真实调用次数并以预设的成功/失败序列驱动熔断状态流转。
	/// </summary>
	public class CircuitProbe : ICircuitProbe
	{
		/// <summary>
		/// 目标方法被真实执行的累计次数，用于断言快速失败时未进入方法体。
		/// </summary>
		public int Calls;

		/// <summary>
		/// 单飞探测进入临界区时置位的信号，供测试等待探测开始。
		/// </summary>
		public ManualResetEventSlim Entered { get; } = new(false);

		/// <summary>
		/// 放行单飞探测的信号，测试置位后探测方可继续完成。
		/// </summary>
		public ManualResetEventSlim Release { get; } = new(false);

		/// <summary>
		/// 记录一次调用并原样返回传入的字符串。
		/// </summary>
		[CircuitBreaker(MaxFailures = 3)]
		public virtual string Echo(string value)
		{
			Calls++;
			return value;
		}

		/// <summary>
		/// 记录一次调用，前两次抛出异常、第三次返回 <c>ok</c>。
		/// </summary>
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

		/// <summary>
		/// 记录一次调用后始终抛出异常。
		/// </summary>
		[CircuitBreaker(MaxFailures = 2)]
		public virtual void AlwaysFails()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 记录一次调用，仅首次失败，之后返回 <c>ok</c>。
		/// </summary>
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

		/// <summary>
		/// 记录一次调用后始终抛出异常，用于验证半开探测失败后重新打开熔断。
		/// </summary>
		[CircuitBreaker(MaxFailures = 1, ResetTimeoutSeconds = 60)]
		public virtual void AlwaysFailsReopen()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 记录一次调用，奇数调用抛出异常、偶数调用返回 <c>ok</c>。
		/// </summary>
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

		/// <summary>
		/// 记录一次调用后异步抛出异常，用于验证异步方法的熔断计数与快速失败。
		/// </summary>
		[CircuitBreaker(MaxFailures = 2)]
		public virtual async Task<string> AlwaysFailsAsync()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 记录一次调用后异步抛出异常（无返回值），用于验证无结果 <see cref="ValueTask"/> 的快速失败。
		/// </summary>
		[CircuitBreaker(MaxFailures = 2)]
		public virtual async ValueTask AlwaysValueTask()
		{
			Calls++;
			await Task.Yield();
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 记录一次调用并原样返回传入的字符串，不含熔断特性。
		/// </summary>
		public virtual string Plain(string value)
		{
			Calls++;
			return value;
		}

		/// <summary>
		/// 记录一次调用，首次失败，之后置位 <see cref="Entered"/> 并等待 <see cref="Release"/> 放行。
		/// </summary>
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

		/// <summary>
		/// 记录一次调用，首次失败、之后返回 <c>ok</c>，需要连续两次成功才关闭熔断。
		/// </summary>
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

		/// <summary>
		/// 记录一次调用后始终抛出异常，熔断器位于重试拦截器外层。
		/// </summary>
		[CircuitBreaker(MaxFailures = 2)]
		[Retry(MaxRetries = 1)]
		public virtual void BreakerOutRetryIn()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}

		/// <summary>
		/// 记录一次调用后始终抛出异常，重试拦截器位于熔断器外层。
		/// </summary>
		[Retry(MaxRetries = 2)]
		[CircuitBreaker(MaxFailures = 1)]
		public virtual void RetryOutBreakerIn()
		{
			Calls++;
			throw new InvalidOperationException("boom");
		}
	}
}