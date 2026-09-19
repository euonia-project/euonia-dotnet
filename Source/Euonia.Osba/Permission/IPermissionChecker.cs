namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 提供对当前用户权限和角色进行断言的能力。
/// </summary>
public interface IPermissionChecker
{
	/// <summary>
	/// 判断当前用户是否已被授予指定的权限。
	/// </summary>
	/// <param name="permission">权限名称，空字符串或 <see langword="null"/> 视为放行；支持以 <c>*</c> 结尾的前缀通配符匹配。</param>
	/// <returns>授权通过则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsGranted(string permission);

	/// <summary>
	/// 判断当前用户是否已被授予任意一个指定权限。
	/// </summary>
	/// <param name="permissions">权限名称数组。</param>
	/// <returns>任意一个权限通过则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsGrantedAny(params string[] permissions);

	/// <summary>
	/// 判断当前用户是否属于指定的角色。
	/// </summary>
	/// <param name="role">角色名称。</param>
	/// <returns>属于该角色则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsInRole(string role);

	/// <summary>
	/// 判断当前用户是否属于任意一个指定的角色。
	/// </summary>
	/// <param name="roles">角色名称数组。</param>
	/// <returns>属于任意一个角色则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsInAnyRole(params string[] roles);

	/// <summary>
	/// 判断当前用户是否满足权限与角色的组合要求。
	/// </summary>
	/// <param name="permission">权限名称，空字符串或 <see langword="null"/> 表示不校验权限。</param>
	/// <param name="roles">角色名称数组，为空或 <see langword="null"/> 表示不校验角色。</param>
	/// <returns>同时满足有效要求（已指定权限或角色时全部通过）则返回 <see langword="true"/>。</returns>
	bool IsRequirementSatisfied(string permission, string[] roles);
}