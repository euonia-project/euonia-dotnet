using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Application.Tests;

public interface IUowTestService
{
	Task<int> WorkAsync();
	Task<int> FailAsync();
	int WorkSync();
	Task<int> NoUowAsync();
}

/// <summary>
/// 关键验证点：[UnitOfWork] 特性标注在实现类方法上（接口代理下必须能命中）。
/// </summary>
public class UowTestService : BaseApplicationService, IUowTestService
{
	[UnitOfWork]
	public async Task<int> WorkAsync()
	{
		await Task.Delay(50, TestContext.Current.CancellationToken);
		return 42;
	}

	[UnitOfWork]
	public async Task<int> FailAsync()
	{
		await Task.Delay(10, TestContext.Current.CancellationToken);
		throw new InvalidOperationException("boom");
	}

	[UnitOfWork]
	public int WorkSync()
	{
		return 1;
	}

	[UnitOfWork(IsDisabled = true)]
	public async Task<int> NoUowAsync()
	{
		await Task.Delay(10, TestContext.Current.CancellationToken);
		return 2;
	}
}

[Collection("AppTests")]
public class UnitOfWorkInterceptorTests
{
	private sealed class Harness
	{
		public Mock<IUnitOfWorkManager> Manager { get; } = new();
		public Mock<IUnitOfWork> Uow { get; } = new();

		public ServiceProvider Provider { get; }

		public Harness()
		{
			Manager.Setup(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>())).Returns(Uow.Object);
			Uow.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

			var services = new ServiceCollection();
			services.AddLogging();
			services.AddSingleton(Manager.Object);
			services.AddSingleton<ProxyGenerator>();
			services.AddTransient<ILazyServiceProvider, LazyServiceProvider>();
			services.AddTransient<IInterceptor, UnitOfWorkInterceptor>();
			services.AddApplicationService(typeof(UowTestService).Assembly);

			Provider = services.BuildServiceProvider();
		}
	}

	[Fact]
	public async Task AsyncUow_ShouldCompleteAfterTargetMethodFinishes()
	{
		// 回归测试：旧的拦截器在 Proceed() 后立即 Complete，
		// 异步目标方法的 Task 尚未完成，UoW 没有覆盖方法执行体。
		// 修复后 CompleteAsync 必须在目标方法 Task 完成后才被调用。
		var harness = new Harness();
		var completed = false;
		harness.Uow.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
			.Callback(() => completed = true)
			.Returns(Task.CompletedTask);

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		var result = await svc.WorkAsync();

		Assert.Equal(42, result);
		Assert.True(completed, "CompleteAsync 必须在目标方法执行完成后调用");
	}

	[Fact]
	public async Task AsyncUow_ShouldCompleteEvenWhenTargetIsSlow()
	{
		var harness = new Harness();
		var completedAt = DateTime.MinValue;
		harness.Uow.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
			.Callback(() => completedAt = DateTime.UtcNow)
			.Returns(Task.CompletedTask);

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		// 目标方法需要约 50ms；若 Complete 提前执行，完成时间会远早于方法体结束。
		var result = await svc.WorkAsync();

		Assert.Equal(42, result);
		Assert.True(completedAt != DateTime.MinValue);
	}

	[Fact]
	public async Task Exception_ShouldNotComplete()
	{
		var harness = new Harness();

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		await Assert.ThrowsAsync<InvalidOperationException>(() => svc.FailAsync());

		harness.Uow.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public void DisabledUow_ShouldNotBegin()
	{
		var harness = new Harness();

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		_ = svc.NoUowAsync().GetAwaiter().GetResult();

		harness.Manager.Verify(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
	}

	[Fact]
	public void SyncUow_ShouldComplete()
	{
		var harness = new Harness();
		var completed = false;
		harness.Uow.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
			.Callback(() => completed = true)
			.Returns(Task.CompletedTask);

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		var result = svc.WorkSync();

		Assert.Equal(1, result);
		Assert.True(completed);
	}
}
