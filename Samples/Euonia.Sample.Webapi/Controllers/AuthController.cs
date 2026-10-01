using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Toolkit;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 账号登录：以用户名 + 密码对真实账号（<see cref="UserEntity"/> 表）做身份校验并签发 JWT。
/// 令牌只携带身份与角色；权限码、团队范围与行级授予不出现在令牌中，由
/// <see cref="Nerosoft.Euonia.Sample.Domain.Permissions.ScopeSubjectResolver"/> 在请求时从授权数据解析，
/// 故撤销授权即时生效、无需重新登录。
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(IConfiguration configuration, IUserRepository users) : ControllerBase
{
	/// <summary>登录入参。</summary>
	public record LoginInput(string Username, string Password);

	/// <summary>按用户名与密码登录，成功时返回 JWT 令牌。</summary>
	[HttpPost("login")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login([FromBody] LoginInput input, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(input?.Username) || string.IsNullOrWhiteSpace(input.Password))
		{
			return BadRequest(new { code = 400, error = "用户名和密码不能为空。" });
		}

		// 按用户名查真实账号（含角色），已软删除的账号视为不存在。
		var matches = await users.FindAsync(user => user.Username == input.Username && !user.IsDeleted, ["Roles"], 0, 1, cancellationToken);
		var user = matches.FirstOrDefault();
		if (user == null)
		{
			return Unauthorized(new { code = 401, error = "用户名或密码错误。" });
		}

		if (user.LockoutEnd > DateTime.Now)
		{
			return Unauthorized(new { code = 401, error = "登录失败次数过多，账号已临时锁定，请稍后再试。", details = user.LockoutEnd });
		}

		if (!VerifyPassword(user, input.Password))
		{
			// 密码错误：累加失败次数，次数达到阈值时锁定（见 UserEntity.IncreaseAccessFailedCount）。
			user.IncreaseAccessFailedCount();
			await users.UpdateAsync(user, true, cancellationToken);
			return Unauthorized(new { code = 401, error = "用户名或密码错误。" });
		}

		if (user.AccessFailedCount != 0)
		{
			user.ResetAccessFailedCount();
			await users.UpdateAsync(user, true, cancellationToken);
		}

		var token = IssueToken(user);
		return Ok(new
		{
			token,
			userId = user.Id,
			name = user.Nickname ?? user.Username,
			roles = user.Roles?.Select(role => role.Name).ToArray() ?? []
		});
	}

	private string IssueToken(UserEntity user)
	{
		var section = configuration.GetSection("JwtAuthenticationOptions");
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["SigningKey"]));
		var issuer = section.GetSection("Issuer").Get<string[]>()?.FirstOrDefault() ?? "euonia.com";

		var claims = new List<Claim>
		{
			new(JwtClaimTypes.Subject, user.Id),
			new(JwtClaimTypes.Name, user.Nickname ?? user.Username)
		};
		if (user.Roles != null)
		{
			claims.AddRange(user.Roles.Select(role => new Claim(JwtClaimTypes.Role, role.Name)));
		}

		var token = new JwtSecurityToken(
			issuer: issuer,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(8),
			signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private static bool VerifyPassword(UserEntity user, string password)
	{
		if (string.IsNullOrWhiteSpace(user.PasswordHash))
		{
			return false;
		}

		var candidate = Cryptography.SHA.Encrypt(user.PasswordSalt + password);
		var stored = Convert.FromBase64String(user.PasswordHash);
		var input = Convert.FromBase64String(candidate);
		return stored.Length == input.Length && CryptographicOperations.FixedTimeEquals(stored, input);
	}
}