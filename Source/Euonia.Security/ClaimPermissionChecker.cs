namespace Nerosoft.Euonia.Security;

/// <summary>
/// 基于当前用户声明的权限检查器，从 <see cref="UserClaimTypes.Permission"/> 声明中读取权限。
/// </summary>
/// <remarks>
/// <para>
/// 权限值支持以 <c>*</c> 结尾的前缀通配符匹配，例如用户拥有 <c>order:*</c> 可匹配 <c>order:create</c>。
/// </para>
/// <para>
/// 本实现把权限码固化在令牌里，权限码多时会撑爆令牌，
/// 且<b>取消授权后旧令牌在过期前仍然有效</b>。
/// 需要实时解析授权数据时请使用 <see cref="SubjectPermissionChecker"/>。
/// </para>
/// </remarks>
public class ClaimPermissionChecker : IPermissionChecker
{
	private readonly UserPrincipal _user;

	/// <summary>
	/// 初始化 <see cref="ClaimPermissionChecker"/> 的新实例。
	/// </summary>
	/// <param name="user">当前登录用户；为 <see langword="null"/> 时所有权限码判定为未授权。</param>
	public ClaimPermissionChecker(UserPrincipal user)
	{
		_user = user;
	}

	/// <inheritdoc />
	public bool IsGranted(string permission)
	{
		if (string.IsNullOrEmpty(permission))
		{
			return true;
		}

		var user = _user;
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
	public bool IsInRole(string role)
	{
		var user = _user;
		return user != null && user.IsAuthenticated && user.IsInRole(role);
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