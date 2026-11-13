namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// 当前用户的密码修改输入。
/// </summary>
public class UserChangePasswordDto
{
	/// <summary>
	/// 获取或设置当前密码，用于证明对账户的所有权。
	/// </summary>
	public string OldPassword { get; set; } = null!;

	/// <summary>
	/// 获取或设置新密码。
	/// </summary>
	public string NewPassword { get; set; } = null!;
}