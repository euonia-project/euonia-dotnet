namespace Nerosoft.Euonia.Security;

/// <summary>
/// 默认的操作权限检查器：权限码<b>从授权数据实时解析</b>，角色仍来自声明。
/// </summary>
/// <remarks>
/// <para>
/// 权限码<b>不放在令牌里</b>（理由见 DESIGN §1.2）：它由 <see cref="IScopeSubjectResolver"/> 实时解析、随 <see cref="IScopeGuard"/> 按请求缓存。
/// </para>
/// <para>
/// 因此<b>撤销的生效时机是「下一次解析」</b>（通常是下一个请求）；同一作用域内需要立即生效时显式调用
/// <see cref="IScopeGuard.Refresh"/>，全程不需要重新签发令牌（见 README §5.5）。
/// </para>
/// <para>
/// 角色仍走 <see cref="UserPrincipal.IsInRole"/>（来自声明）：角色数量少而稳定，不构成令牌膨胀问题；<b>细粒度授权请一律使用权限码</b>。
/// </para>
/// </remarks>
public class SubjectPermissionChecker : IPermissionChecker
{
	private readonly UserPrincipal _user;
	private readonly IScopeGuard _guard;

	/// <summary>
	/// 初始化 <see cref="SubjectPermissionChecker"/> 的新实例。
	/// </summary>
	/// <param name="user">当前登录用户；为 <see langword="null"/> 时所有权限码判定为未授权。</param>
	/// <param name="guard">授权数据入口，提供当前用户持有的权限码。</param>
	public SubjectPermissionChecker(UserPrincipal user, IScopeGuard guard)
	{
		_user = user;
		_guard = guard;
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

		// 支持以 * 结尾的前缀通配（例如持有 repo:* 可通过 repo:push 的类型级闸门）
		return _guard.GetSubjects().HoldsPermission(permission);
	}

	/// <inheritdoc />
	public bool IsGrantedAny(params string[] permissions)
	{
		return permissions?.Any(IsGranted) == true;
	}

	/// <inheritdoc />
	public bool IsInRole(string role)
	{
		return _user is { IsAuthenticated: true } && _user.IsInRole(role);
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
}
