using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 审计中间件注册扩展。
/// </summary>
public static class AuditingApplicationBuilderExtensions
{
	/// <summary>
	/// 安装审计中间件；应位于租户中间件之前。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>应用构建器。</returns>
	public static IApplicationBuilder UseAuditing(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);

		var options = app.ApplicationServices.GetService<IOptions<AuditingOptions>>()?.Value;
		return options is { Enabled: false } or null ? app : app.UseMiddleware<AuditingMiddleware>();
	}
}
