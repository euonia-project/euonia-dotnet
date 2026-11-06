using System.Security.Authentication;
using System.Security.Claims;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 类级授权测试使用的管理员服务契约。
/// </summary>
public interface IAdminService
{
	/// <summary>
	/// 获取仅管理员角色可访问的机密字符串。
	/// </summary>
	/// <returns>机密字符串。</returns>
	string GetSecret();
}

/// <summary>
/// 类级授权：整个服务要求管理员角色。
/// </summary>
[Authorize(Roles = "admin")]
public class AdminService : BaseApplicationService, IAdminService
{
	/// <summary>
	/// 返回机密字符串；实际访问权限由类级 <see cref="AuthorizeAttribute"/> 在拦截器中校验。
	/// </summary>
	/// <returns>机密字符串。</returns>
	public virtual string GetSecret() => "secret";
}

/// <summary>
/// 方法级授权测试使用的服务契约，要求调用者具备 user 角色。
/// </summary>
public interface IPublicService
{
	/// <summary>
	/// 获取仅 user 角色可访问的内容。
	/// </summary>
	/// <returns>当前用户可访问的内容。</returns>
	[Authorize(Roles = "user")]
	string GetForUser();
}

/// <summary>
/// 方法级授权测试使用的服务实现，授权要求声明在接口方法上。
/// </summary>
public class PublicService : BaseApplicationService, IPublicService
{
	/// <summary>
	/// 返回 user 角色可访问的内容，调用前由授权拦截器校验角色。
	/// </summary>
	/// <returns>当前用户可访问的内容。</returns>
	public virtual string GetForUser() => "for-user";
}

/// <summary>
/// 验证 <see cref="AuthorizationInterceptor"/> 对类级与方法级授权的校验，以及认证缺失或角色不符时抛出的异常。
/// </summary>
public class AuthorizationInterceptorTests
{
	private static ServiceProvider CreateProvider(UserPrincipal user)
	{
		var services = new ServiceCollection();
		services.AddScoped(_ => user);
		services.AddSingleton<ProxyGenerator>();
		services.AddTransient<IInterceptor, AuthorizationInterceptor>();
		services.AddApplicationService(typeof(AdminService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}

	private static UserPrincipal CreateUser(params string[] roles)
	{
		var claims = roles.Select(role => new Claim(UserClaimTypes.Role, role)).ToList();
		var identity = new ClaimsIdentity(claims, "Bearer");
		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	/// <summary>
	/// 已认证且具备 admin 角色的调用者访问类级授权服务时应成功返回结果。
	/// </summary>
	[Fact]
	public void ClassLevelAuthorize_AuthenticatedAdmin_ShouldPass()
	{
		using var provider = CreateProvider(CreateUser("admin"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Equal("secret", svc.GetSecret());
	}

	/// <summary>
	/// 已认证但角色不匹配的调用者访问类级授权服务时应抛出 <see cref="UnauthorizedAccessException"/>。
	/// </summary>
	[Fact]
	public void ClassLevelAuthorize_AuthenticatedNonAdmin_ShouldThrow()
	{
		using var provider = CreateProvider(CreateUser("user"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Throws<UnauthorizedAccessException>(() => svc.GetSecret());
	}

	/// <summary>
	/// 未认证的调用者访问类级授权服务时应抛出 <see cref="AuthenticationException"/>。
	/// </summary>
	[Fact]
	public void ClassLevelAuthorize_Anonymous_ShouldThrow()
	{
		using var provider = CreateProvider(new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity())));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Throws<AuthenticationException>(() => svc.GetSecret());
	}

	/// <summary>
	/// 具备接口方法所要求 user 角色的调用者应成功调用该方法。
	/// </summary>
	[Fact]
	public void MethodLevelAuthorize_WithExpectedRole_ShouldPass()
	{
		using var provider = CreateProvider(CreateUser("user"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IPublicService>();

		Assert.Equal("for-user", svc.GetForUser());
	}

	/// <summary>
	/// 角色与接口方法要求不符的调用者应收到 <see cref="UnauthorizedAccessException"/>。
	/// </summary>
	[Fact]
	public void MethodLevelAuthorize_WrongRole_ShouldThrow()
	{
		using var provider = CreateProvider(CreateUser("admin"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IPublicService>();

		Assert.Throws<UnauthorizedAccessException>(() => svc.GetForUser());
	}
}