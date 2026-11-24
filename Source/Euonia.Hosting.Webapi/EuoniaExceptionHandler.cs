using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
		logger.LogError(exception, "{Message}", exception.Message);

		var statusCode = (int)exception.GetStatusCode();

		httpContext.Response.StatusCode = statusCode;

		await httpContext.Response.WriteAsJsonAsync(new
		{
			status = statusCode,
			message = exception.GetErrorMessage(),
			details = exception.GetErrorDetails()
		}, cancellationToken);

		return true;
	}
}
