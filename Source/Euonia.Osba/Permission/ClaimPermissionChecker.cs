using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 基于当前用户声明的默认权限检查器，从 <see cref="UserClaimTypes.Permission"/> 声明中读取权限。
/// </summary>
/// <remarks>
/// 权限值支持以 <c>*</c> 结尾的前缀通配符匹配，例如用户拥有 <c>order:*</c> 可匹配 <c>order:create</c>。
/// </remarks>
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