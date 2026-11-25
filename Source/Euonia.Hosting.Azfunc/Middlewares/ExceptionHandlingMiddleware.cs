using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
			var httpContext = context.GetHttpContext();

			if (httpContext is null || httpContext.Response.HasStarted)
			{
				throw;
			}

			// 客户端已断开：不写响应、不记错误日志。
			if (httpContext.RequestAborted.IsCancellationRequested)
			{
				throw;
			}

			logger.LogError(exception, "{Message}", exception.Message);

			var statusCode = (int)exception.GetStatusCode();

			httpContext.Response.StatusCode = statusCode;

			var message = exception.GetErrorMessage();
			var details = exception.GetErrorDetails();

			// 5xx 可能暴露内部实现细节：对外统一通用消息，完整异常仅写日志；Development 环境保留详情便于调试。
			if (statusCode >= StatusCodes.Status500InternalServerError && !IsDevelopment(context))
			{
				message = "服务器内部错误，请稍后重试";
				details = null;
			}

			await httpContext.Response.WriteAsJsonAsync(new
			{
				status = statusCode,
				message,
				details
			}, context.CancellationToken);
		}
	}

	private static bool IsDevelopment(FunctionContext context)
	{
		var environment = context.InstanceServices?.GetService<IHostEnvironment>();
		return environment?.IsDevelopment() ?? false;
	}
}
