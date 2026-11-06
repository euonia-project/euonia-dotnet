namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// 用户详情（读模型）。
/// </summary>
public class UserDetailDto
{
	/// <summary>
	/// 获取或设置用户的唯一标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 获取或设置登录用户名。
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// 获取或设置用户的昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// 获取或设置用户的邮箱地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 获取或设置用户的手机号码。
	/// </summary>
	public string Phone { get; set; } = null!;

	/// <summary>
	/// 获取或设置密码最后一次修改的时间。
	/// </summary>
	public DateTime? PasswordChangedTime { get; set; }

	/// <summary>
	/// 获取或设置创建时间。
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// 获取或设置最后更新时间。
	/// </summary>
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// 获取或设置授予该用户的角色名称。
	/// </summary>
	public string[] Roles { get; set; } = null!;
}