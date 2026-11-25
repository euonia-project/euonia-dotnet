using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对函数异常中间件消息泄漏与请求中止的测试。
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
	/// <summary>
	/// 500 响应不应包含原始异常消息，仅返回通用提示。
	/// </summary>
	[Fact]
	public async Task ServerError_ReturnsGenericMessage()
	{
		var httpContext = await InvokeAsync(new InvalidOperationException("数据库连接字符串泄露"));

		Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

		httpContext.Response.Body.Position = 0;
		using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
		var message = document.RootElement.GetProperty("message").GetString();
		Assert.NotNull(message);
		Assert.Equal("服务器内部错误，请稍后重试", message);
		Assert.DoesNotContain("数据库连接字符串泄露", message);
	}

	/// <summary>
	/// 4xx 应保留业务消息。
	/// </summary>
	[Fact]
	public async Task BadRequest_KeepsBusinessMessage()
	{
		var httpContext = await InvokeAsync(new ArgumentException("bad"));

		Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

		httpContext.Response.Body.Position = 0;
		using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("bad", document.RootElement.GetProperty("message").GetString());
	}

	/// <summary>
	/// Development 环境下 500 响应应保留原始异常消息便于调试。
	/// </summary>
	[Fact]
	public async Task ServerError_Development_ReturnsOriginalMessage()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));

		var httpContext = await InvokeAsync(new InvalidOperationException("调试详情"), services.BuildServiceProvider());

		httpContext.Response.Body.Position = 0;
		using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("调试详情", document.RootElement.GetProperty("message").GetString());
	}

	/// <summary>
	/// 客户端已中止时不应写响应或记错误，直接重抛。
	/// </summary>
	[Fact]
	public async Task RequestAborted_RethrowsWithoutResponse()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var httpContext = new DefaultHttpContext
		{
			Response = { Body = new MemoryStream() },
			RequestAborted = cts.Token
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeCoreAsync(httpContext, new InvalidOperationException("不应输出")));

		Assert.Equal(0, httpContext.Response.Body.Length);
		Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
	}

	/// <summary>
	/// 非 HTTP 触发器异常应直接重抛。
	/// </summary>
	[Fact]
	public async Task NonHttpTrigger_Rethrows()
	{
		var context = TestFunctionContext.Create(
			httpContext: null,
			new ServiceCollection().BuildServiceProvider(),
			TestFunctionContext.CreateDefinition("QueueFunction", "Ns.QueueFunction.Run"));
		var middleware = new ExceptionHandlingMiddleware(NullLogger<ExceptionHandlingMiddleware>.Instance);

		await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context, _ => throw new InvalidOperationException("queue")));
	}

	private static Task<HttpContext> InvokeAsync(Exception exception, IServiceProvider? services = null)
	{
		var httpContext = new DefaultHttpContext
		{
			Response = { Body = new MemoryStream() }
		};
		return InvokeCoreAsync(httpContext, exception, services);
	}

	private static async Task<HttpContext> InvokeCoreAsync(HttpContext httpContext, Exception exception, IServiceProvider? services = null)
	{
		var context = TestFunctionContext.Create(
			httpContext,
			services ?? new ServiceCollection().BuildServiceProvider(),
			TestFunctionContext.CreateDefinition("TestFunction", "Ns.TestFunction.Run"));
		var middleware = new ExceptionHandlingMiddleware(NullLogger<ExceptionHandlingMiddleware>.Instance);

		await middleware.Invoke(context, _ => throw exception);
		return httpContext;
	}
}
