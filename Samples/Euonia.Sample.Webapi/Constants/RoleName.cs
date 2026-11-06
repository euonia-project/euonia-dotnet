namespace Nerosoft.Euonia.Sample.Constants;

/// <summary>
/// 示例应用使用的角色名称常量。
/// </summary>
public static class RoleName
{
	/// <summary>
	/// 超级用户角色常量。
	/// </summary>
	public const string SuperUser = "SU";

	/// <summary>
	/// 系统管理员角色常量。
	/// </summary>
	public const string SystemAdmin = "AD";

	/// <summary>
	/// 服务台角色常量。
	/// </summary>
	public const string Helpdesk = "HD";

	/// <summary>
	/// 普通用户角色常量。
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
	/// 系统支持使用的组合角色。
	/// </summary>
	public const string Support = "SU,AD,HD";

	/// <summary>
	/// 管理员使用的组合角色。
	/// </summary>
	public const string Admin = "SU,AD";

	/// <summary>
	/// 获取所有已定义的角色名称。
	/// </summary>
	public static IEnumerable<string> All => [SuperUser, SystemAdmin, Helpdesk, NormalUser, Developer, Tester, ProjectManager];
}