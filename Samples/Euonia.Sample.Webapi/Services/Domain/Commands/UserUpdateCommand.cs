using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 更新用户资料命令（昵称 / 邮箱 / 手机号）。
/// </summary>
public class UserUpdateCommand : Command
{
	/// <summary>
	/// 获取或设置要更新的用户标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 获取或设置昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// 获取或设置电子邮箱地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 获取或设置电话号码。
	/// </summary>
	public string Phone { get; set; } = null!;
}