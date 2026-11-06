using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 计数器服务契约，各方法均标注 <see cref="SemaphoreLockAttribute"/> 以验证并发调用的串行化。
/// </summary>
public interface ICounterService
{
	/// <summary>
	/// 异步自增并返回计数，用于验证异步方法上的锁拦截。
	/// </summary>
	/// <returns>自增后的计数值。</returns>
	Task<int> GetNextAsync();
	/// <summary>
	/// 延迟后返回传入的批量大小，用于验证带参数的异步方法。
	/// </summary>
	/// <param name="n">要返回的批量大小。</param>
	/// <returns>传入的 <paramref name="n"/>。</returns>
	Task<int> GetBatchedAsync(int n);
	/// <summary>
	/// 同步自增并返回计数，用于验证同步方法上的锁拦截。
	/// </summary>
	/// <returns>自增后的计数值。</returns>
	int GetSyncNext();
}

/// <summary>
/// 锁特性标注在【实现类】方法上（修复 1 的关键场景：接口代理下必须能命中）。
/// </summary>
public class CounterService : BaseApplicationService, ICounterService
{
	/// <summary>
	/// 获取当前并发进入 <see cref="CounterService.GetNextAsync"/> 的调用数。
	/// </summary>
	public static int Concurrent;
	/// <summary>
	/// 获取观测到的最大并发调用数，用于断言锁是否将并发串行化。
	/// </summary>
	public static int MaxConcurrent;

	private int _counter;

	/// <summary>
	/// 在锁保护下延迟 50 毫秒后自增并返回计数，同时统计并发峰值。
	/// </summary>
	/// <returns>自增后的计数值。</returns>
	[SemaphoreLock("test:counter", Timeout = 5000)]
	// virtual：实现类直接解析（类代理）时才能被拦截；接口代理不受此限制。
	public virtual async Task<int> GetNextAsync()
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

	/// <summary>
	/// 在锁保护下延迟 30 毫秒并返回 <paramref name="n"/>。
	/// </summary>
	/// <param name="n">要返回的批量大小。</param>
	/// <returns>传入的 <paramref name="n"/>。</returns>
	[SemaphoreLock("test:batch", Timeout = 5000)]
	public virtual async Task<int> GetBatchedAsync(int n)
	{
		await Task.Delay(30);
		return n;
	}

	/// <summary>
	/// 在锁保护下同步自增并返回计数。
	/// </summary>
	/// <returns>自增后的计数值。</returns>
	[SemaphoreLock("test:sync", Timeout = 5000)]
	public virtual int GetSyncNext()
	{
		return ++_counter;
	}
}

/// <summary>
/// 订单服务视图，用于验证同一作用域内多个接口共享同一目标实例。
/// </summary>
public interface IOrderService
{
	/// <summary>
	/// 设置共享标志值。
	/// </summary>
	/// <param name="value">要写入的标志值。</param>
	void SetFlag(string value);
}

/// <summary>
/// 审计服务视图，用于读取订单服务写入的共享标志。
/// </summary>
public interface IAuditService
{
	/// <summary>
	/// 获取共享标志值。
	/// </summary>
	/// <returns>当前共享标志值。</returns>
	string GetFlag();
}

/// <summary>
/// 同一实现类的多个接口视图，用于验证作用域内共享目标实例。
/// </summary>
public class OrderAuditService : BaseApplicationService, IOrderService, IAuditService
{
	/// <summary>
	/// 获取 <see cref="OrderAuditService"/> 的实例构造次数，用于验证作用域内只创建一次。
	/// </summary>
	public static int Created;

	/// <summary>
	/// 初始化实例并累加构造计数。
	/// </summary>
	public OrderAuditService()
	{
		Interlocked.Increment(ref Created);
	}

	/// <summary>
	/// 获取或设置共享标志值；声明为虚属性以便类代理拦截并转发。
	/// </summary>
	// virtual 属性：类代理可拦截并转发到共享的目标实例（字段不会被转发）。
	public virtual string Flag { get; set; } = string.Empty;

	/// <summary>
	/// 设置共享标志值。
	/// </summary>
	/// <param name="value">要写入的标志值。</param>
	public virtual void SetFlag(string value) => Flag = value;

	/// <summary>
	/// 获取共享标志值。
	/// </summary>
	/// <returns>当前共享标志值。</returns>
	public virtual string GetFlag() => Flag;
}

/// <summary>
/// 无默认构造函数服务的契约，用于验证实现类路径可回退为裸实例。
/// </summary>
public interface INoDefaultCtorService
{
	/// <summary>
	/// 获取固定值。
	/// </summary>
	int Value { get; }
}

/// <summary>
/// 无默认构造函数（构造参数可从 DI 解析）：实现类路径应回退为裸实例。
/// </summary>
public class NoDefaultCtorService : BaseApplicationService, INoDefaultCtorService
{
	/// <summary>
	/// 通过依赖注入接收 <see cref="ProxyGenerator"/> 并初始化实例。
	/// </summary>
	/// <param name="generator">由容器注入的代理生成器，本测试不使用。</param>
	public NoDefaultCtorService(ProxyGenerator generator)
	{
	}

	/// <summary>
	/// 获取固定值 <c>1</c>。
	/// </summary>
	public int Value => 1;
}

/// <summary>
/// 构建应用层测试容器的工厂：注册代理生成器、默认拦截器与应用服务，供各测试按需创建服务提供程序。
/// </summary>
internal static class TestContainer
{
	/// <summary>
	/// 创建已注册测试所需应用服务与拦截器的服务提供程序。
	/// </summary>
	/// <returns>可创建作用域的服务提供程序；调用方负责释放。</returns>
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
		services.AddTransient<IUseCaseExecutor, UseCaseExecutor>();
		services.AddApplicationService(typeof(CounterService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}
}

/// <summary>
/// 串行化共享静态状态的测试类（CounterService.MaxConcurrent 等）。
/// </summary>
[CollectionDefinition("AppTests", DisableParallelization = true)]
public class AppTestsCollection
{
}
