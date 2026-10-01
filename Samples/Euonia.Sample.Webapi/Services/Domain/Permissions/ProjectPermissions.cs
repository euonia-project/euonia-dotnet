namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 项目资源在工厂方法上声明的权限码。这些码同时充当类型级操作闸门
/// （见 <see cref="Nerosoft.Euonia.Security.PermissionAttribute"/>，可附加允许的角色）与行级策略键
/// （见 <see cref="ProjectScopeModel"/> 中按码声明的策略）。
/// </summary>
public static class ProjectPermissions
{
	/// <summary>操作码前缀：供 <see cref="ScopeSubjectResolver"/> 把行级授予路由到项目维度。</summary>
	public const string Prefix = "project:";

	/// <summary>创建项目（工厂 Create/Insert），允许 开发、项目管理。</summary>
	public const string Create = "project:create";

	/// <summary>查看项目（工厂 Fetch），允许 开发、测试、项目管理。</summary>
	public const string View = "project:view";

	/// <summary>编辑项目（工厂 Update），允许 开发、项目管理。</summary>
	public const string Edit = "project:edit";

	/// <summary>归档项目（命令对象 Archive），允许 开发、项目管理。</summary>
	public const string Archive = "project:archive";

	/// <summary>删除项目（工厂 Delete），仅允许 项目管理。</summary>
	public const string Delete = "project:delete";

	/// <summary>项目全部权限码（用于初始化与管理授权）。</summary>
	public static readonly string[] All = [Create, View, Edit, Archive, Delete];
}