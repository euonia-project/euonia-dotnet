namespace Nerosoft.Euonia.Sample.Domain.Dtos;

public class OnetimeCodeCreatedEto
{
	/// <summary>
	/// 获取或设置验证码的发送渠道类型。
	/// </summary>
	public int Type { get; set; }

	/// <summary>
	/// 获取或设置验证码内容。
	/// </summary>
	public string Code { get; set; }

	/// <summary>
	/// 获取或设置验证码接收方（手机号码或邮箱地址）。
	/// </summary>
	public string Recipient { get; set; }

	/// <summary>
	/// 获取或设置验证码的用途场景。
	/// </summary>
	public int Usage { get; set; }

	/// <summary>
	/// 获取或设置验证码的有效时长。
	/// </summary>
	public TimeSpan Timeout { get; set; }
}