namespace Nerosoft.Euonia.Sample.Constants;

/// <summary>系统初始化（Bootstrap）内置账号与归属的常量。</summary>
public static class BootstrapUsers
{
	/// <summary>内置管理员账号标识（对应 <c>user.id</c> 与授权行的 <c>user_id</c>）。</summary>
	public const string AdminId = "admin";

	/// <summary>内置管理员所属团队（bootstrap 阶段唯一的团队，业务团队由运行时创建）。</summary>
	public const string AdminTeamId = "T-1";
}