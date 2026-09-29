namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// <see cref="UserPrincipal"/> 的空主体护栏：文档承诺「底层 <c>ClaimsPrincipal</c> 为 null 时返回 null」，
/// 实现必须与之相符，而不是先抛 <see cref="NullReferenceException"/>。
/// </summary>
public class UserPrincipalRegressionTests
{
	[Fact]
	public void Accessors_Should_Return_Defaults_When_Claims_Principal_Is_Missing()
	{
		var principal = new UserPrincipal(null);

		Assert.Null(principal.Username);
		Assert.Null(principal.UserId);
		Assert.Null(principal.Code);
		Assert.False(principal.IsAuthenticated);
	}
}
