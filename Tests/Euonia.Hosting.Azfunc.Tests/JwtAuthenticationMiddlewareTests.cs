using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对 JWT 认证中间件授权特性解析顺序的测试。
/// </summary>
public class JwtAuthenticationMiddlewareTests
{
	/// <summary>
	/// 无授权特性的匿名函数携带过期令牌仍应放行。
	/// </summary>
	[Fact]
	public async Task NoAttributes_ExpiredToken_CallsNext()
	{
		var (nextCalled, _) = await InvokeAsync(nameof(AuthFunctions.NoAttributes), CreateToken(expired: true));

		Assert.True(nextCalled);
	}

	/// <summary>
	/// 显式 [AllowAnonymous] 函数携带过期令牌仍应放行。
	/// </summary>
	[Fact]
	public async Task ExplicitAnonymous_ExpiredToken_CallsNext()
	{
		var (nextCalled, _) = await InvokeAsync(nameof(AuthFunctions.ExplicitAnonymous), CreateToken(expired: true));

		Assert.True(nextCalled);
	}

	/// <summary>
	/// 匿名函数携带有效令牌时应设置 User。
	/// </summary>
	[Fact]
	public async Task NoAttributes_ValidToken_SetsUser()
	{
		var (nextCalled, httpContext) = await InvokeAsync(nameof(AuthFunctions.NoAttributes), CreateToken());

		Assert.True(nextCalled);
		Assert.True(httpContext.User.Identity?.IsAuthenticated);
	}

	/// <summary>
	/// 匿名函数不携带令牌时应放行且不设置认证身份。
	/// </summary>
	[Fact]
	public async Task NoAttributes_NoToken_CallsNext()
	{
		var (nextCalled, httpContext) = await InvokeAsync(nameof(AuthFunctions.NoAttributes), token: null);

		Assert.True(nextCalled);
		Assert.NotEqual(true, httpContext.User.Identity?.IsAuthenticated);
	}

	/// <summary>
	/// [Authorize] 函数携带过期令牌应拒绝（401 语义）。
	/// </summary>
	[Fact]
	public async Task Protected_ExpiredToken_Throws()
	{
		await Assert.ThrowsAsync<AuthenticationException>(() => InvokeAsync(nameof(AuthFunctions.Protected), CreateToken(expired: true)));
	}

	/// <summary>
	/// [Authorize] 函数不携带令牌应拒绝（401 语义）。
	/// </summary>
	[Fact]
	public async Task Protected_NoToken_Throws()
	{
		await Assert.ThrowsAsync<AuthenticationException>(() => InvokeAsync(nameof(AuthFunctions.Protected), token: null));
	}

	/// <summary>
	/// [Authorize] 函数携带有效令牌应放行。
	/// </summary>
	[Fact]
	public async Task Protected_ValidToken_CallsNext()
	{
		var (nextCalled, _) = await InvokeAsync(nameof(AuthFunctions.Protected), CreateToken());

		Assert.True(nextCalled);
	}

	/// <summary>
	/// 角色不足应拒绝（403 语义）。
	/// </summary>
	[Fact]
	public async Task AdminOnly_WrongRole_Throws()
	{
		await Assert.ThrowsAsync<UnauthorizedAccessException>(() => InvokeAsync(nameof(AuthFunctions.AdminOnly), CreateToken(role: "user")));
	}

	/// <summary>
	/// 角色满足应放行。
	/// </summary>
	[Fact]
	public async Task AdminOnly_AdminRole_CallsNext()
	{
		var (nextCalled, _) = await InvokeAsync(nameof(AuthFunctions.AdminOnly), CreateToken(role: "admin"));

		Assert.True(nextCalled);
	}

	private static async Task<(bool NextCalled, DefaultHttpContext HttpContext)> InvokeAsync(string methodName, string? token)
	{
		var options = new EuoniaFunctionsOptions
		{
			EnableJwtAuthentication = true,
			JwtAuthentication = CreateJwtOptions()
		};
		var services = new ServiceCollection().AddSingleton(options).BuildServiceProvider();

		var httpContext = new DefaultHttpContext();

		if (token is not null)
		{
			httpContext.Request.Headers.Authorization = "Bearer " + token;
		}

		var definition = TestFunctionContext.CreateDefinition(
			"TestFunction",
			typeof(AuthFunctions).FullName + "." + methodName,
			typeof(AuthFunctions).Assembly.Location);
		var context = TestFunctionContext.Create(httpContext, services, definition);
		var middleware = new JwtAuthenticationMiddleware(NullLogger<JwtAuthenticationMiddleware>.Instance);

		var nextCalled = false;
		await middleware.Invoke(context, _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		});

		return (nextCalled, httpContext);
	}

	private static JwtAuthenticationOptions CreateJwtOptions()
	{
		return new JwtAuthenticationOptions
		{
			SigningKey = "0123456789abcdef0123456789abcdef",
			Issuer = ["test-issuer"],
			Audience = "test-audience",
			NameClaimType = ClaimTypes.Name,
			RoleClaimType = ClaimTypes.Role
		};
	}

	private static string CreateToken(bool expired = false, string role = "admin")
	{
		var options = CreateJwtOptions();
		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
			SecurityAlgorithms.HmacSha256);

		var now = DateTime.UtcNow;
		var descriptor = new SecurityTokenDescriptor
		{
			Issuer = options.Issuer!.First(),
			Audience = options.Audience,
			Subject = new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, "user-1"),
				new Claim(ClaimTypes.Name, "user-1"),
				new Claim(ClaimTypes.Role, role)
			]),
			NotBefore = now.AddMinutes(-30),
			IssuedAt = now.AddMinutes(-30),
			Expires = expired ? now.AddMinutes(-10) : now.AddMinutes(5),
			SigningCredentials = credentials
		};

		return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
	}

	private sealed class AuthFunctions
	{
		[AllowAnonymous]
		public void ExplicitAnonymous()
		{
		}

		public void NoAttributes()
		{
		}

		[Authorize]
		public void Protected()
		{
		}

		[Authorize(Roles = "admin")]
		public void AdminOnly()
		{
		}
	}
}
