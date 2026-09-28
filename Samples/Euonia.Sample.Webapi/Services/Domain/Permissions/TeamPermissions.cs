namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 团队资源在工厂方法上声明的权限码。这些码同时充当类型级操作闸门
/// （见 <see cref="Nerosoft.Euonia.Security.PermissionAttribute"/>，可附加允许的角色）与行级策略键
/// （见 <see cref="TeamScopeModel"/> 中按码声明的策略）。
/// </summary>
public static class TeamPermissions
{
	/// <summary>创建团队（工厂 Create/Insert），允许 开发、项目管理。</summary>
	public const string Create = "team:create";

	/// <summary>查看团队（工厂 Fetch），允许 开发、测试、项目管理。</summary>
	public const string View = "team:view";

	/// <summary>编辑团队（工厂 Update），允许 开发、项目管理。</summary>
	public const string Edit = "team:edit";

	/// <summary>删除团队（工厂 Delete），仅允许 项目管理。</summary>
	public const string Delete = "team:delete";

	/// <summary>团队全部权限码（用于初始化与管理授权）。</summary>
	public static readonly string[] All = [Create, View, Edit, Delete];
}