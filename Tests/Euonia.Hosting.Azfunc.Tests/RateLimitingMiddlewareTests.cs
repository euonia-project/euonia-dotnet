using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nerosoft.Euonia.Hosting.RateLimiting;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对函数限流中间件并发单实例与驱逐释放的测试。
/// </summary>
public class RateLimitingMiddlewareTests
{
	/// <summary>
	/// 并发请求同一分区应共享唯一限流器：许可为 1 时只放行一个请求。
	/// </summary>
	[Fact]
	public async Task ConcurrentRequests_ShareSingleLimiter()
	{
		var options = new EuoniaFunctionsOptions
		{
			EnableRateLimiting = true,
			RateLimiting = new DualWindowRateLimiterOptions { MinutePermitLimit = 1, HourPermitLimit = 1000 }
		};
		using var middleware = new RateLimitingMiddleware(options, NullLogger<RateLimitingMiddleware>.Instance);
		var definition = TestFunctionContext.CreateDefinition("ConcurrentFunction", "Ns.ConcurrentFunction.Run");
		var services = new ServiceCollection().BuildServiceProvider();

		var successes = 0;
		var tasks = Enumerable.Range(0, 64).Select(_ =>
		{
			var httpContext = CreateHttpContext("10.1.0.1");
			var context = TestFunctionContext.Create(httpContext, services, definition);
			return middleware.Invoke(context, _ =>
			{
				Interlocked.Increment(ref successes);
				return Task.CompletedTask;
			});
		}).ToArray();

		await Task.WhenAll(tasks);

		Assert.Equal(1, successes);

		// 缓存值应为 Lazy 包装，保证并发工厂落选者不会创建限流器。
		var cache = GetCache(middleware);
		Assert.True(cache.TryGetValue("ConcurrentFunction:10.1.0.1", out var cached));
		Assert.IsType<Lazy<DualWindowRateLimiter>>(cached);
	}

	/// <summary>
	/// 分区缓存驱逐后应 Dispose 限流器，释放自动补充定时器。
	/// </summary>
	[Fact]
	public async Task Eviction_DisposesLimiter()
	{
		using var middleware = new RateLimitingMiddleware(new EuoniaFunctionsOptions(), NullLogger<RateLimitingMiddleware>.Instance);
		var httpContext = CreateHttpContext("10.1.0.2");
		var context = TestFunctionContext.Create(httpContext, new ServiceCollection().BuildServiceProvider(), TestFunctionContext.CreateDefinition("EvictFunction", "Ns.EvictFunction.Run"));

		await middleware.Invoke(context, _ => Task.CompletedTask);

		var cache = GetCache(middleware);
		Assert.True(cache.TryGetValue("EvictFunction:10.1.0.2", out var cached));
		var lazy = Assert.IsType<Lazy<DualWindowRateLimiter>>(cached);
		var limiter = lazy.Value;

		cache.Compact(1.0);

		await WaitUntilDisposedAsync(limiter);
	}

	/// <summary>
	/// 中间件释放时应级联释放全部分区限流器。
	/// </summary>
	[Fact]
	public async Task Dispose_DisposesLimiters()
	{
		var middleware = new RateLimitingMiddleware(new EuoniaFunctionsOptions(), NullLogger<RateLimitingMiddleware>.Instance);
		var httpContext = CreateHttpContext("10.1.0.3");
		var context = TestFunctionContext.Create(httpContext, new ServiceCollection().BuildServiceProvider(), TestFunctionContext.CreateDefinition("DisposeFunction", "Ns.DisposeFunction.Run"));

		await middleware.Invoke(context, _ => Task.CompletedTask);

		var cache = GetCache(middleware);
		Assert.True(cache.TryGetValue("DisposeFunction:10.1.0.3", out var cached));
		var limiter = Assert.IsType<Lazy<DualWindowRateLimiter>>(cached).Value;

		middleware.Dispose();

		await WaitUntilDisposedAsync(limiter);
	}

	/// <summary>
	/// 分区耗尽后应返回 429 与 RetryAfter 头。
	/// </summary>
	[Fact]
	public async Task ExhaustedPartition_Returns429()
	{
		var options = new EuoniaFunctionsOptions
		{
			RateLimiting = new DualWindowRateLimiterOptions { MinutePermitLimit = 1, HourPermitLimit = 1000 }
		};
		using var middleware = new RateLimitingMiddleware(options, NullLogger<RateLimitingMiddleware>.Instance);
		var definition = TestFunctionContext.CreateDefinition("LimitedFunction", "Ns.LimitedFunction.Run");
		var services = new ServiceCollection().BuildServiceProvider();

		var first = CreateHttpContext("10.1.0.4");
		var second = CreateHttpContext("10.1.0.4");

		var nextCalled = false;
		await middleware.Invoke(TestFunctionContext.Create(first, services, definition), _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		});
		await middleware.Invoke(TestFunctionContext.Create(second, services, definition), _ => Task.CompletedTask);

		Assert.True(nextCalled);
		Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
		Assert.True(second.Response.Headers.ContainsKey("Retry-After"));
	}

	private static DefaultHttpContext CreateHttpContext(string ip)
	{
		var context = new DefaultHttpContext
		{
			Response = { Body = new MemoryStream() }
		};
		context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
		return context;
	}

	private static MemoryCache GetCache(RateLimitingMiddleware middleware)
	{
		return (MemoryCache)typeof(RateLimitingMiddleware)
			.GetField("_limiters", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(middleware)!;
	}

	private static async Task WaitUntilDisposedAsync(RateLimiter limiter)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);

		while (DateTime.UtcNow < deadline)
		{
			try
			{
				using var lease = limiter.AttemptAcquire();
			}
			catch (ObjectDisposedException)
			{
				return;
			}

			await Task.Delay(20, TestContext.Current.CancellationToken);
		}

		Assert.Fail("驱逐后限流器未被 Dispose。");
	}
}
