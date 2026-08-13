using Duende.IdentityModel;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// JWT 认证选项。
/// </summary>
public class JwtAuthenticationOptions
{
	/// <summary>
	/// 获取或设置认证方案名称。
	/// </summary>
	public string Scheme { get; set; }

	/// <summary>
	/// 获取或设置令牌颁发者。
	/// </summary>
	public IEnumerable<string> Issuer { get; set; }

	/// <summary>
	/// 获取或设置签名密钥。
	/// </summary>
	public string SigningKey { get; set; }

	/// <summary>
	/// 获取或设置颁发机构（authority）的 URL。
	/// </summary>
	public string Authority { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否要求 HTTPS 元数据。
	/// </summary>
	public bool RequireHttpsMetadata { get; set; }

	/// <summary>
	/// 获取或设置受众（audience）。
	/// </summary>
	public string Audience { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否使用策略。
	/// </summary>
	public bool UsePolicy { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否验证颁发者。
	/// </summary>
	public bool ValidateIssuer { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否验证受众。
	/// </summary>
	public bool ValidateAudience { get; set; } = true;

	/// <summary>
	/// 获取或设置定义 NameClaimType 的值。
	/// </summary>
	public string NameClaimType { get; set; } = JwtClaimTypes.Name;

	/// <summary>
	/// 获取或设置定义 RoleClaimType 的值。
	/// </summary>
	public string RoleClaimType { get; set; } = JwtClaimTypes.Role;
}