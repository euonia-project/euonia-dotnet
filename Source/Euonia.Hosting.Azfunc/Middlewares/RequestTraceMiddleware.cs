using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 为函数请求写入 <c>x-request-trace-id</c> 响应头，并同步到 <see cref="HttpContext.TraceIdentifier"/>。
/// </summary>
internal sealed class RequestTraceMiddleware : IFunctionsWorkerMiddleware
{
	/// <inheritdoc />
	public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		var httpContext = context.GetHttpContext();

		if (httpContext is null)
		{
			return next(context);
		}

		var traceId = Activity.Current?.TraceId.ToString();

		if (string.IsNullOrWhiteSpace(traceId))
		{
			traceId = httpContext.TraceIdentifier;
		}

		if (string.IsNullOrWhiteSpace(traceId))
		{
			traceId = Guid.NewGuid().ToString("N");
		}

		httpContext.TraceIdentifier = traceId;
		httpContext.Response.Headers["x-request-trace-id"] = traceId;
		context.Items["RequestTraceId"] = traceId;

		return next(context);
	}
}
