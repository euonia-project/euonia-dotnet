using Microsoft.AspNetCore.Http;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 从 <see cref="HttpContext"/> 构造租户解析输入。
/// </summary>
internal static class TenantResolveContextFactory
{
	public static TenantResolveContext FromHttpContext(HttpContext context, MultiTenancyOptions options)
	{
		var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		foreach (var header in context.Request.Headers)
		{
			// 与中间件保持一致：忽略空白项，避免 ["", "acme"] 取到空值。
			headers[header.Key] = header.Value.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
		}

		string? routeTenant = null;
		if (!string.IsNullOrWhiteSpace(options.RouteKey)
		    && context.Request.RouteValues.TryGetValue(options.RouteKey, out var routeValue))
		{
			routeTenant = routeValue?.ToString();
		}

		return new TenantResolveContext
		{
			Services = context.RequestServices,
			User = context.User,
			Headers = headers,
			Host = context.Request.Host.Host,
			Path = context.Request.Path.Value,
			RouteTenant = routeTenant,
			RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
		};
	}
}
