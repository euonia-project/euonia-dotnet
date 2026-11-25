using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Hosting;
using Nerosoft.Euonia.Hosting.Webapi;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 为 <see cref="WebApplication"/> 提供 Web API 宿主管道扩展。
/// </summary>
public static class WebApplicationExtensions
{
	/// <summary>
	/// 配置统一异常、HTTPS 重定向、CORS、OpenAPI、控制器与健康检查端点。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>传入的 <see cref="IApplicationBuilder"/>，以便链式调用。</returns>
	/// <remarks>调用方应已启用路由（<c>UseRouting</c>）；本方法通过 <c>UseEndpoints</c> 映射端点。</remarks>
	public static IApplicationBuilder UseEuoniaWebApi(this IApplicationBuilder app)
	{
		var options = app.ApplicationServices.GetRequiredService<EuoniaWebApiOptions>();

		app.UseMiddleware<RequestTraceMiddleware>();

		if (options.EnableExceptionHandling)
		{
			app.UseExceptionHandler();
		}

		if (options.EnableHttpsRedirection)
		{
			app.UseHttpsRedirection();
		}

		if (options.AllowedOrigins is { Length: > 0 })
		{
			app.UseCors();
		}

		app.UseEndpoints(endpoints =>
		{
			if (options.EnableOpenApi)
			{
				endpoints.MapOpenApi();
			}

			endpoints.MapControllers();

			if (options.EnableHealthChecks)
			{
				endpoints.MapHealthChecks(options.HealthCheckPath);
			}
		});

		return app;
	}

	/// <summary>
	/// 配置统一异常、HTTPS 重定向、CORS、OpenAPI、控制器与健康检查端点。
	/// </summary>
	/// <param name="app">Web 应用。</param>
	/// <returns>传入的 <see cref="WebApplication"/>，以便链式调用。</returns>
	public static WebApplication UseEuoniaWebApi(this WebApplication app)
	{
		var options = app.Services.GetRequiredService<EuoniaWebApiOptions>();

		app.UseMiddleware<RequestTraceMiddleware>();

		if (options.EnableExceptionHandling)
		{
			app.UseExceptionHandler();
		}

		if (options.EnableHttpsRedirection)
		{
			app.UseHttpsRedirection();
		}

		if (options.AllowedOrigins is { Length: > 0 })
		{
			app.UseCors();
		}

		if (options.EnableOpenApi)
		{
			app.MapOpenApi();
		}

		app.MapControllers();

		if (options.EnableHealthChecks)
		{
			app.MapHealthChecks(options.HealthCheckPath);
		}

		return app;
	}
}
