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
		IOptions<MultiTenancyOptions> optionsAccessor,
		ILogger<TenancyMiddleware> logger)
	{
		var options = optionsAccessor.Value;

		if (IsExcluded(context, options))
		{
			await _next(context);
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

		using var scope = scopeFactory.Begin(result.Tenant!);

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
			if (!string.IsNullOrWhiteSpace(excluded)
			    && path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
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
