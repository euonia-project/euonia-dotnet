using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Application;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 有输入用例使用的问候请求，携带待问候的名称。
/// </summary>
/// <param name="Name">待问候的名称。</param>
public record GreetingInput(string Name);

/// <summary>
/// 问候用例返回的结果。
/// </summary>
/// <param name="Text">问候文本。</param>
public record GreetingOutput(string Text);

/// <summary>
/// 有输入有输出的用例。
/// </summary>
public class GreetingUseCase : IUseCase<GreetingInput, GreetingOutput>
{
	/// <summary>
	/// 根据输入名称生成问候文本。
	/// </summary>
	/// <param name="input">问候请求，包含待问候的名称。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>包含问候文本的输出对象。</returns>
	public Task<GreetingOutput> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new GreetingOutput($"Hello {input.Name}"));
	}
}

/// <summary>
/// 无输出（只执行）的用例。
/// </summary>
public class FlagUseCase : INonOutputUseCase<GreetingInput>
{
	/// <summary>
	/// 已执行次数的累计计数，供测试断言用例是否被调用。
	/// </summary>
	public static int Calls;

	/// <summary>
	/// 累加调用计数并完成执行，不产生输出。
	/// </summary>
	/// <param name="input">问候输入，本用例仅用于演示无输出场景。</param>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示执行完成的任务。</returns>
	public Task ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
	{
		Interlocked.Increment(ref Calls);
		return Task.CompletedTask;
	}
}

/// <summary>
/// 无输入、有输出的用例。
/// </summary>
public class ClockUseCase : INonInputUseCase<GreetingOutput>
{
	/// <summary>
	/// 无需输入即可返回内容为 <c>now</c> 的固定输出。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>包含固定文本的输出对象。</returns>
	public Task<GreetingOutput> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new GreetingOutput("now"));
	}
}

/// <summary>
/// 无输入无输出的用例。
/// </summary>
public class NoopUseCase : IParameterlessUseCase
{
	/// <summary>
	/// 已执行次数的累计计数，供测试断言用例是否被调用。
	/// </summary>
	public static int Calls;

	/// <summary>
	/// 累加调用计数并完成执行，不接收输入也不产生输出。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示执行完成的任务。</returns>
	public Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Interlocked.Increment(ref Calls);
		return Task.CompletedTask;
	}
}

/// <summary>
/// 覆盖各类用例接口的适配与执行行为，包括有输入/输出、无输出、无输入及无输入输出四种形态。
/// </summary>
public class UseCaseTests
{
	/// <summary>
	/// 有输入输出的用例应基于输入名称返回问候文本。
	/// </summary>
	[Fact]
	public async Task TypedUseCase_ShouldReturnOutput()
	{
		var useCase = new GreetingUseCase();

		var output = await useCase.ExecuteAsync(new GreetingInput("Alice"), TestContext.Current.CancellationToken);

		Assert.Equal("Hello Alice", output.Text);
	}

	/// <summary>
	/// 通过非泛型用例入口调用应路由到强类型实现并返回强类型输出。
	/// </summary>
	[Fact]
	public async Task NonGenericEntry_ShouldRouteToTypedUseCase()
	{
		var useCase = new GreetingUseCase();
		var nonGeneric = (IUseCase)useCase;

		var result = await nonGeneric.ExecuteAsync(new GreetingInput("Bob"), TestContext.Current.CancellationToken);

		Assert.IsType<GreetingOutput>(result);
		Assert.Equal("Hello Bob", ((GreetingOutput)result).Text);
	}

	/// <summary>
	/// 无输出用例应执行一次，且非泛型入口返回 <see cref="EmptyUseCaseOutput"/>。
	/// </summary>
	[Fact]
	public async Task NonOutputUseCase_ShouldReturnEmptyOutput()
	{
		FlagUseCase.Calls = 0;
		var useCase = new FlagUseCase();

		await useCase.ExecuteAsync(new GreetingInput("Eve"), TestContext.Current.CancellationToken);

		Assert.Equal(1, FlagUseCase.Calls);

		// 非泛型入口返回 EmptyUseCaseOutput（INonOutputUseCase 适配）。
		var nonGeneric = (IUseCase)useCase;
		var result = await nonGeneric.ExecuteAsync(new GreetingInput("Eve"), TestContext.Current.CancellationToken);
		Assert.IsType<EmptyUseCaseOutput>(result);
	}

	/// <summary>
	/// 无输入用例无需输入即可执行并返回固定输出。
	/// </summary>
	[Fact]
	public async Task NonInputUseCase_ShouldIgnoreEmptyInput()
	{
		var useCase = new ClockUseCase();

		var output = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("now", output.Text);
	}

	/// <summary>
	/// 无输入无输出用例应执行一次，且非泛型入口返回 <see cref="EmptyUseCaseOutput"/>。
	/// </summary>
	[Fact]
	public async Task ParameterlessUseCase_ShouldReturnEmptyInputAndOutput()
	{
		NoopUseCase.Calls = 0;
		var useCase = new NoopUseCase();

		await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, NoopUseCase.Calls);

		// 通过非泛型入口调用应能得到 EmptyUseCaseOutput。
		var nonGeneric = (IUseCase)useCase;
		var result = await nonGeneric.ExecuteAsync(new EmptyUseCaseInput(), TestContext.Current.CancellationToken);
		Assert.IsType<EmptyUseCaseOutput>(result);
	}
}

/// <summary>
/// 针对 <see cref="DefaultUseCasePresenter{T}"/> 输出设置、错误分发、取消分发与释放行为的测试。
/// </summary>
[Collection("AppTests")]
public class DefaultUseCasePresenterTests
{
	/// <summary>
	/// 调用 <c>Ok</c> 应设置输出并触发 <c>OnSucceed</c> 回调。
	/// </summary>
	[Fact]
	public void Ok_ShouldSetOutputAndRaiseOnSucceed()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnSucceed += (_, output) =>
		{
			raised = output == "result";
		};

		presenter.Ok("result");

		Assert.True(raised);
		Assert.Equal("result", presenter.Output);
	}

	/// <summary>
	/// 传入普通异常应触发 <c>OnFailed</c> 回调。
	/// </summary>
	[Fact]
	public void Error_OrdinaryException_ShouldRaiseOnFailed()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnFailed += (_, _) => raised = true;

		presenter.Error(new InvalidOperationException("boom"));

		Assert.True(raised);
	}

	/// <summary>
	/// 传入取消异常应触发 <c>OnCanceled</c> 回调。
	/// </summary>
	[Fact]
	public void Error_CancellationException_ShouldRaiseOnCanceled()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnCanceled += (_, _) => raised = true;

		presenter.Error(new OperationCanceledException());

		Assert.True(raised);
	}

	/// <summary>
	/// 释放后应解绑全部事件处理器，后续调用 <c>Ok</c> 不再触发回调。
	/// </summary>
	[Fact]
	public void Dispose_ShouldDetachAllEventHandlers()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnSucceed += (_, _) => raised = true;

		presenter.Dispose();
		presenter.Ok("result");

		Assert.False(raised);
	}

	/// <summary>
	/// 无订阅者时调用 <c>Ok</c> 与 <c>Error</c> 均不应抛出异常。
	/// </summary>
	[Fact]
	public void Ok_WithoutSubscribers_ShouldNotThrow()
	{
		var presenter = new DefaultUseCasePresenter<string>();

		presenter.Ok("result");
		presenter.Error(new InvalidOperationException());
		presenter.Error(new OperationCanceledException());
	}
}

/// <summary>
/// 针对 <see cref="UseCaseExecutor"/> 分发成功、失败、取消及容器解析行为的测试。
/// </summary>
[Collection("AppTests")]
public class UseCaseExecutorTests
{
	private static UseCaseExecutor CreateExecutor(params ServiceDescriptor[] descriptors)
	{
		var services = new ServiceCollection();
		foreach (var descriptor in descriptors)
		{
			((IServiceCollection)services).Add(descriptor);
		}

		return new UseCaseExecutor(services.BuildServiceProvider());
	}

	/// <summary>
	/// 强类型用例成功执行后应把输出写入呈现器。
	/// </summary>
	[Fact]
	public async Task TypedUseCase_Success_ShouldDistributeToPresenter()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync(new GreetingUseCase(), new GreetingInput("Alice"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("Hello Alice", presenter.Output.Text);
	}

	/// <summary>
	/// 用例抛出异常时应通过呈现器触发失败回调。
	/// </summary>
	[Fact]
	public async Task TypedUseCase_Throws_ShouldDistributeError()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<string>();
		var failed = false;
		presenter.OnFailed += (_, _) => failed = true;

		await executor.ExecuteAsync(new ThrowingUseCase(), new GreetingInput("x"), presenter, TestContext.Current.CancellationToken);

		Assert.True(failed);
	}

	/// <summary>
	/// 无输出用例应执行一次并分发 <see cref="EmptyUseCaseOutput.Instance"/>。
	/// </summary>
	[Fact]
	public async Task NonOutputUseCase_ShouldDistributeEmptyOutput()
	{
		FlagUseCase.Calls = 0;
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<EmptyUseCaseOutput>();

		await executor.ExecuteAsync(new FlagUseCase(), new GreetingInput("Eve"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal(1, FlagUseCase.Calls);
		Assert.Same(EmptyUseCaseOutput.Instance, presenter.Output);
	}

	/// <summary>
	/// 无输入用例应执行并把输出写入呈现器。
	/// </summary>
	[Fact]
	public async Task NonInputUseCase_ShouldDistributeOutput()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync(new ClockUseCase(), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("now", presenter.Output.Text);
	}

	/// <summary>
	/// 无输入无输出用例应执行一次并分发空输出单例。
	/// </summary>
	[Fact]
	public async Task ParameterlessUseCase_ShouldDistributeEmptyOutput()
	{
		NoopUseCase.Calls = 0;
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<EmptyUseCaseOutput>();

		await executor.ExecuteAsync(new NoopUseCase(), presenter, TestContext.Current.CancellationToken);

		Assert.Equal(1, NoopUseCase.Calls);
		Assert.Same(EmptyUseCaseOutput.Instance, presenter.Output);
	}

	/// <summary>
	/// 用例抛出取消异常时应通过呈现器触发取消回调。
	/// </summary>
	[Fact]
	public async Task UseCase_Canceled_ShouldDistributeCanceledToPresenter()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<string>();
		var canceled = false;
		presenter.OnCanceled += (_, _) => canceled = true;

		await executor.ExecuteAsync(new CancelingUseCase(), new GreetingInput("x"), presenter, TestContext.Current.CancellationToken);

		Assert.True(canceled);
	}

	/// <summary>
	/// 容器应能解析 <see cref="IUseCaseExecutor"/> 并返回 <see cref="UseCaseExecutor"/> 实例。
	/// </summary>
	[Fact]
	public async Task Executor_FromContainer_ShouldBeResolvable()
	{
		using var provider = TestContainer.CreateProvider();

		var executor = provider.GetRequiredService<Nerosoft.Euonia.Application.IUseCaseExecutor>();

		Assert.IsType<Nerosoft.Euonia.Application.UseCaseExecutor>(executor);
	}

	/// <summary>
	/// 从容器解析的强类型用例应执行并把结果写入呈现器。
	/// </summary>
	[Fact]
	public async Task ContainerTypedUseCase_ResolvedFromContainer_ShouldExecute()
	{
		var executor = CreateExecutor(ServiceDescriptor.Singleton<GreetingUseCase, GreetingUseCase>());
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync<GreetingUseCase, GreetingInput, GreetingOutput>(new GreetingInput("Bob"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("Hello Bob", presenter.Output.Text);
	}

	/// <summary>
	/// 未注册的用例类型在解析时应失败并通过呈现器触发失败回调。
	/// </summary>
	[Fact]
	public async Task ContainerTypedUseCase_Unregistered_ShouldDistributeError()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<string>();
		var failed = false;
		presenter.OnFailed += (_, _) => failed = true;

		await executor.ExecuteAsync<ThrowingUseCase, GreetingInput, string>(new GreetingInput("x"), presenter, TestContext.Current.CancellationToken);

		Assert.True(failed);
	}

	/// <summary>
	/// 从容器解析的无输出用例应执行一次并分发空输出单例。
	/// </summary>
	[Fact]
	public async Task ContainerNonOutputUseCase_ResolvedFromContainer_ShouldExecute()
	{
		FlagUseCase.Calls = 0;
		var executor = CreateExecutor(ServiceDescriptor.Singleton<FlagUseCase, FlagUseCase>());
		var presenter = new DefaultUseCasePresenter<EmptyUseCaseOutput>();

		await executor.ExecuteAsync<FlagUseCase, GreetingInput>(new GreetingInput("Eve"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal(1, FlagUseCase.Calls);
		Assert.Same(EmptyUseCaseOutput.Instance, presenter.Output);
	}

	/// <summary>
	/// 从容器解析的无输入用例应执行并把输出写入呈现器。
	/// </summary>
	[Fact]
	public async Task ContainerNonInputUseCase_ResolvedFromContainer_ShouldExecute()
	{
		var executor = CreateExecutor(ServiceDescriptor.Singleton<ClockUseCase, ClockUseCase>());
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync<ClockUseCase, GreetingOutput>(presenter, TestContext.Current.CancellationToken);

		Assert.Equal("now", presenter.Output.Text);
	}

	/// <summary>
	/// 从容器解析的无输入无输出用例应执行一次并分发空输出单例。
	/// </summary>
	[Fact]
	public async Task ContainerParameterlessUseCase_ResolvedFromContainer_ShouldExecute()
	{
		NoopUseCase.Calls = 0;
		var executor = CreateExecutor(ServiceDescriptor.Singleton<NoopUseCase, NoopUseCase>());
		var presenter = new DefaultUseCasePresenter<EmptyUseCaseOutput>();

		await executor.ExecuteAsync<NoopUseCase>(presenter, TestContext.Current.CancellationToken);

		Assert.Equal(1, NoopUseCase.Calls);
		Assert.Same(EmptyUseCaseOutput.Instance, presenter.Output);
	}

	/// <summary>
	/// 作用域用例每次执行都应获得新实例，两次执行应创建两个实例。
	/// </summary>
	[Fact]
	public async Task ScopedUseCase_ResolvedFromContainer_ShouldGetNewInstancePerExecution()
	{
		var executor = CreateExecutor(ServiceDescriptor.Scoped<ScopedCountingUseCase, ScopedCountingUseCase>());
		var presenter = new DefaultUseCasePresenter<string>();

		await executor.ExecuteAsync<ScopedCountingUseCase, GreetingInput, string>(new GreetingInput("a"), presenter, TestContext.Current.CancellationToken);
		await executor.ExecuteAsync<ScopedCountingUseCase, GreetingInput, string>(new GreetingInput("b"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal(2, ScopedCountingUseCase.InstancesCreated);
	}

	/// <summary>
	/// 用例与其作用域依赖应来自同一作用域，输出为 <c>scoped</c>。
	/// </summary>
	[Fact]
	public async Task ScopedDependency_InjectsUseCase_ShouldResolveSameScope()
	{
		var executor = CreateExecutor(
			ServiceDescriptor.Scoped<ScopedCounter, ScopedCounter>(),
			ServiceDescriptor.Scoped<ScopedDependencyUseCase, ScopedDependencyUseCase>());
		var presenter = new DefaultUseCasePresenter<string>();

		await executor.ExecuteAsync<ScopedDependencyUseCase, GreetingInput, string>(new GreetingInput("a"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("scoped", presenter.Output);
	}

	/// <summary>
	/// 作用域内的可释放依赖应在用例执行期间保持存活，输出为 <c>alive</c> 而非 <c>disposed</c>。
	/// </summary>
	[Fact]
	public async Task ScopedDisposableDependency_ShouldRemainAliveDuringUseCaseExecution()
	{
		// 回归测试：作用域曾在 GetRequiredService 返回后立即释放，
		// 导致用例执行时依赖（如 DbContext）已经被 Dispose。
		var executor = CreateExecutor(
			ServiceDescriptor.Scoped<DisposableDependency, DisposableDependency>(),
			ServiceDescriptor.Scoped<DisposableDependencyUseCase, DisposableDependencyUseCase>());
		var presenter = new DefaultUseCasePresenter<string>();

		await executor.ExecuteAsync<DisposableDependencyUseCase, GreetingInput, string>(new GreetingInput("a"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("alive", presenter.Output);
	}

	/// <summary>
	/// 执行时始终抛出 <see cref="InvalidOperationException"/> 的用例，用于验证执行器对业务异常的传播方式。
	/// </summary>
	private sealed class ThrowingUseCase : IUseCase<GreetingInput, string>
	{
		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("boom");
	}

	/// <summary>
	/// 执行时抛出 <see cref="OperationCanceledException"/> 的用例，用于验证执行器能区分取消与普通失败。
	/// </summary>
	private sealed class CancelingUseCase : IUseCase<GreetingInput, string>
	{
		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> throw new OperationCanceledException();
	}

	/// <summary>
	/// 在构造时累加实例计数的用例，用于验证每次执行都从新的作用域解析实例。
	/// </summary>
	private sealed class ScopedCountingUseCase : IUseCase<GreetingInput, string>
	{
		public static int InstancesCreated;

		public ScopedCountingUseCase()
		{
			Interlocked.Increment(ref InstancesCreated);
		}

		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> Task.FromResult(input.Name);
	}

	/// <summary>
	/// 无状态的作用域依赖，用于验证用例能够从执行作用域中解析并注入依赖。
	/// </summary>
	private sealed class ScopedCounter
	{
	}

	/// <summary>
	/// 记录自身是否已被释放的可释放依赖，用于验证用例执行期间作用域不会提前释放它。
	/// </summary>
	private sealed class DisposableDependency : IDisposable
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}

	/// <summary>
	/// 依赖 <see cref="DisposableDependency"/> 的用例，按依赖是否已被释放返回 <c>alive</c> 或 <c>disposed</c>。
	/// </summary>
	private sealed class DisposableDependencyUseCase : IUseCase<GreetingInput, string>
	{
		private readonly DisposableDependency _dependency;

		public DisposableDependencyUseCase(DisposableDependency dependency)
		{
			_dependency = dependency;
		}

		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> Task.FromResult(_dependency.IsDisposed ? "disposed" : "alive");
	}

	/// <summary>
	/// 依赖 <see cref="ScopedCounter"/> 的用例，用于验证作用域依赖能被正常注入，成功时返回 <c>scoped</c>。
	/// </summary>
	private sealed class ScopedDependencyUseCase : IUseCase<GreetingInput, string>
	{
		private readonly ScopedCounter _counter;

		public ScopedDependencyUseCase(ScopedCounter counter)
		{
			_counter = counter;
		}

		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> Task.FromResult(_counter is null ? "none" : "scoped");
	}
}