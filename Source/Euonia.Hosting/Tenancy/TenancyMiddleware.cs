using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 解析、校验并安装租户作用域。
/// </summary>
internal sealed class TenancyMiddleware
{
	private readonly RequestDelegate _next;

	public TenancyMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(
		HttpContext context,
		ITenantResolver resolver,
		ITenantAccessValidator validator,
		ITenantScopeFactory scopeFactory,
		IRequestContextAccessor requestContextAccessor,
		IOptionsMonitor<MultiTenancyOptions> optionsAccessor,
		ILogger<TenancyMiddleware> logger)
	{
		var options = optionsAccessor.CurrentValue;

		if (IsExcluded(context, options))
		{
			await _next(context);
			return;
		}

		// 携带了非法租户头却按“无租户”继续会绕过隔离，必须显式拒绝。
		var rawHeaderValues = context.Request.Headers[options.HeaderName]
		                             .Where(value => !string.IsNullOrWhiteSpace(value))
		                             .Distinct(StringComparer.Ordinal)
		                             .ToArray();
		var rawHeader = rawHeaderValues.FirstOrDefault();
		if (rawHeaderValues.Length > 1)
		{
			context.Items[TenantHttpKeys.Failure] = TenantFailureCodes.Conflict;
			await TenantErrorWriter.WriteAsync(context, StatusCodes.Status400BadRequest, TenantFailureCodes.Conflict, "Multiple conflicting tenant headers were provided.");
			return;
		}

		if (!string.IsNullOrEmpty(rawHeader) && !TenantIdValidator.IsValid(rawHeader))
		{
			context.Items[TenantHttpKeys.Failure] = TenantFailureCodes.Invalid;
			await TenantErrorWriter.WriteAsync(context, StatusCodes.Status400BadRequest, TenantFailureCodes.Invalid, "The tenant identifier is invalid.");
			return;
		}

		var resolveContext = TenantResolveContextFactory.FromHttpContext(context, options);
		var result = await resolver.ResolveAsync(resolveContext, context.RequestAborted);

		if (!result.Succeeded)
		{
			// 可选租户：没有候选值时继续，不安装租户作用域。
			if (result.FailureCode == null && !options.RequireTenant)
			{
				await _next(context);
				return;
			}

			await WriteFailureAsync(context, result, options, logger);
			return;
		}

		if (!await validator.CanAccessAsync(context.User, result.Tenant!, context.RequestAborted))
		{
			context.Items[TenantHttpKeys.Failure] = TenantFailureCodes.Forbidden;
			context.Items[TenantHttpKeys.Candidates] = result.Candidates;
			await TenantErrorWriter.WriteAsync(context, options.ExposeTenantExistence ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound, TenantFailureCodes.Forbidden, "Access to the tenant was denied.");
			return;
		}

		ITenantScope scope;
		try
		{
			scope = scopeFactory.Begin(result.Tenant!);
		}
		catch (TenantScopeConflictException)
		{
			// 外层已存在不同租户作用域（例如消息管道内再走 HTTP）：映射为冲突而不是 500。
			context.Items[TenantHttpKeys.Failure] = TenantFailureCodes.Conflict;
			await TenantErrorWriter.WriteAsync(context, StatusCodes.Status409Conflict, TenantFailureCodes.Conflict, "The request conflicts with an active tenant scope.");
			return;
		}

		using var _ = scope;
		context.Items[TenantHttpKeys.Context] = scope.Context;
		context.Items[TenantHttpKeys.Candidates] = result.Candidates;

		var requestContext = requestContextAccessor.Context;
		if (requestContext != null)
		{
			requestContext.TenantId = result.Tenant!.Id;
		}

		if (options.EmitResponseHeader)
		{
			context.Response.Headers[options.HeaderName] = result.Tenant!.Id;
		}

		await _next(context);
	}

	private static bool IsExcluded(HttpContext context, MultiTenancyOptions options)
	{
		var path = context.Request.Path.Value ?? string.Empty;
		foreach (var excluded in options.ExcludedPaths)
		{
			if (string.IsNullOrWhiteSpace(excluded))
			{
				continue;
			}

			// 规范化尾斜杠后按路径段边界匹配：/api 不应排除 /apix 或 /api-docs。
			var normalized = excluded.TrimEnd('/');
			if (normalized.Length == 0)
			{
				normalized = "/";
			}

			if (path.Equals(normalized, StringComparison.OrdinalIgnoreCase)
			    || path.StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static Task WriteFailureAsync(HttpContext context, TenantResolutionResult result, MultiTenancyOptions options, ILogger logger)
	{
		context.Items[TenantHttpKeys.Failure] = result.FailureCode;
		context.Items[TenantHttpKeys.Candidates] = result.Candidates;

		var (status, message) = result.FailureCode switch
		{
			TenantFailureCodes.Required => (StatusCodes.Status400BadRequest, "A tenant is required but was not resolved."),
			TenantFailureCodes.Invalid => (StatusCodes.Status400BadRequest, "The tenant identifier is invalid."),
			TenantFailureCodes.Conflict => (StatusCodes.Status400BadRequest, "Multiple conflicting tenants were resolved."),
			TenantFailureCodes.NotFound => (StatusCodes.Status404NotFound, "The tenant was not found."),
			TenantFailureCodes.Suspended or TenantFailureCodes.Archived => options.ExposeTenantExistence
				? (StatusCodes.Status403Forbidden, "The tenant is not active.")
				: (StatusCodes.Status404NotFound, "The tenant was not found."),
			TenantFailureCodes.Forbidden => options.ExposeTenantExistence
				? (StatusCodes.Status403Forbidden, "Access to the tenant was denied.")
				: (StatusCodes.Status404NotFound, "The tenant was not found."),
			TenantFailureCodes.StoreUnavailable => (StatusCodes.Status503ServiceUnavailable, "The tenant store is unavailable."),
			_ => (StatusCodes.Status400BadRequest, "The tenant could not be resolved."),
		};

		logger.LogDebug("Tenant resolution failed with {FailureCode} for {Path}.", result.FailureCode, context.Request.Path);

		return TenantErrorWriter.WriteAsync(context, status, result.FailureCode ?? TenantFailureCodes.Required, message);
	}
}
