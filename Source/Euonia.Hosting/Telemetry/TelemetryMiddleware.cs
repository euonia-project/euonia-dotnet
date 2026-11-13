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
	private readonly Microsoft.Extensions.Options.IOptionsMonitor<TelemetryOptions>? _options;

	public TelemetryMiddleware(
		RequestDelegate next,
		Microsoft.Extensions.Options.IOptionsMonitor<TelemetryOptions>? options = null)
	{
		_next = next;
		_options = options;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// 入口捕获，避免下游创建未释放的子 Activity 导致标签打错 span。
		var activity = Activity.Current;
		var failed = false;
		try
		{
			await _next(context);
		}
		catch
		{
			failed = true;
			if (activity != null)
			{
				activity.SetStatus(ActivityStatusCode.Error);
			}

			throw;
		}
		finally
		{
			Enrich(context, activity, failed);
		}
	}

	private void Enrich(HttpContext context, Activity? activity, bool failed)
	{
		if (activity == null)
		{
			return;
		}

		try
		{
			if (failed)
			{
				// 异常在 finally 阶段尚未被外层异常处理器改写状态码，这里统一标记失败。
				activity.SetTag("http.response.status_code", 500);
				activity.SetTag(TelemetryNames.Outcome, "failure");
			}
			else
			{
				activity.SetTag("http.response.status_code", context.Response.StatusCode);
				activity.SetTag(TelemetryNames.Outcome, context.Response.StatusCode switch
				{
					< 400 => "success",
					< 500 => "client_error",
					_ => "failure",
				});
			}

			var route = context.GetEndpoint() is RouteEndpoint endpoint ? endpoint.RoutePattern.RawText : null;
			if (!string.IsNullOrWhiteSpace(route))
			{
				activity.SetTag("url.path", route);
			}

			if (_options?.CurrentValue.RecordTenantId != false
			    && context.Items.TryGetValue(TenantHttpKeys.Context, out var value)
			    && value is TenantContext tenant)
			{
				activity.SetTag(TelemetryNames.TenantId, tenant.TenantId);
				activity.SetTag(TelemetryNames.TenantActorId, tenant.ActorTenantId);
				if (tenant.IsCrossTenant)
				{
					activity.SetTag(TelemetryNames.TenantCross, true);
					activity.SetTag(TelemetryNames.TenantCrossReason, tenant.CrossTenantReason);
				}
			}
		}
		catch (Exception)
		{
			// 遥测失败开放。
		}
	}
}
