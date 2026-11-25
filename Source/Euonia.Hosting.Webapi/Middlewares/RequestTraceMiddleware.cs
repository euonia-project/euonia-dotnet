using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 用于追踪 HTTP 请求的中间件。
/// </summary>
internal class RequestTraceMiddleware
{
	private readonly RequestDelegate _next;

	/// <summary>
	/// 初始化 <see cref="RequestTraceMiddleware"/> 类的新实例。
	/// </summary>
	/// <param name="next">请求管道中要调用的下一个中间件委托。</param>
	public RequestTraceMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	/// <summary>
	/// 处理请求，并在响应头中写入请求跟踪标识。
	/// </summary>
	/// <param name="context">当前 HttpContext 实例。</param>
	/// <returns>表示异步请求处理过程的任务。</returns>
	public async Task InvokeAsync(HttpContext context)
	{
		context.Response.Headers.Append("x-request-trace-id", context.TraceIdentifier);

		await _next(context);
	}
}