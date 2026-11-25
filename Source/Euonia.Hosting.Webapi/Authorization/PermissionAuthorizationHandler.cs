using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Nerosoft.Euonia.Security;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 依据 <see cref="RequiresPermissionAttribute"/> 声明的权限码完成授权判定。
/// </summary>
/// <param name="guard">作用域守卫，负责解析当前主体的权限码集合。</param>
/// <param name="accessor">HTTP 上下文访问器，用于取得请求取消标记。</param>
public sealed class PermissionAuthorizationHandler(IScopeGuard guard, IHttpContextAccessor accessor) : AuthorizationHandler<PermissionRequirement>
{
	/// <inheritdoc />
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
	{
		if (context.User?.Identity?.IsAuthenticated != true)
		{
			return;
		}

		var cancellationToken = accessor.HttpContext?.RequestAborted ?? default;

		await guard.EnsureResolvedAsync(cancellationToken);

		var subjects = guard.GetSubjects();

		if (requirement.Permissions.Any(permission => subjects.HoldsPermission(permission.Trim())))
		{
			context.Succeed(requirement);
		}
	}
}
