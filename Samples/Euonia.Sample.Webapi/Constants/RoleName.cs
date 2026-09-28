namespace Nerosoft.Euonia.Sample.Constants;

public static class RoleName
{
	/// <summary>
	/// Super user role constant.
	/// </summary>
	public const string SuperUser = "SU";

	/// <summary>
	/// System administrator role constant.
	/// </summary>
	public const string SystemAdmin = "AD";

	/// <summary>
	/// Helpdesk role constant.
	/// </summary>
	public const string Helpdesk = "HD";

	/// <summary>
	/// Normal user role constant.
	/// </summary>
	public const string NormalUser = "US";

	/// <summary>
	/// 开发角色（可创建仓库、推送/更新或删除被授予的仓库行；可编辑本团队）。
	/// </summary>
	public const string Developer = "developer";

	/// <summary>
	/// 测试角色（只读可见仓库与团队）。
	/// </summary>
	public const string Tester = "tester";

	/// <summary>
	/// 项目管理角色（可对仓库做全部操作，含删除）。
	/// </summary>
	public const string ProjectManager = "project-manager";

	/// <summary>
	/// Combined roles for system support.
	/// </summary>
	public const string Support = "SU,AD,HD";

	/// <summary>
	/// Combined roles for administrators.
	/// </summary>
	public const string Admin = "SU,AD";

	/// <summary>
	/// Gets all defined role names.
	/// </summary>
	public static IEnumerable<string> All => [SuperUser, SystemAdmin, Helpdesk, NormalUser, Developer, Tester, ProjectManager];
}