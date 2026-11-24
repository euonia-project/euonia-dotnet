using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 校验 Bearer JWT，并按函数或类型上的 <see cref="AuthorizeAttribute"/> / <see cref="AllowAnonymousAttribute"/> 执行认证与角色检查。
/// </summary>
internal sealed class JwtAuthenticationMiddleware(ILogger<JwtAuthenticationMiddleware> logger) : IFunctionsWorkerMiddleware
{
	/// <inheritdoc />
	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		var httpContext = context.GetHttpContext();

		if (httpContext is null)
		{
			await next(context);
			return;
		}

		var options = context.InstanceServices.GetService<EuoniaFunctionsOptions>();

		if (options?.EnableJwtAuthentication != true)
		{
			await next(context);
			return;
		}

		var method = FunctionMethodResolver.Resolve(context);

		if (method is null)
		{
			logger.LogWarning("无法解析函数 {Function} 的入口方法，已拒绝请求。", context.FunctionDefinition.Name);
			throw new AuthenticationException("无法解析函数入口方法。");
		}

		// 匿名函数不应因为携带了过期或无效令牌而被拒绝。
		if (IsAllowAnonymous(method))
		{
			await next(context);
			return;
		}

		var principal = Authenticate(httpContext.Request.Headers[HeaderNames.Authorization].ToString(), options.JwtAuthentication);

		if (principal is not null)
		{
			httpContext.User = principal;
		}

		var authorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
			.Concat(method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ?? [])
			.ToArray();

		if (authorizeAttributes.Length == 0)
		{
			await next(context);
			return;
		}

		if (httpContext.User.Identity?.IsAuthenticated != true)
		{
			throw new AuthenticationException("请求未经身份验证。");
		}

		foreach (var authorize in authorizeAttributes)
		{
			if (!string.IsNullOrWhiteSpace(authorize.Policy))
			{
				throw new UnauthorizedAccessException("Azure Functions 不支持基于策略的授权特性，请使用角色或 IPermissionChecker。");
			}

			if (string.IsNullOrWhiteSpace(authorize.Roles))
			{
				continue;
			}

			var roles = authorize.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (!roles.Any(httpContext.User.IsInRole))
			{
				throw new UnauthorizedAccessException("当前用户不属于所需角色。");
			}
		}

		await next(context);
	}

	private static System.Security.Claims.ClaimsPrincipal? Authenticate(string authorization, Microsoft.AspNetCore.Authentication.JwtAuthenticationOptions options)
	{
		if (string.IsNullOrWhiteSpace(authorization))
		{
			return null;
		}

		if (!AuthenticationHeaderValue.TryParse(authorization, out var header)
		    || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
		    || string.IsNullOrWhiteSpace(header.Parameter))
		{
			throw new AuthenticationException("Authorization 头不是有效的 Bearer 令牌。");
		}

		return JwtTokenValidator.Validate(header.Parameter, options);
	}

	private static bool IsAllowAnonymous(MethodInfo method)
	{
		return method.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null
		       || method.DeclaringType?.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;
	}
}
