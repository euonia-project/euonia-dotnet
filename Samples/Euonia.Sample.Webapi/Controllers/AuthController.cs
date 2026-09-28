using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nerosoft.Euonia.Sample.Domain.Permissions;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 演示用令牌签发：令牌携带身份（subject/name）与角色（role，来自授权数据）。
/// 权限码与行级授予不出现在令牌中，由
/// <see cref="DemoScopeSubjectResolver"/> 在请求时从授权数据解析。
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(IConfiguration configuration, DemoAuthorizationStore store) : ControllerBase
{
	/// <summary>登录入参。</summary>
	public record LoginInput(string UserId);

	/// <summary>按演示用户签发令牌。</summary>
	/// <param name="input">要登录的演示用户标识（u-1/u-2/u-3）。</param>
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginInput input, CancellationToken cancellationToken)
	{
		var userId = input?.UserId;
		var authorization = string.IsNullOrWhiteSpace(userId) ? null : await store.GetAsync(userId, cancellationToken);
		if (authorization == null)
		{
			return NotFound($"Unknown demo user '{userId}'.");
		}

		var section = configuration.GetSection("JwtAuthenticationOptions");
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["SigningKey"]));
		var issuer = section.GetSection("Issuer").Get<string[]>()?.FirstOrDefault() ?? "euonia.com";

		var claims = new List<Claim>
		{
			new(JwtClaimTypes.Subject, userId),
			new(JwtClaimTypes.Name, authorization.Name),
		};
		claims.AddRange(authorization.Roles.Select(role => new Claim(JwtClaimTypes.Role, role)));

		var token = new JwtSecurityToken(
			issuer: issuer,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(1),
			signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

		return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), userId, name = authorization.Name, roles = authorization.Roles });
	}
}