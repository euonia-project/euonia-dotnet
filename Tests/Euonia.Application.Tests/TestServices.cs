using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

public interface ICounterService
{
	Task<int> GetNextAsync();
	Task<int> GetBatchedAsync(int n);
	int GetSyncNext();
}

/// <summary>
/// 锁特性标注在【实现类】方法上（修复 1 的关键场景：接口代理下必须能命中）。
/// </summary>
public class CounterService : BaseApplicationService, ICounterService
{
	public static int Concurrent;
	public static int MaxConcurrent;

	private int _counter;

	[SemaphoreLock("test:counter", Timeout = 5000)]
	public async Task<int> GetNextAsync()
	{
		var current = Interlocked.Increment(ref Concurrent);
		var max = Volatile.Read(ref MaxConcurrent);
		while (current > max && Interlocked.CompareExchange(ref MaxConcurrent, current, max) != max)
		{
			max = Volatile.Read(ref MaxConcurrent);
		}

		await Task.Delay(50);
		Interlocked.Decrement(ref Concurrent);
		return ++_counter;
	}

	[SemaphoreLock("test:batch", Timeout = 5000)]
	public async Task<int> GetBatchedAsync(int n)
	{
		await Task.Delay(30);
		return n;
	}

	[SemaphoreLock("test:sync", Timeout = 5000)]
	public int GetSyncNext()
	{
		return ++_counter;
	}
}

public interface IOrderService
{
	void SetFlag(string value);
}

public interface IAuditService
{
	string GetFlag();
}

/// <summary>
/// 同一实现类的多个接口视图，用于验证作用域内共享目标实例。
/// </summary>
public class OrderAuditService : BaseApplicationService, IOrderService, IAuditService
{
	public static int Created;

	public OrderAuditService()
	{
		Interlocked.Increment(ref Created);
	}

	public string Flag;

	public void SetFlag(string value) => Flag = value;

	public string GetFlag() => Flag;
}

internal static class TestContainer
{
	public static ServiceProvider CreateProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
		services.AddSingleton<ProxyGenerator>();
		services.AddTransient<ILazyServiceProvider, LazyServiceProvider>();
		services.AddTransient<IInterceptor, LoggingInterceptor>();
		services.AddTransient<IInterceptor, AuthorizationInterceptor>();
		services.AddTransient<IInterceptor, ValidationInterceptor>();
		services.AddTransient<IInterceptor, TracingInterceptor>();
		services.AddTransient<IInterceptor, LockInterceptor>();
		services.AddApplicationService(typeof(CounterService).Assembly);
		return services.BuildServiceProvider();
	}
}
