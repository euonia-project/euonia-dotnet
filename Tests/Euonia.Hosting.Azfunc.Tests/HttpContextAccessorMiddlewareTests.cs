using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对 HTTP 上下文中转中间件上下文还原的测试。
/// </summary>
public class HttpContextAccessorMiddlewareTests
{
	/// <summary>
	/// 请求结束后应还原先前的 HttpContext 与 AsyncLocal 请求上下文。
	/// </summary>
	[Fact]
	public async Task Invoke_SetsAndRestoresRequestContext()
	{
		var previousRequestContext = new RequestContext { ConnectionId = "previous" };
		var (accessor, requestContextAccessor, services) = CreateServices();
		requestContextAccessor.Context = previousRequestContext;

		var httpContext = new DefaultHttpContext();
		var context = TestFunctionContext.Create(httpContext, services, TestFunctionContext.CreateDefinition("TestFunction", "Ns.TestFunction.Run"));
		var middleware = new HttpContextAccessorMiddleware();

		RequestContext? insideRequestContext = null;

		await middleware.Invoke(context, _ =>
		{
			insideRequestContext = requestContextAccessor.Context;
			return Task.CompletedTask;
		});

		Assert.NotNull(insideRequestContext);
		Assert.NotSame(previousRequestContext, insideRequestContext);

		// timer/queue 触发器复用同一执行流，必须还原先前值。
		Assert.Same(previousRequestContext, requestContextAccessor.Context);
	}

	/// <summary>
	/// 先前值为空时请求结束后应清空，而不是残留本次请求上下文。
	/// </summary>
	[Fact]
	public async Task Invoke_RestoresNullContexts()
	{
		var (accessor, requestContextAccessor, services) = CreateServices();

		// 先前值为 null（AsyncLocal 默认值），无需显式设置。

		var httpContext = new DefaultHttpContext();
		var context = TestFunctionContext.Create(httpContext, services, TestFunctionContext.CreateDefinition("TestFunction", "Ns.TestFunction.Run"));
		var middleware = new HttpContextAccessorMiddleware();

		await middleware.Invoke(context, _ => Task.CompletedTask);

		Assert.Null(accessor.HttpContext);
		Assert.Null(requestContextAccessor.Context);
	}

	/// <summary>
	/// 后续中间件抛异常时仍应还原上下文。
	/// </summary>
	[Fact]
	public async Task Invoke_Exception_RestoresRequestContext()
	{
		var previousRequestContext = new RequestContext { ConnectionId = "previous" };
		var (_, requestContextAccessor, services) = CreateServices();
		requestContextAccessor.Context = previousRequestContext;

		var httpContext = new DefaultHttpContext();
		var context = TestFunctionContext.Create(httpContext, services, TestFunctionContext.CreateDefinition("TestFunction", "Ns.TestFunction.Run"));
		var middleware = new HttpContextAccessorMiddleware();

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			middleware.Invoke(context, _ => throw new InvalidOperationException("boom")));

		// 异常路径下 finally 仍应还原 RequestContext。
		Assert.Same(previousRequestContext, requestContextAccessor.Context);
	}

	private static (HttpContextAccessor Accessor, DefaultRequestContextAccessor RequestContextAccessor, IServiceProvider Services) CreateServices()
	{
		var accessor = new HttpContextAccessor();
		var requestContextAccessor = new DefaultRequestContextAccessor();

		var services = new ServiceCollection()
			.AddSingleton<IHttpContextAccessor>(accessor)
			.AddSingleton(requestContextAccessor)
			.BuildServiceProvider();

		return (accessor, requestContextAccessor, services);
	}
}
