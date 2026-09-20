using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Application;

namespace Nerosoft.Euonia.Application.Tests;

public record GreetingInput(string Name);

public record GreetingOutput(string Text);

/// <summary>
/// 有输入有输出的用例。
/// </summary>
public class GreetingUseCase : IUseCase<GreetingInput, GreetingOutput>
{
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
	public static int Calls;

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
	public static int Calls;

	public Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Interlocked.Increment(ref Calls);
		return Task.CompletedTask;
	}
}

public class UseCaseTests
{
	[Fact]
	public async Task TypedUseCase_ShouldReturnOutput()
	{
		var useCase = new GreetingUseCase();

		var output = await useCase.ExecuteAsync(new GreetingInput("Alice"), TestContext.Current.CancellationToken);

		Assert.Equal("Hello Alice", output.Text);
	}

	[Fact]
	public async Task NonGenericEntry_ShouldRouteToTypedUseCase()
	{
		var useCase = new GreetingUseCase();
		var nonGeneric = (IUseCase)useCase;

		var result = await nonGeneric.ExecuteAsync(new GreetingInput("Bob"), TestContext.Current.CancellationToken);

		Assert.IsType<GreetingOutput>(result);
		Assert.Equal("Hello Bob", ((GreetingOutput)result).Text);
	}

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

	[Fact]
	public async Task NonInputUseCase_ShouldIgnoreEmptyInput()
	{
		var useCase = new ClockUseCase();

		var output = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal("now", output.Text);
	}

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

[Collection("AppTests")]
public class DefaultUseCasePresenterTests
{
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

	[Fact]
	public void Error_OrdinaryException_ShouldRaiseOnFailed()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnFailed += (_, _) => raised = true;

		presenter.Error(new InvalidOperationException("boom"));

		Assert.True(raised);
	}

	[Fact]
	public void Error_CancellationException_ShouldRaiseOnCanceled()
	{
		var presenter = new DefaultUseCasePresenter<string>();
		var raised = false;
		presenter.OnCanceled += (_, _) => raised = true;

		presenter.Error(new OperationCanceledException());

		Assert.True(raised);
	}

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

	[Fact]
	public void Ok_WithoutSubscribers_ShouldNotThrow()
	{
		var presenter = new DefaultUseCasePresenter<string>();

		presenter.Ok("result");
		presenter.Error(new InvalidOperationException());
		presenter.Error(new OperationCanceledException());
	}
}

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

	[Fact]
	public async Task TypedUseCase_Success_ShouldDistributeToPresenter()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync(new GreetingUseCase(), new GreetingInput("Alice"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("Hello Alice", presenter.Output.Text);
	}

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

	[Fact]
	public async Task NonInputUseCase_ShouldDistributeOutput()
	{
		var executor = CreateExecutor();
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync(new ClockUseCase(), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("now", presenter.Output.Text);
	}

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

	[Fact]
	public async Task Executor_FromContainer_ShouldBeResolvable()
	{
		using var provider = TestContainer.CreateProvider();

		var executor = provider.GetRequiredService<Nerosoft.Euonia.Application.IUseCaseExecutor>();

		Assert.IsType<Nerosoft.Euonia.Application.UseCaseExecutor>(executor);
	}

	[Fact]
	public async Task ContainerTypedUseCase_ResolvedFromContainer_ShouldExecute()
	{
		var executor = CreateExecutor(ServiceDescriptor.Singleton<GreetingUseCase, GreetingUseCase>());
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync<GreetingUseCase, GreetingInput, GreetingOutput>(new GreetingInput("Bob"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal("Hello Bob", presenter.Output.Text);
	}

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

	[Fact]
	public async Task ContainerNonInputUseCase_ResolvedFromContainer_ShouldExecute()
	{
		var executor = CreateExecutor(ServiceDescriptor.Singleton<ClockUseCase, ClockUseCase>());
		var presenter = new DefaultUseCasePresenter<GreetingOutput>();

		await executor.ExecuteAsync<ClockUseCase, GreetingOutput>(presenter, TestContext.Current.CancellationToken);

		Assert.Equal("now", presenter.Output.Text);
	}

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

	[Fact]
	public async Task ScopedUseCase_ResolvedFromContainer_ShouldGetNewInstancePerExecution()
	{
		var executor = CreateExecutor(ServiceDescriptor.Scoped<ScopedCountingUseCase, ScopedCountingUseCase>());
		var presenter = new DefaultUseCasePresenter<string>();

		await executor.ExecuteAsync<ScopedCountingUseCase, GreetingInput, string>(new GreetingInput("a"), presenter, TestContext.Current.CancellationToken);
		await executor.ExecuteAsync<ScopedCountingUseCase, GreetingInput, string>(new GreetingInput("b"), presenter, TestContext.Current.CancellationToken);

		Assert.Equal(2, ScopedCountingUseCase.InstancesCreated);
	}

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

	private sealed class ThrowingUseCase : IUseCase<GreetingInput, string>
	{
		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("boom");
	}

	private sealed class CancelingUseCase : IUseCase<GreetingInput, string>
	{
		public Task<string> ExecuteAsync(GreetingInput input, CancellationToken cancellationToken = default)
			=> throw new OperationCanceledException();
	}

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

	private sealed class ScopedCounter
	{
	}

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