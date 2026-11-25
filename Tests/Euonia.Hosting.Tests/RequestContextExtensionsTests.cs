using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 针对由 <see cref="HttpContext"/> 创建 <see cref="RequestContext"/> 的回归测试（中5：HTTP/2 小写头名）。
/// </summary>
public class RequestContextExtensionsTests
{
	/// <summary>
	/// 中5：HTTP/2 头名全小写时，经 UseDefaultRequestContextAccessor（内部调用 RequestContext.From）
	/// 创建的请求上下文，其头字典应忽略大小写，按 "User-Agent" 等规范大小写读取应命中。
	/// 修复前 ToDictionary 默认大小写敏感，小写键导致精确匹配全部 miss。
	/// </summary>
	[Fact]
	public async Task From_LowercaseHttp2Headers_ShouldBeReadableByCanonicalNames()
	{
		var services = new ServiceCollection();
		services.AddScoped<DefaultRequestContextAccessor>();
		await using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var httpContext = new DefaultHttpContext
		{
			RequestServices = scope.ServiceProvider
		};
		httpContext.Request.Protocol = "HTTP/2";
		httpContext.Request.Method = "GET";
		httpContext.Request.Headers["user-agent"] = "h2-agent";
		httpContext.Request.Headers["referer"] = "https://h2.example.com";
		httpContext.Request.Headers["x-client-name"] = "h2-client";
		httpContext.Request.Headers["x-client-version"] = "2.0.0";
		httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

		RequestContext? captured = null;
		var app = new ApplicationBuilder(scope.ServiceProvider);
		app.UseDefaultRequestContextAccessor();
		app.Use(async (ctx, next) =>
		{
			// AsyncLocal 的值不会向调用方回流，须在与设置中间件相同的执行流内捕获上下文。
			captured = ctx.RequestServices.GetRequiredService<DefaultRequestContextAccessor>().Context;
			await next();
		});

		await app.Build()(httpContext);

		Assert.NotNull(captured);
		Assert.Equal("h2-agent", captured.UserAgent);
		Assert.Equal("https://h2.example.com", captured.Referer);
		Assert.True(captured.Headers!.TryGetValue("User-Agent", out var userAgent));
		Assert.Equal("h2-agent", userAgent);
		Assert.True(captured.Headers.TryGetValue("X-Client-Name", out var clientName));
		Assert.Equal("h2-client", clientName);
	}

	/// <summary>
	/// UseDefaultRequestContextAccessor 应在请求结束后恢复进入管道前的 AsyncLocal 值，
	/// 与 Azfunc 的 HttpContextAccessorMiddleware 保持一致。
	/// </summary>
	[Fact]
	public async Task UseDefaultRequestContextAccessor_RestoresPreviousContext()
	{
		var services = new ServiceCollection();
		services.AddScoped<DefaultRequestContextAccessor>();
		await using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		var accessor = scope.ServiceProvider.GetRequiredService<DefaultRequestContextAccessor>();
		var previous = new RequestContext { TraceIdentifier = "previous" };
		accessor.Context = previous;

		RequestContext? captured = null;
		var app = new ApplicationBuilder(scope.ServiceProvider);
		app.UseDefaultRequestContextAccessor();
		app.Use(async (_, next) =>
		{
			captured = accessor.Context;
			await next();
		});

		var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		await app.Build()(httpContext);

		Assert.NotNull(captured);
		Assert.NotSame(previous, captured);
		Assert.Same(previous, accessor.Context);
	}
}
