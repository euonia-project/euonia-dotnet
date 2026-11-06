using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 用于处理异常的中间件。
/// </summary>
internal class ExceptionHandlingMiddleware : IMiddleware
{
	private readonly ILogger<ExceptionHandlingMiddleware> _logger;

	/// <summary>
	/// 初始化 <see cref="ExceptionHandlingMiddleware"/> 类的新实例。
	/// </summary>
	/// <param name="logger">日志工厂，用于创建该中间件类型的日志记录器。</param>
	public ExceptionHandlingMiddleware(ILoggerFactory logger)
	{
		_logger = logger.CreateLogger<ExceptionHandlingMiddleware>();
	}

	/// <inheritdoc />
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "{Message}", exception.Message);

			await HandleExceptionAsync(context, exception);
		}
	}

	private static Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
	{
		var statusCode = (int)(exception?.GetStatusCode() ?? HttpStatusCode.InternalServerError);

		var response = new
		{
			status = statusCode,
			message = exception?.GetErrorMessage(),
			details = exception?.GetErrorDetails()
		};

		httpContext.Response.ContentType = "application/json";

		httpContext.Response.StatusCode = statusCode;

		return httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}