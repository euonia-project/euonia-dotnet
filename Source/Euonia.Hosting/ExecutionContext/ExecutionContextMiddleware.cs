using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 把当前 HTTP 请求的身份信息写入执行上下文，供租户、审计与遥测只读消费。
/// </summary>
internal sealed class ExecutionContextMiddleware
{
	private readonly RequestDelegate _next;

	public ExecutionContextMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(
		HttpContext context,
		IRequestContextAccessor requestContextAccessor,
		IExecutionContextScopeFactory scopeFactory,
		IOptions<MultiTenancyOptions> tenancyOptions)
	{
		var user = context.User;
		var principal = new UserPrincipal(user);
		var correlationId = ResolveCorrelationId(context);

		using var scope = scopeFactory.Begin(values =>
		{
			values.UserId = principal.UserId;
			values.UserName = principal.Username;
			values.ActorTenantId = context.User?.FindFirst(tenancyOptions.Value.ClaimType)?.Value;
			values.CorrelationId = correlationId;
			values.RequestTraceId = context.Request.Headers["x-request-trace-id"].FirstOrDefault()
			                        ?? context.TraceIdentifier;
		});

		var requestContext = requestContextAccessor.Context;
		if (requestContext != null)
		{
			requestContext.TenantId = scope.Context.TenantId;
			requestContext.CorrelationId = scope.Context.CorrelationId;
		}

		await _next(context);
	}

	private static string ResolveCorrelationId(HttpContext context)
	{
		var header = context.Request.Headers["x-correlation-id"].FirstOrDefault();
		if (!string.IsNullOrWhiteSpace(header))
		{
			return header;
		}

		var requestId = context.Request.Headers["Request-Id"].FirstOrDefault();
		return string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId;
	}
}
