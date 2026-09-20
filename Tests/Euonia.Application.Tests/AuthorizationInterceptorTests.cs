using System.Security.Authentication;
using System.Security.Claims;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests;

public interface IAdminService
{
	string GetSecret();
}

/// <summary>
/// 类级授权：整个服务要求管理员角色。
/// </summary>
[Authorize(Roles = "admin")]
public class AdminService : BaseApplicationService, IAdminService
{
	public virtual string GetSecret() => "secret";
}

public interface IPublicService
{
	[Authorize(Roles = "user")]
	string GetForUser();
}

public class PublicService : BaseApplicationService, IPublicService
{
	public virtual string GetForUser() => "for-user";
}

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

	[Fact]
	public void ClassLevelAuthorize_AuthenticatedAdmin_ShouldPass()
	{
		using var provider = CreateProvider(CreateUser("admin"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Equal("secret", svc.GetSecret());
	}

	[Fact]
	public void ClassLevelAuthorize_AuthenticatedNonAdmin_ShouldThrow()
	{
		using var provider = CreateProvider(CreateUser("user"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Throws<UnauthorizedAccessException>(() => svc.GetSecret());
	}

	[Fact]
	public void ClassLevelAuthorize_Anonymous_ShouldThrow()
	{
		using var provider = CreateProvider(new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity())));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IAdminService>();

		Assert.Throws<AuthenticationException>(() => svc.GetSecret());
	}

	[Fact]
	public void MethodLevelAuthorize_WithExpectedRole_ShouldPass()
	{
		using var provider = CreateProvider(CreateUser("user"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IPublicService>();

		Assert.Equal("for-user", svc.GetForUser());
	}

	[Fact]
	public void MethodLevelAuthorize_WrongRole_ShouldThrow()
	{
		using var provider = CreateProvider(CreateUser("admin"));
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IPublicService>();

		Assert.Throws<UnauthorizedAccessException>(() => svc.GetForUser());
	}
}