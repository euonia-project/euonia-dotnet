using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 使用 <see cref="JwtAuthenticationOptions"/> 校验 JWT 并返回声明主体。
/// </summary>
internal static class JwtTokenValidator
{
	/// <summary>
	/// 校验令牌；失败时抛出 <see cref="AuthenticationException"/>。
	/// </summary>
	/// <param name="token">JWT 字符串。</param>
	/// <param name="options">JWT 认证选项。</param>
	/// <returns>校验通过的声明主体。</returns>
	/// <exception cref="AuthenticationException">令牌无效、过期或签名不匹配。</exception>
	public static ClaimsPrincipal Validate(string token, JwtAuthenticationOptions options)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(token);
		ArgumentNullException.ThrowIfNull(options);

		var parameters = new TokenValidationParameters
		{
			NameClaimType = options.NameClaimType,
			RoleClaimType = options.RoleClaimType,
			ValidIssuers = options.Issuer,
			ValidateIssuer = options.ValidateIssuer,
			ValidateAudience = options.ValidateAudience,
			ValidAudience = options.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
			ValidateIssuerSigningKey = true,
			ValidateLifetime = true,
			RequireExpirationTime = true,
			ClockSkew = TimeSpan.FromMinutes(1)
		};

		try
		{
			return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
		}
		catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
		{
			throw new AuthenticationException("无效的访问令牌。", exception);
		}
	}
}
