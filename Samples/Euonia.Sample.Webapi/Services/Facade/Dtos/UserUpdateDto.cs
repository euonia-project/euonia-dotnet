namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// 用户资料更新输入（昵称 / 邮箱 / 手机号）。
/// </summary>
public class UserUpdateDto
{
	/// <summary>
	/// 获取或设置昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// 获取或设置邮箱地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 获取或设置手机号码。
	/// </summary>
	public string Phone { get; set; } = null!;
}