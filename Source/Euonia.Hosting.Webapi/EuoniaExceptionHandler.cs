using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Hosting;

namespace Nerosoft.Euonia.Hosting.Webapi;

/// <summary>
/// 将未处理异常转换为统一的 JSON 错误响应。
/// </summary>
internal sealed class EuoniaExceptionHandler(ILogger<EuoniaExceptionHandler> logger) : IExceptionHandler
{
	/// <inheritdoc />
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		// 客户端已断开：不写响应、不记错误日志，交由服务器按请求中止处理。
		if (httpContext.RequestAborted.IsCancellationRequested)
		{
			return false;
		}

		logger.LogError(exception, "{Message}", exception.Message);

		var statusCode = (int)exception.GetStatusCode();

		httpContext.Response.StatusCode = statusCode;

		var message = exception.GetErrorMessage();
		var details = exception.GetErrorDetails();

		// 5xx 可能暴露内部实现细节：对外统一通用消息，完整异常仅写日志；Development 环境保留详情便于调试。
		if (statusCode >= StatusCodes.Status500InternalServerError && !IsDevelopment(httpContext))
		{
			message = "服务器内部错误，请稍后重试";
			details = null;
		}

		await httpContext.Response.WriteAsJsonAsync(new
		{
			status = statusCode,
			message,
			details
		}, cancellationToken);

		return true;
	}

	private static bool IsDevelopment(HttpContext httpContext)
	{
		var environment = httpContext.RequestServices?.GetService<IHostEnvironment>();
		return environment?.IsDevelopment() ?? false;
	}
}
