using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 为每个 HTTP 请求安装审计作用域，并在响应结束后提交记录。
/// </summary>
/// <remarks>
/// 必须位于 <see cref="TenancyMiddleware"/> 之前（外层）：租户在内部解析，完成时从
/// <see cref="TenantHttpKeys.Context"/> 快照回填，不能读取已释放的租户作用域。
/// </remarks>
internal sealed class AuditingMiddleware
{
	private readonly RequestDelegate _next;

	public AuditingMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(
		HttpContext context,
		IAuditingRecorder recorder,
		IRequestContextAccessor requestContextAccessor,
		ILogger<AuditingMiddleware> logger)
	{
		if (!recorder.IsEnabled)
		{
			await _next(context);
			return;
		}

		var scope = recorder.EnsureScope(new AuditingScopeOptions
		{
			OperationKey = $"req:{Guid.CreateVersion7():N}",
			AutoComplete = false,
			HttpMethod = context.Request.Method,
			Url = context.Request.Path.Value,
			ClientIpAddress = context.Connection.RemoteIpAddress?.ToString(),
			ClientName = context.Request.Headers["X-Client-Name"].FirstOrDefault(),
			BrowserInfo = context.Request.Headers.UserAgent.FirstOrDefault(),
		});

		Exception? error = null;
		try
		{
			await _next(context);
		}
		catch (Exception exception)
		{
			error = exception;
			throw;
		}
		finally
		{
			try
			{
				FillRecord(scope, context, requestContextAccessor, error);
				await scope.CompleteAsync(CancellationToken.None);
			}
			catch (Exception exception)
			{
				// 审计失败开放：回填或提交失败只记录日志，不能覆盖原始业务异常。
				logger.LogError(exception, "Failed to complete auditing scope {ScopeId}.", scope.ScopeId);
			}
		}
	}

	private static void FillRecord(IAuditingScope scope, HttpContext context, IRequestContextAccessor requestContextAccessor, Exception? error)
	{
		var record = scope.Record;
		record.HttpStatusCode = context.Response.StatusCode;
		record.IsSuccess = error == null && context.Response.StatusCode < 400;

		if (context.Items.TryGetValue(TenantHttpKeys.Context, out var value) && value is TenantContext tenant)
		{
			record.TenantId ??= tenant.ActorTenantId ?? tenant.TenantId;
			record.TenantName ??= tenant.Tenant?.Name;
			record.TargetTenantId ??= tenant.TargetTenantId;
			record.IsCrossTenant = record.IsCrossTenant || tenant.IsCrossTenant;
			record.CrossTenantReason ??= tenant.CrossTenantReason;
		}

		// 租户解析失败也要留痕：租户中间件把失败码写入 Items，审计记录带上以便排查/合规。
		if (context.Items.TryGetValue(TenantHttpKeys.Failure, out var failure) && failure is string failureCode)
		{
			record.IsSuccess = false;
			record.MergeExtraProperties(new Dictionary<string, string>
			{
				["tenant.failure"] = failureCode,
				["tenant.candidates"] = context.Items.TryGetValue(TenantHttpKeys.Candidates, out var candidates)
					                        ? System.Text.Json.JsonSerializer.Serialize(candidates)
					                        : string.Empty,
			});
		}

		var requestContext = requestContextAccessor.Context;
		if (requestContext != null)
		{
			record.CorrelationId ??= requestContext.CorrelationId;
			record.RequestTraceId ??= requestContext.TraceIdentifier;

			var principal = new UserPrincipal(requestContext.User as ClaimsPrincipal);
			record.UserId ??= principal.UserId;
			record.UserName ??= principal.Username;
		}
	}
}
