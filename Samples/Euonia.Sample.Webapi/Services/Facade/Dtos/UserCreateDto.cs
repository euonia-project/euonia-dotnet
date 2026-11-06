namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// 创建用户时提交的数据传输对象。
/// </summary>
/// <remarks>
/// 该对象仅承载用户注册所需的信息，随后由映射转换为 <c>UserCreateCommand</c> 执行创建流程。
/// </remarks>
public class UserCreateDto
{
	/// <summary>
	/// 用户名，用作登录标识并需保持唯一。
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// 明文密码，仅在创建请求中传输，持久化前会加盐哈希。
	/// </summary>
	public string Password { get; set; } = null!;

	/// <summary>
	/// 用户邮箱地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 用户手机号码。
	/// </summary>
	public string Phone { get; set; } = null!;

	/// <summary>
	/// 用户昵称，用于界面展示。
	/// </summary>
	public string Nickname { get; set; } = null!;
}
