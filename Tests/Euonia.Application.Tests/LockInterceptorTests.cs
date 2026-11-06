using System.Collections;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 锁拦截器的集成测试，验证实现方法上的锁特性解析、并发调用串行化、异步续延不重复进入拦截器链，
/// 以及信号量锁表的回收与互斥体保持。
/// </summary>
[Collection("AppTests")]
public class LockInterceptorTests
{
	/// <summary>
	/// 多个并发调用命中同一把实现方法锁时应被串行化，最大并发保持为 1 且结束后无残留并发。
	/// </summary>
	[Fact]
	public async Task ImplementationMethodLock_ShouldSerializeConcurrentCalls()
	{
		CounterService.MaxConcurrent = 0;
		CounterService.Concurrent = 0;

		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => svc.GetNextAsync()));

		Assert.Equal(1, CounterService.MaxConcurrent);
		Assert.Equal(0, CounterService.Concurrent);
	}

	/// <summary>
	/// 异步续延中继续执行不应重新进入拦截器链：10 个并发同锁调用应全部成功返回且不出现自锁超时。
	/// </summary>
	[Fact]
	public async Task AsyncProceed_ShouldNotReenterInterceptorChain()
	{
		// 回归测试：Castle 的拦截器索引在链展开后复位，
		// 若在异步续延中直接调用 invocation.Proceed() 会重新派发拦截器链，
		// 导致持有锁的方法再次进入 LockInterceptor（自锁死等，超时抛出）。
		// 修复后通过 CaptureProceedInfo 在链展开前捕获继续执行信息。
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		// 10 个任务并发抢同一把锁；若自锁 bug 存在，这里会抛出 TimeoutException。
		var tasks = Enumerable.Range(1, 10).Select(i => svc.GetBatchedAsync(i)).ToArray();
		var results = await Task.WhenAll(tasks);

		Assert.Equal(Enumerable.Range(1, 10), results.OrderBy(x => x));
	}

	/// <summary>
	/// 锁释放后，信号量锁表应回收不再使用的条目，避免内存持续增长。
	/// </summary>
	[Fact]
	public async Task SemaphoreLockStore_ShouldEvictUnusedEntries()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		await svc.GetBatchedAsync(42);
		// 等待匿名释放回调执行完毕。
		await Task.Delay(100, TestContext.Current.CancellationToken);

		Assert.Equal(0, CountStoreEntries());
	}

	/// <summary>
	/// 锁表条目被回收后再次创建仍应复用同一互斥体，后续并发调用依旧保持串行。
	/// </summary>
	[Fact]
	public async Task SemaphoreLockStore_ShouldKeepMutexAfterEviction()
	{
		CounterService.MaxConcurrent = 0;

		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		await svc.GetBatchedAsync(1); // 触发一次完整的创建-释放-回收
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => svc.GetNextAsync()));

		Assert.Equal(1, CounterService.MaxConcurrent);
	}

	/// <summary>
	/// 实现方法上应能通过反射解析到 <see cref="LockAttribute"/>，确保拦截器可据此加锁。
	/// </summary>
	[Fact]
	public void ImplementationMethod_ShouldResolveLockAttribute()
	{
		var method = typeof(CounterService).GetMethod(nameof(CounterService.GetSyncNext));
		var attribute = method!.GetCustomAttribute<LockAttribute>();

		Assert.NotNull(attribute);
	}

	/// <summary>
	/// 同步调用路径应正常通过锁拦截器执行，并返回首次计数结果 <c>1</c>。
	/// </summary>
	[Fact]
	public void SyncPath_ShouldWork()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		var result = svc.GetSyncNext();

		Assert.Equal(1, result);
	}

	private static int CountStoreEntries()
	{
		var type = typeof(BaseApplicationService).Assembly
			.GetType("Nerosoft.Euonia.Application.SemaphoreLockStore");
		var field = type!.GetField("_locks", BindingFlags.Static | BindingFlags.NonPublic);
		return ((ICollection)field!.GetValue(null)!).Count;
	}
}
