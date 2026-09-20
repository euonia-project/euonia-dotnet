using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 基于当前用户声明的权限检查器，从 <see cref="UserClaimTypes.Permission"/> 声明中读取权限。
/// </summary>
/// <remarks>
/// <para>
/// 权限值支持以 <c>*</c> 结尾的前缀通配符匹配，例如用户拥有 <c>order:*</c> 可匹配 <c>order:create</c>。
/// </para>
/// <para>
/// <b>不建议使用</b>：本实现把权限码固化在令牌里，权限码多时会撑爆令牌，
/// 且<b>取消授权后旧令牌在过期前仍然有效</b>。它已不是默认实现，
/// 仅为显式回退保留；新代码请使用 <see cref="SubjectPermissionChecker"/>（从授权数据实时解析）。
/// </para>
/// </remarks>
[Obsolete("权限码不应固化在令牌中：取消授权后旧令牌仍然有效。请改用 SubjectPermissionChecker（从授权数据实时解析）。")]
public class ClaimPermissionChecker : IPermissionChecker
{
	private readonly BusinessContext _context;

	/// <summary>
	/// 初始化 <see cref="ClaimPermissionChecker"/> 的新实例。
	/// </summary>
	/// <param name="context">当前业务上下文，用于解析当前用户。</param>
	public ClaimPermissionChecker(BusinessContext context)
	{
		_context = context;
	}

	/// <inheritdoc />
	public bool IsGranted(string permission)
	{
		if (string.IsNullOrEmpty(permission))
		{
			return true;
		}

		var user = _context.User;
		if (user == null || !user.IsAuthenticated)
		{
			return false;
		}

		var granted = user.Claims
		                   .FindAll(UserClaimTypes.Permission)
		                   .Select(claim => claim.Value);

		return granted.Any(grantedPermission => Matches(grantedPermission, permission));
	}

	/// <inheritdoc />
	public bool IsGrantedAny(params string[] permissions)
	{
		return permissions?.Any(IsGranted) == true;
	}

	/// <inheritdoc />
	public bool IsInRole(string role)
	{
		var user = _context.User;
		return user != null && user.IsAuthenticated && user.IsInRole(role);
	}

	/// <inheritdoc />
	public bool IsInAnyRole(params string[] roles)
	{
		return roles?.Any(IsInRole) == true;
	}

	/// <inheritdoc />
	public bool IsRequirementSatisfied(string permission, string[] roles)
	{
		var rolesGranted = roles == null || roles.Length == 0 || IsInAnyRole(roles);
		var permissionGranted = string.IsNullOrEmpty(permission) || IsGranted(permission);
		return rolesGranted && permissionGranted;
	}

	private static bool Matches(string grantedPermission, string requiredPermission)
	{
		if (string.Equals(grantedPermission, requiredPermission, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (grantedPermission.EndsWith('*'))
		{
			var prefix = grantedPermission[..^1];
			return requiredPermission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}
}