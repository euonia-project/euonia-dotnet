using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nerosoft.Euonia.Telemetry;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 在租户解析完成后为现有 HTTP 活动补充租户与结果标签；不创建新 Span。
/// </summary>
internal sealed class TelemetryMiddleware
{
	private readonly RequestDelegate _next;

	public TelemetryMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		finally
		{
			Enrich(context);
		}
	}

	private static void Enrich(HttpContext context)
	{
		var activity = Activity.Current;
		if (activity == null)
		{
			return;
		}

		try
		{
			activity.SetTag("http.response.status_code", context.Response.StatusCode);
			activity.SetTag(TelemetryNames.Outcome, context.Response.StatusCode < 500 ? "success" : "failure");

			var route = context.GetEndpoint() is RouteEndpoint endpoint ? endpoint.RoutePattern.RawText : null;
			if (!string.IsNullOrWhiteSpace(route))
			{
				activity.SetTag("url.path", route);
			}

			if (context.Items.TryGetValue(TenantHttpKeys.Context, out var value) && value is TenantContext tenant)
			{
				activity.SetTag(TelemetryNames.TenantId, tenant.TenantId);
				activity.SetTag(TelemetryNames.TenantActorId, tenant.ActorTenantId);
				if (tenant.IsCrossTenant)
				{
					activity.SetTag(TelemetryNames.TenantCross, true);
				}
			}
		}
		catch (Exception)
		{
			// 遥测失败开放。
		}
	}
}
