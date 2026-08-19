using System.Collections;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

[Collection("AppTests")]
public class LockInterceptorTests
{
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

	[Fact]
	public void ImplementationMethod_ShouldResolveLockAttribute()
	{
		var method = typeof(CounterService).GetMethod(nameof(CounterService.GetSyncNext));
		var attribute = method!.GetCustomAttribute<LockAttribute>();

		Assert.NotNull(attribute);
	}

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
