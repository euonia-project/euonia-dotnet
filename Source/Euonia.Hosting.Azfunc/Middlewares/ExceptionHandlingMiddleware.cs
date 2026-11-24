using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Hosting;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 捕获函数执行异常并写出统一的 JSON 错误响应。
/// </summary>
internal sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IFunctionsWorkerMiddleware
{
	/// <inheritdoc />
	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "{Message}", exception.Message);

			var httpContext = context.GetHttpContext();

			if (httpContext is null || httpContext.Response.HasStarted)
			{
				throw;
			}

			var statusCode = (int)exception.GetStatusCode();

			httpContext.Response.StatusCode = statusCode;

			await httpContext.Response.WriteAsJsonAsync(new
			{
				status = statusCode,
				message = exception.GetErrorMessage(),
				details = exception.GetErrorDetails()
			}, context.CancellationToken);
		}
	}
}
