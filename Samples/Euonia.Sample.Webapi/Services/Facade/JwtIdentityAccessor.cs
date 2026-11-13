using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// 从 JWT 授权头中解析用户身份。
/// </summary>
internal class JwtIdentityAccessor
{
	/// <summary>
	/// 从提供的 JWT 授权头中解析 <see cref="IPrincipal"/>。
	/// </summary>
	/// <param name="jwt">
	/// 期望格式为 "Bearer {token}" 的授权头值。
	/// 若为 <c>null</c> 或不包含令牌，则方法返回空主体。
	/// </param>
	/// <param name="configuration">
	/// 应用程序的 <see cref="IConfiguration"/> 实例。该方法从 <c>JwtAuthenticationOptions</c> 节读取
	/// 以下设置：
	/// <list type="bullet">
	/// <item><description><c>SigningKey</c> (string) — 对称签名密钥。</description></item>
	/// <item><description><c>Issuer</c> (string[]) — 有效的颁发者。</description></item>
	/// <item><description><c>ValidateIssuer</c> (bool) — 是否验证颁发者。</description></item>
	/// <item><description><c>ValidateAudience</c> (bool) — 是否验证受众。</description></item>
	/// </list>
	/// </param>
	/// <returns>
	/// 令牌有效时返回经过验证的 <see cref="ClaimsPrincipal"/>；否则返回空的 <see cref="ClaimsPrincipal"/>。
	/// 返回的实例实现 <see cref="IPrincipal"/>。
	/// </returns>
	/// <remarks>
	/// 该方法使用正则表达式提取令牌，查找前缀 "Bearer "。令牌验证通过
	/// <see cref="JwtSecurityTokenHandler"/> 执行。所有验证异常都会被忽略，失败时返回空主体。
	/// </remarks>
	public static IPrincipal Resolve(string jwt, IConfiguration configuration)
	{
		var token = Regex.Match(jwt ?? "Bearer ", @"^Bearer\s+(.*)").Groups[1].Value;
		ClaimsPrincipal principal;
		if (string.IsNullOrWhiteSpace(token))
		{
			principal = new ClaimsPrincipal(); //GenericPrincipal(null, null);
		}
		else
		{
			const string prefix = "JwtAuthenticationOptions";

			var signingKey = configuration.GetValue<string>($"{prefix}:SigningKey");
			var key = Encoding.UTF8.GetBytes(signingKey!);

			var validation = new TokenValidationParameters
			{
				NameClaimType = ClaimTypes.Name,
				RoleClaimType = ClaimTypes.Role,
				ValidIssuers = configuration.GetSection($"{prefix}:Issuer").Get<string[]>(),
				ValidateIssuer = configuration.GetValue<bool>($"{prefix}:ValidateIssuer"),
				ValidateAudience = configuration.GetValue<bool>($"{prefix}:ValidateAudience"),
				IssuerSigningKey = new SymmetricSecurityKey(key)
			};
			try
			{
				principal = new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
			}
			catch
			{
				principal = new ClaimsPrincipal();
			}
		}

		return principal;
	}
}
