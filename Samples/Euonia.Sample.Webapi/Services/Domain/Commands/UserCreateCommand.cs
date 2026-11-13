using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 创建用户命令：把账号资料与初始口令交给用户工厂创建聚合，口令在落库时加盐哈希。
/// </summary>
internal sealed class UserCreateCommand : Command
{
	/// <summary>
	/// 用户名。
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// 初始口令明文，仅在聚合内暂存用于强度校验与哈希计算。
	/// </summary>
	public string Password { get; set; } = null!;

	/// <summary>
	/// 电子邮件地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 联系电话。
	/// </summary>
	public string Phone { get; set; } = null!;

	/// <summary>
	/// 获取或设置用户昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;
}

