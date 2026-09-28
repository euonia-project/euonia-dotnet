namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供对当前用户权限和角色进行断言的能力。
/// </summary>
public interface IPermissionChecker
{
	/// <summary>
	/// 判断当前用户是否已被授予指定权限；除 <paramref name="permission"/> 为空外，未认证的用户一律返回 <see langword="false"/>。
	/// 默认实现 <see cref="SubjectPermissionChecker"/> 从授权数据实时解析权限码，撤销授权在下一次解析即刻生效。
	/// </summary>
	/// <param name="permission">权限名称，空字符串或 <see langword="null"/> 视为放行；支持以 <c>*</c> 结尾的前缀通配符匹配。</param>
	/// <returns>授权通过则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsGranted(string permission);

	/// <summary>
	/// 判断当前用户是否已被授予指定权限中的任意一个；逐一委托给 <see cref="IsGranted"/>，<paramref name="permissions"/>
	/// 为 <see langword="null"/> 或空数组时返回 <see langword="false"/>。
	/// </summary>
	/// <param name="permissions">权限名称数组。</param>
	/// <returns>任意一个权限通过则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsGrantedAny(params string[] permissions);

	/// <summary>
	/// 判断当前用户是否属于指定角色；角色取自用户声明（<see cref="UserPrincipal.IsInRole"/>），
	/// 未认证的用户一律返回 <see langword="false"/>。
	/// </summary>
	/// <param name="role">角色名称。</param>
	/// <returns>属于该角色则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsInRole(string role);

	/// <summary>
	/// 判断当前用户是否属于指定角色中的任意一个；逐一委托给 <see cref="IsInRole"/>，<paramref name="roles"/>
	/// 为 <see langword="null"/> 或空数组时返回 <see langword="false"/>。
	/// </summary>
	/// <param name="roles">角色名称数组。</param>
	/// <returns>属于任意一个角色则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsInAnyRole(params string[] roles);

	/// <summary>
	/// 判断当前用户是否同时满足给定的权限要求与角色要求；两部分各自独立判定，角色一侧只需满足其中任意一个。
	/// </summary>
	/// <param name="permission">权限名称，空字符串或 <see langword="null"/> 表示不校验权限。</param>
	/// <param name="roles">角色名称数组，为空或 <see langword="null"/> 表示不校验角色。</param>
	/// <returns>同时满足有效要求（已指定权限或角色时全部通过）则返回 <see langword="true"/>。</returns>
	bool IsRequirementSatisfied(string permission, string[] roles);
}