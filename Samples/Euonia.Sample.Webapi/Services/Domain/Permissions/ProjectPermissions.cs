namespace Nerosoft.Euonia.Sample.Domain.Permissions;

/// <summary>
/// 项目资源在工厂方法上声明的权限码。这些码同时充当类型级操作闸门
/// （见 <see cref="Nerosoft.Euonia.Security.PermissionAttribute"/>）与行级策略键
/// （见 <see cref="ProjectScopeModel"/> 中按码声明的策略）。
/// </summary>
public static class ProjectPermissions
{
	/// <summary>创建项目（工厂 Create/Insert）。</summary>
	public const string Create = "project:create";

	/// <summary>查看项目（工厂 Fetch）。</summary>
	public const string View = "project:view";

	/// <summary>编辑项目（工厂 Update）。</summary>
	public const string Edit = "project:edit";

	/// <summary>删除项目（工厂 Delete）。</summary>
	public const string Delete = "project:delete";
}