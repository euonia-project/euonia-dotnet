using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nerosoft.Euonia.Hosting.Azfunc;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对 Azure Functions 宿主选项、注册与函数基类的测试。
/// </summary>
public class EuoniaFunctionsTests
{
	/// <summary>
	/// 默认选项应启用异常处理与 HTTP 上下文中转。
	/// </summary>
	[Fact]
	public void Options_Defaults_AreEnabled()
	{
		var options = new EuoniaFunctionsOptions();

		Assert.True(options.EnableExceptionHandling);
		Assert.True(options.EnableHttpContext);
		Assert.True(options.EnableRequestTrace);
		Assert.False(options.EnableRateLimiting);
		Assert.NotNull(options.RateLimiting);
	}

	/// <summary>
	/// 非法的限流配置应在注册时失败。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctions_InvalidRateLimiting_Throws()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			services.AddEuoniaFunctions(options => options.RateLimiting.MinutePermitLimit = 0));
	}

	/// <summary>
	/// 注册后应能解析选项与 HTTP 上下文访问器。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctions_RegistersOptionsAndHttpContextAccessor()
	{
		var services = new ServiceCollection();

		services.AddEuoniaFunctions(options => options.EnableExceptionHandling = false);

		using var provider = services.BuildServiceProvider();

		Assert.False(provider.GetRequiredService<EuoniaFunctionsOptions>().EnableExceptionHandling);
		Assert.NotNull(provider.GetRequiredService<IHttpContextAccessor>());
	}

	/// <summary>
	/// <see cref="FunctionBase{T}.ExecuteAsync{TResult}(Func{Task{TResult}})"/> 应把结果包装为 200 对象结果。
	/// </summary>
	[Fact]
	public async Task ExecuteAsync_WrapsResultInOkObjectResult()
	{
		var function = new TestFunction();
		var result = await function.InvokeAsync(() => Task.FromResult(42));

		var objectResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(42, objectResult.Value);
	}

	/// <summary>
	/// <see cref="FunctionBase{T}.Ok(object)"/> 应返回对象结果。
	/// </summary>
	[Fact]
	public void Ok_ReturnsObjectResult()
	{
		var function = new TestFunction();
		var result = function.InvokeOk("value");

		var objectResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal("value", objectResult.Value);
	}

	/// <summary>
	/// 注册函数宿主时应从 HTTP 上下文构建用户主体。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctions_RegistersUserPrincipalFromHttpContext()
	{
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(UserClaimTypes.Subject, "user-1")], "Bearer"))
		};
		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });

		services.AddEuoniaFunctions();

		using var provider = services.BuildServiceProvider();

		Assert.Equal("user-1", provider.GetRequiredService<UserPrincipal>().UserId);
	}

	/// <summary>
	/// 启用权限体系后应能解析 <see cref="IPermissionChecker"/>。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctionsSecurity_RegistersPermissionChecker()
	{
		var services = new ServiceCollection();

		services.AddEuoniaFunctionsSecurity(options => options.NoModels());

		using var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetRequiredService<IPermissionChecker>());
	}

	/// <summary>
	/// 启用 JWT 但未配置签名密钥时应失败。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctions_JwtWithoutSigningKey_Throws()
	{
		var services = new ServiceCollection();

		Assert.ThrowsAny<ArgumentException>(() =>
			services.AddEuoniaFunctions(options => options.EnableJwtAuthentication = true));
	}

	/// <summary>
	/// 启用 JWT 且要求受众校验但未配置受众时应失败。
	/// </summary>
	[Fact]
	public void AddEuoniaFunctions_JwtWithoutAudience_Throws()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentException>(() =>
			services.AddEuoniaFunctions(options =>
			{
				options.EnableJwtAuthentication = true;
				options.JwtAuthentication.SigningKey = "0123456789abcdef0123456789abcdef";
				options.JwtAuthentication.Issuer = ["issuer"];
			}));
	}

	/// <summary>
	/// 有效令牌应通过校验并保留角色声明。
	/// </summary>
	[Fact]
	public void JwtTokenValidator_ValidToken_ReturnsPrincipal()
	{
		var options = CreateJwtOptions();
		var token = CreateToken(options, "user-1");

		var principal = JwtTokenValidator.Validate(token, options);

		Assert.True(principal.Identity?.IsAuthenticated);
		Assert.Equal("user-1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
		Assert.True(principal.IsInRole("admin"));
	}

	/// <summary>
	/// 签名不匹配的令牌应被拒绝。
	/// </summary>
	[Fact]
	public void JwtTokenValidator_InvalidSignature_Throws()
	{
		var options = CreateJwtOptions();
		var token = CreateToken(options, "user-1");
		options.SigningKey = "ffffffffffffffffffffffffffffffff";

		Assert.Throws<AuthenticationException>(() => JwtTokenValidator.Validate(token, options));
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

	private static string CreateToken(JwtAuthenticationOptions options, string userId)
	{
		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.SigningKey)),
			SecurityAlgorithms.HmacSha256);

		var descriptor = new SecurityTokenDescriptor
		{
			Issuer = options.Issuer!.First(),
			Audience = options.Audience,
			Subject = new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userId),
				new Claim(ClaimTypes.Role, "admin")
			]),
			Expires = DateTime.UtcNow.AddMinutes(5),
			SigningCredentials = credentials
		};

		return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
	}

	/// <summary>
	/// 应按程序集路径与入口点解析函数方法。
	/// </summary>
	[Fact]
	public void FunctionMethodResolver_ResolvesEntryPoint()
	{
		var method = FunctionMethodResolver.Resolve(
			typeof(EuoniaFunctionsTests).Assembly.Location,
			typeof(EuoniaFunctionsTests).FullName + "." + nameof(Marker));

		Assert.NotNull(method);
		Assert.Equal(nameof(Marker), method.Name);
	}

	/// <summary>
	/// 供入口点解析测试使用。
	/// </summary>
	public static void Marker()
	{
	}

	private sealed class TestFunction() : FunctionBase<TestFunction>(NullLoggerFactory.Instance)
	{
		public Task<IActionResult> InvokeAsync(Func<Task<int>> action)
		{
			return ExecuteAsync(action);
		}

		public IActionResult InvokeOk(object? value)
		{
			return Ok(value);
		}
	}
}
