using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 工作单元测试服务契约，覆盖异步、同步、禁用与 <see cref="ValueTask"/> 形态。
/// </summary>
public interface IUowTestService
{
	/// <summary>
	/// 异步返回 <c>42</c>，用于验证工作单元在方法体完成后提交。
	/// </summary>
	/// <returns>固定值 <c>42</c>。</returns>
	Task<int> WorkAsync();
	/// <summary>
	/// 异步抛出异常，用于验证失败时不提交工作单元。
	/// </summary>
	/// <returns>不会正常完成。</returns>
	/// <exception cref="InvalidOperationException">方法体总是抛出。</exception>
	Task<int> FailAsync();
	/// <summary>
	/// 同步返回 <c>1</c>，用于验证同步方法的提交。
	/// </summary>
	/// <returns>固定值 <c>1</c>。</returns>
	int WorkSync();
	/// <summary>
	/// 禁用工作单元的异步方法，返回 <c>2</c>。
	/// </summary>
	/// <returns>固定值 <c>2</c>。</returns>
	Task<int> NoUowAsync();
	/// <summary>
	/// 返回 <see cref="ValueTask{TResult}"/> 并返回 <c>43</c>，用于验证 ValueTask 的提交时机。
	/// </summary>
	/// <returns>固定值 <c>43</c>。</returns>
	ValueTask<int> WorkValueTaskAsync();
	/// <summary>
	/// 无返回值的 <see cref="ValueTask"/> 方法，用于验证其提交时机。
	/// </summary>
	/// <returns>完成后结束的 ValueTask。</returns>
	ValueTask WorkValueTaskVoidAsync();
}

/// <summary>
/// 关键验证点：[UnitOfWork] 特性标注在实现类方法上（接口代理下必须能命中）。
/// </summary>
public class UowTestService : BaseApplicationService, IUowTestService
{
	/// <summary>
	/// 延迟 50 毫秒后返回 <c>42</c>。
	/// </summary>
	/// <returns>固定值 <c>42</c>。</returns>
	[UnitOfWork]
	public async Task<int> WorkAsync()
	{
		await Task.Delay(50, TestContext.Current.CancellationToken);
		return 42;
	}

	/// <summary>
	/// 延迟 10 毫秒后抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <returns>不会正常完成。</returns>
	/// <exception cref="InvalidOperationException">方法体总是抛出。</exception>
	[UnitOfWork]
	public async Task<int> FailAsync()
	{
		await Task.Delay(10, TestContext.Current.CancellationToken);
		throw new InvalidOperationException("boom");
	}

	/// <summary>
	/// 同步返回 <c>1</c>。
	/// </summary>
	/// <returns>固定值 <c>1</c>。</returns>
	[UnitOfWork]
	public int WorkSync()
	{
		return 1;
	}

	/// <summary>
	/// 延迟 10 毫秒返回 <c>2</c>，且不开启工作单元。
	/// </summary>
	/// <returns>固定值 <c>2</c>。</returns>
	[UnitOfWork(IsDisabled = true)]
	public async Task<int> NoUowAsync()
	{
		await Task.Delay(10, TestContext.Current.CancellationToken);
		return 2;
	}

	/// <summary>
	/// 获取或设置 <see cref="WorkValueTaskAsync"/> 方法体是否已执行完毕，供提交回调断言时序。
	/// </summary>
	public static bool ValueTaskBodyCompleted;

	/// <summary>
	/// 在方法体开始与结束处更新完成标志，延迟 50 毫秒后返回 <c>43</c>。
	/// </summary>
	/// <returns>固定值 <c>43</c>。</returns>
	[UnitOfWork]
	public async ValueTask<int> WorkValueTaskAsync()
	{
		ValueTaskBodyCompleted = false;
		await Task.Delay(50, TestContext.Current.CancellationToken);
		ValueTaskBodyCompleted = true;
		return 43;
	}

	/// <summary>
	/// 延迟 10 毫秒的无返回值 ValueTask 方法。
	/// </summary>
	/// <returns>完成后结束的 ValueTask。</returns>
	[UnitOfWork]
	public async ValueTask WorkValueTaskVoidAsync()
	{
		await Task.Delay(10, TestContext.Current.CancellationToken);
	}
}

/// <summary>
/// 验证 <see cref="UnitOfWorkInterceptor"/> 在异步、同步与 ValueTask 方法上的提交时机，以及异常与禁用场景。
/// </summary>
[Collection("AppTests")]
public class UnitOfWorkInterceptorTests
{
	/// <summary>
	/// 工作单元拦截器测试的装配环境：预置工作单元管理器与工作单元的模拟对象，并构建可解析被测服务的容器。
	/// </summary>
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
			services.AddApplicationService(typeof(UowTestService).Assembly, ServiceLifetime.Scoped);

			Provider = services.BuildServiceProvider();
		}
	}

	/// <summary>
	/// 异步目标方法执行完成后才应调用 <c>CompleteAsync</c>，返回 <c>42</c> 且提交回调已触发；旧实现会在 <c>Proceed</c> 后立即提交。
	/// </summary>
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

	/// <summary>
	/// 目标方法耗时约 50 毫秒时，提交仍应发生在方法结束之后。
	/// </summary>
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

	/// <summary>
	/// <see cref="ValueTask{TResult}"/> 方法体执行完成后才应提交，返回 <c>43</c>。
	/// </summary>
	[Fact]
	public async Task ValueTaskUow_ShouldCompleteAfterTargetMethodFinishes()
	{
		// 回归测试：ValueTask/ValueTask<T> 曾被当作同步方法，工作单元在方法体完成前就被 Complete。
		var harness = new Harness();
		var completedAfterBody = false;
		harness.Uow.Setup(u => u.CompleteAsync(It.IsAny<CancellationToken>()))
			.Callback(() => completedAfterBody = UowTestService.ValueTaskBodyCompleted)
			.Returns(Task.CompletedTask);

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		var result = await svc.WorkValueTaskAsync();

		Assert.Equal(43, result);
		Assert.True(completedAfterBody, "CompleteAsync 必须在 ValueTask 方法体执行完成后调用");
	}

	/// <summary>
	/// 无返回值 <see cref="ValueTask"/> 方法完成后，<c>CompleteAsync</c> 应恰好调用一次。
	/// </summary>
	[Fact]
	public async Task ValueTaskVoidUow_ShouldCompleteAfterTargetMethodFinishes()
	{
		var harness = new Harness();

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		await svc.WorkValueTaskVoidAsync();

		harness.Uow.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// 目标方法抛出异常时不应调用 <c>CompleteAsync</c>，异常应原样向上传播。
	/// </summary>
	[Fact]
	public async Task Exception_ShouldNotComplete()
	{
		var harness = new Harness();

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		await Assert.ThrowsAsync<InvalidOperationException>(() => svc.FailAsync());

		harness.Uow.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	/// <summary>
	/// 工作单元被禁用（<c>IsDisabled = true</c>）时不应调用 <see cref="IUnitOfWorkManager.Begin(bool, bool)"/>。
	/// </summary>
	[Fact]
	public async Task DisabledUow_ShouldNotBegin()
	{
		var harness = new Harness();

		using var scope = harness.Provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IUowTestService>();

		await svc.NoUowAsync();

		harness.Manager.Verify(m => m.Begin(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
	}

	/// <summary>
	/// 同步方法执行后应调用一次 <c>CompleteAsync</c>，且返回 <c>1</c>。
	/// </summary>
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
