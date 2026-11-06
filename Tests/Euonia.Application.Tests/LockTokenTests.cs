using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 令牌锁服务的测试契约，用于验证令牌占位符会按方法实参替换为具体的锁名。
/// </summary>
public interface ITokenLockService
{
	/// <summary>
	/// 以字符串实参替换令牌中的 <c>{id}</c> 占位符后执行耗时操作。
	/// </summary>
	Task TouchAsync(string id);

	/// <summary>
	/// 以载荷对象的 <see cref="TokenPayload.Id"/> 替换令牌中的 <c>{payload.Id}</c> 占位符后执行耗时操作。
	/// </summary>
	Task TouchNestedAsync(TokenPayload payload);
}

/// <summary>
/// 令牌锁测试使用的载荷类型，其属性值参与嵌套属性占位符的替换。
/// </summary>
public class TokenPayload
{
	/// <summary>
	/// 用于替换 <c>{payload.Id}</c> 占位符的标识值。
	/// </summary>
	public string Id { get; set; } = null!;
}

/// <summary>
/// 锁令牌占位符回归测试：令牌形如 <c>{param}</c> 或 <c>{param.Property}</c>，
/// 运行时按方法实参替换。若占位符未被替换（退化为字面令牌），
/// 所有调用将共享同一把锁，无法证明占位符语义。
/// </summary>
public class TokenLockService : BaseApplicationService, ITokenLockService
{
	/// <summary>
	/// 当前正在执行 <see cref="TouchAsync"/> 或 <see cref="TouchNestedAsync"/> 的调用数，用于观察实际并发度。
	/// </summary>
	public static int Concurrent;

	/// <summary>
	/// 观测到的最大并发调用数，用于断言令牌替换后调用是并发还是串行。
	/// </summary>
	public static int MaxConcurrent;

	/// <summary>
	/// 以 <c>item:{id}</c> 为锁令牌，记录并发度后延迟 50 毫秒。
	/// </summary>
	/// <param name="id">替换令牌占位符的标识值。</param>
	[SemaphoreLock("item:{id}", Timeout = 5000)]
	public virtual async Task TouchAsync(string id)
	{
		await Delay();
	}

	/// <summary>
	/// 以 <c>nested:{payload.Id}</c> 为锁令牌，记录并发度后延迟 50 毫秒。
	/// </summary>
	/// <param name="payload">提供嵌套属性值的载荷对象。</param>
	[SemaphoreLock("nested:{payload.Id}", Timeout = 5000)]
	public virtual async Task TouchNestedAsync(TokenPayload payload)
	{
		await Delay();
	}

	private static async Task Delay()
	{
		var current = Interlocked.Increment(ref Concurrent);
		var max = Volatile.Read(ref MaxConcurrent);
		while (current > max && Interlocked.CompareExchange(ref MaxConcurrent, current, max) != max)
		{
			max = Volatile.Read(ref MaxConcurrent);
		}

		await Task.Delay(50, TestContext.Current.CancellationToken);
		Interlocked.Decrement(ref Concurrent);
	}
}

/// <summary>
/// 锁令牌占位符的回归测试，验证 <c>{param}</c> 与 <c>{param.Property}</c> 会按实参替换，
/// 从而让不同实参使用独立锁、相同实参共享同一把锁。
/// </summary>
[Collection("AppTests")]
public class LockTokenTests
{
	/// <summary>
	/// 不同实参生成不同令牌，两次调用应并发执行（最大并发达到 2）。
	/// </summary>
	[Fact]
	public async Task PlaceholderToken_DifferentIds_ShouldRunConcurrently()
	{
		// 不同实参 → 不同令牌 → 各自独立信号量 → 应并发（MaxConcurrent 达到 2）。
		// 若占位符未替换（共用一个字面令牌），MaxConcurrent 只能为 1。
		TokenLockService.MaxConcurrent = 0;
		TokenLockService.Concurrent = 0;

		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ITokenLockService>();

		await Task.WhenAll(svc.TouchAsync("a"), svc.TouchAsync("b"));

		Assert.Equal(2, TokenLockService.MaxConcurrent);
		Assert.Equal(0, TokenLockService.Concurrent);
	}

	/// <summary>
	/// 相同实参生成相同令牌，10 个并发调用应串行执行（最大并发保持为 1）。
	/// </summary>
	[Fact]
	public async Task PlaceholderToken_SameId_ShouldSerialize()
	{
		// 相同实参 → 相同令牌 → 串行互斥（MaxConcurrent 保持 1）。
		TokenLockService.MaxConcurrent = 0;
		TokenLockService.Concurrent = 0;

		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ITokenLockService>();

		await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => svc.TouchAsync("same")));

		Assert.Equal(1, TokenLockService.MaxConcurrent);
		Assert.Equal(0, TokenLockService.Concurrent);
	}

	/// <summary>
	/// 嵌套属性占位符按载荷的 <see cref="TokenPayload.Id"/> 替换，不同属性值的调用应并发执行。
	/// </summary>
	[Fact]
	public async Task NestedPropertyPlaceholder_DifferentPayloads_ShouldRunConcurrently()
	{
		// {payload.Id}：嵌套属性占位符按实参的属性值替换。
		TokenLockService.MaxConcurrent = 0;
		TokenLockService.Concurrent = 0;

		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ITokenLockService>();

		await Task.WhenAll(
			svc.TouchNestedAsync(new TokenPayload { Id = "x" }),
			svc.TouchNestedAsync(new TokenPayload { Id = "y" }));

		Assert.Equal(2, TokenLockService.MaxConcurrent);
		Assert.Equal(0, TokenLockService.Concurrent);
	}

	private static ServiceProvider CreateProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();
		services.AddTransient<IInterceptor, LockInterceptor>();
		services.AddApplicationService(typeof(TokenLockService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}
}