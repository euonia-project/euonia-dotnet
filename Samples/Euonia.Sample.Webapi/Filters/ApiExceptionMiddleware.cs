using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Authentication;
using Nerosoft.Euonia.Core;

namespace Nerosoft.Euonia.Sample.Filters;

/// <summary>
/// 统一异常出口：把未处理的异常映射为 <c>{ code, error, details }</c> JSON 与 HTTP 状态码。
/// 状态码映射规则：<see cref="HttpStatusException"/> 用其自带状态码；带
/// <see cref="HttpStatusCodeAttribute"/>（如 <see cref="NotFoundException"/>）取特性值；
/// 参数/校验错误 400；认证失败 401；权限不足（<see cref="SecurityException"/> 等）403；
/// 其余未预期异常 500（非开发环境不泄露异常细节）。
/// 控制器自理的场景（如 201/204/403 业务分支）仍按现状返回，本中间件只兜底。
/// </summary>
public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IHostEnvironment environment)
{
	/// <summary>执行管道；发生异常时记录日志并映射为统一错误响应。</summary>
	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await next(context);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "{Message}", exception.Message);

			if (context.Response.HasStarted)
			{
				throw;
			}

			var statusCode = ResolveStatusCode(exception);
			context.Response.StatusCode = statusCode;
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsJsonAsync(new
			{
				code = statusCode,
				error = ResolveMessage(exception, statusCode),
				details = ResolveDetails(exception)
			});
		}
	}

	private int ResolveStatusCode(Exception exception)
	{
		return exception switch
		{
			HttpStatusException ex => (int)ex.StatusCode,
			ValidationException => (int)HttpStatusCode.BadRequest,
			AuthenticationException => (int)HttpStatusCode.Unauthorized,
			SecurityException => (int)HttpStatusCode.Forbidden,
			UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
			ArgumentException => (int)HttpStatusCode.BadRequest,
			BusinessException => (int)HttpStatusCode.BadRequest,
			AggregateException { InnerExceptions.Count: > 0 } ex => ResolveStatusCode(Unwrap(ex)),
			TargetInvocationException ex when ex.InnerException != null => ResolveStatusCode(ex.InnerException),
			_ => TryGetAttributeStatusCode(exception)
		};
	}

	private static int TryGetAttributeStatusCode(Exception exception)
	{
		var attribute = exception.GetType().GetCustomAttribute<HttpStatusCodeAttribute>();
		return attribute == null ? (int)HttpStatusCode.InternalServerError : (int)attribute.StatusCode;
	}

	private string ResolveMessage(Exception exception, int statusCode)
	{
		var inner = Unwrap(exception);
		var message = inner?.Message ?? exception.Message;

		// 未预期异常（500）不向调用方泄露内部细节；开发环境除外。
		if (statusCode >= 500 && !environment.IsDevelopment())
		{
			message = null;
		}

		if (string.IsNullOrWhiteSpace(message))
		{
			message = statusCode >= 500 ? "服务器内部错误，请稍后再试。" : "请求处理失败。";
		}

		return message;
	}

	private object ResolveDetails(Exception exception)
	{
		if (Unwrap(exception) is not ValidationException ex)
		{
			return null;
		}

		return ex.ValidationResult.MemberNames
		          .Distinct()
		          .ToArray();
	}

	private static Exception Unwrap(Exception exception)
	{
		var current = exception;
		while (current is AggregateException or TargetInvocationException)
		{
			current = current switch
			{
				AggregateException { InnerExceptions.Count: > 0 } agg => agg.InnerExceptions[0],
				TargetInvocationException { InnerException: not null } target => target.InnerException,
				_ => current
			};
		}

		return current;
	}
}