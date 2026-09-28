namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 代码仓库资源在工厂方法上声明的权限码。这些码同时充当类型级操作闸门
/// （见 <see cref="Nerosoft.Euonia.Security.PermissionAttribute"/>，可附加允许的角色）与行级策略键
/// （见 <see cref="RepositoryScopeModel"/> 中按码声明的策略）。
/// </summary>
public static class RepositoryPermissions
{
	/// <summary>创建仓库（工厂 Create/Insert），允许 开发、项目管理。</summary>
	public const string Create = "repository:create";

	/// <summary>查看仓库（工厂 Fetch），允许 开发、测试、项目管理。</summary>
	public const string View = "repository:view";

	/// <summary>编辑仓库（工厂 Update），允许 开发、项目管理。</summary>
	public const string Edit = "repository:edit";

	/// <summary>删除仓库（工厂 Delete），仅允许 项目管理。</summary>
	public const string Delete = "repository:delete";
}