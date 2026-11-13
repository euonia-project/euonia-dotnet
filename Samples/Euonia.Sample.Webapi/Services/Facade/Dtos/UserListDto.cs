namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// 用户列表项的数据传输对象，用于分页列表展示。
/// </summary>
public class UserListDto
{
	/// <summary>
	/// 用户的唯一标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 用户的登录名。
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// 用户的昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// 用户的邮箱地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 用户的手机号码。
	/// </summary>
	public string Phone { get; set; } = null!;
}
