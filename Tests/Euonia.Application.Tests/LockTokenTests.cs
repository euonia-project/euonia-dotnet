using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

public interface ITokenLockService
{
	Task TouchAsync(string id);

	Task TouchNestedAsync(TokenPayload payload);
}

public class TokenPayload
{
	public string Id { get; set; }
}

/// <summary>
/// 锁令牌占位符回归测试：令牌形如 <c>{param}</c> 或 <c>{param.Property}</c>，
/// 运行时按方法实参替换。若占位符未被替换（退化为字面令牌），
/// 所有调用将共享同一把锁，无法证明占位符语义。
/// </summary>
public class TokenLockService : BaseApplicationService, ITokenLockService
{
	public static int Concurrent;

	public static int MaxConcurrent;

	[SemaphoreLock("item:{id}", Timeout = 5000)]
	public virtual async Task TouchAsync(string id)
	{
		await Delay();
	}

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

[Collection("AppTests")]
public class LockTokenTests
{
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