using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 写出与现有异常中间件一致形状的租户错误响应。
/// </summary>
internal static class TenantErrorWriter
{
	public static Task WriteAsync(HttpContext context, int statusCode, string error, string message)
	{
		context.Response.StatusCode = statusCode;
		context.Response.ContentType = "application/json";

		var payload = JsonSerializer.Serialize(new
		{
			status = statusCode,
			message,
			details = (string?)null,
			error,
		});

		return context.Response.WriteAsync(payload);
	}
}
