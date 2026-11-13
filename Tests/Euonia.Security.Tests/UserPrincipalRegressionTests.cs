using System.Security.Claims;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// <see cref="UserPrincipal"/> 的声明读取护栏。
/// 覆盖三类承诺：底层 <c>ClaimsPrincipal</c> 为 null 时返回默认值；
/// <c>JwtBearer</c> / <c>Windows</c> / <c>Cookies</c> / <c>Cookie</c> 各自走对应声明；
/// 未识别的认证类型回退到优先级查找（而不是直接返回 null）。
/// </summary>
public class UserPrincipalRegressionTests
{
	/// <summary>
	/// 底层 <c>ClaimsPrincipal</c> 为 <c>null</c> 时，用户名、用户标识、编码均返回 <c>null</c>，且视为未认证。
	/// </summary>
	[Fact]
	public void Accessors_Should_Return_Defaults_When_Claims_Principal_Is_Missing()
	{
		var principal = new UserPrincipal(null);

		Assert.Null(principal.Username);
		Assert.Null(principal.UserId);
		Assert.Null(principal.Code);
		Assert.False(principal.IsAuthenticated);
	}

	/// <summary>
	/// 令牌类认证（<c>Jwt</c>、<c>Bearer</c>、<c>JwtBearer</c>）下，用户名与用户标识应取自 <see cref="UserClaimTypes.Name"/> 与 <see cref="UserClaimTypes.Subject"/>。
	/// </summary>
	/// <param name="authenticationType">用于构造身份声明的认证类型。</param>
	[Theory]
	[InlineData("Jwt")]
	[InlineData("Bearer")]
	[InlineData("JwtBearer")]
	public void Username_And_UserId_Should_Read_Name_And_Subject_For_Token_Authentication(string authenticationType)
	{
		var principal = Principal(
			authenticationType,
			new Claim(UserClaimTypes.Name, "token-name"),
			new Claim(UserClaimTypes.Subject, "token-sub"));

		Assert.Equal("token-name", principal.Username);
		Assert.Equal("token-sub", principal.UserId);
	}

	/// <summary>
	/// Windows 与 Cookie 类认证（<c>Windows</c>、<c>Cookies</c>、<c>Cookie</c>）下，用户名与用户标识应取自 <see cref="ClaimTypes.Name"/> 与 <see cref="ClaimTypes.NameIdentifier"/>。
	/// </summary>
	/// <param name="authenticationType">用于构造身份声明的认证类型。</param>
	[Theory]
	[InlineData("Windows")]
	[InlineData("Cookies")]
	[InlineData("Cookie")]
	public void Username_And_UserId_Should_Read_ClaimTypes_For_Windows_And_Cookie_Authentication(string authenticationType)
	{
		var principal = Principal(
			authenticationType,
			new Claim(ClaimTypes.Name, "domain-name"),
			new Claim(ClaimTypes.NameIdentifier, "domain-id"));

		Assert.Equal("domain-name", principal.Username);
		Assert.Equal("domain-id", principal.UserId);
	}

	/// <summary>
	/// 认证类型无法识别时，用户名与用户标识应回退到优先级查找，而不是直接返回 <c>null</c>。
	/// </summary>
	[Fact]
	public void Username_And_UserId_Should_Fall_Back_When_Authentication_Type_Is_Not_Known()
	{
		var principal = Principal(
			"Basic",
			new Claim(ClaimTypes.Name, "fallback-name"),
			new Claim(ClaimTypes.NameIdentifier, "fallback-id"));

		Assert.Equal("fallback-name", principal.Username);
		Assert.Equal("fallback-id", principal.UserId);
	}

	/// <summary>
	/// 回退查找时，用户名应优先取 <see cref="UserClaimTypes.Name"/> 而非 <see cref="ClaimTypes.Name"/>。
	/// </summary>
	[Fact]
	public void Username_Should_Prefer_UserClaimTypes_Name_Over_ClaimTypes_Name_In_Fallback()
	{
		var principal = Principal(
			"Basic",
			new Claim(UserClaimTypes.Name, "oidc-name"),
			new Claim(ClaimTypes.Name, "windows-name"));

		Assert.Equal("oidc-name", principal.Username);
	}

	private static UserPrincipal Principal(string authenticationType, params Claim[] claims)
	{
		return new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType)));
	}
}
