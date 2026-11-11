using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Telemetry;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 遥测中间件注册扩展。
/// </summary>
public static class TelemetryApplicationBuilderExtensions
{
	/// <summary>
	/// 为 HTTP 活动补充租户与结果标签；应位于租户中间件之后。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>应用构建器。</returns>
	public static IApplicationBuilder UseEuoniaTelemetry(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);

		var options = app.ApplicationServices.GetService<IOptions<TelemetryOptions>>()?.Value;
		return options is { Enabled: false } or null ? app : app.UseMiddleware<TelemetryMiddleware>();
	}
}
