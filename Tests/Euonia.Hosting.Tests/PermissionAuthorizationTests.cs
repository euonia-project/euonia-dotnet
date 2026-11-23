using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 针对权限特性、策略提供程序与授权处理器的测试。
/// </summary>
public class PermissionAuthorizationTests
{
	/// <summary>
	/// 特性应把权限码编码进策略名。
	/// </summary>
	[Fact]
	public void RequiresPermissionAttribute_EncodesPolicyName()
	{
		var attribute = new RequiresPermissionAttribute("account.user.create", "account.user.update");

		Assert.Equal(RequiresPermissionAttribute.PolicyPrefix + "account.user.create|account.user.update", attribute.Policy);
		Assert.Equal(["account.user.create", "account.user.update"], attribute.Permissions);
	}

	/// <summary>
	/// 空权限码应被拒绝。
	/// </summary>
	[Fact]
	public void RequiresPermissionAttribute_EmptyPermissions_Throws()
	{
		Assert.Throws<ArgumentException>(() => new RequiresPermissionAttribute());
		Assert.Throws<ArgumentException>(() => new RequiresPermissionAttribute(" "));
	}

	/// <summary>
	/// 策略提供程序应把权限策略名还原为带权限要求的策略。
	/// </summary>
	[Fact]
	public async Task PermissionPolicyProvider_ResolvesPermissionPolicy()
	{
		var fallback = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
		var provider = new PermissionPolicyProvider(fallback);

		var policy = await provider.GetPolicyAsync(RequiresPermissionAttribute.PolicyPrefix + "a|b");

		Assert.NotNull(policy);
		var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
		Assert.Equal(["a", "b"], requirement.Permissions);
	}

	/// <summary>
	/// 非权限策略名应交给默认提供程序。
	/// </summary>
	[Fact]
	public async Task PermissionPolicyProvider_OtherPolicy_ReturnsFallback()
	{
		var options = new AuthorizationOptions();
		options.AddPolicy("known", policy => policy.RequireAuthenticatedUser());
		var fallback = new DefaultAuthorizationPolicyProvider(Options.Create(options));
		var provider = new PermissionPolicyProvider(fallback);

		var policy = await provider.GetPolicyAsync("known");

		Assert.NotNull(policy);
		Assert.DoesNotContain(policy.Requirements, requirement => requirement is PermissionRequirement);
	}

	/// <summary>
	/// 已认证且持有任一权限码时，处理器应成功。
	/// </summary>
	[Fact]
	public async Task PermissionAuthorizationHandler_HoldingPermission_Succeeds()
	{
		var guard = new Mock<IScopeGuard>();
		guard.Setup(item => item.EnsureResolvedAsync(It.IsAny<CancellationToken>()))
		     .Returns(ValueTask.CompletedTask);
		guard.Setup(item => item.GetSubjects())
		     .Returns(ScopeSubjectSet.CreateBuilder().AddCode("account.user.create").Build());

		var accessor = new Mock<IHttpContextAccessor>();
		accessor.SetupGet(item => item.HttpContext).Returns(new DefaultHttpContext());

		var handler = new PermissionAuthorizationHandler(guard.Object, accessor.Object);
		var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "tester")], "test"));
		var context = new AuthorizationHandlerContext([new PermissionRequirement(["account.user.create"])], user, null);

		await handler.HandleAsync(context);

		Assert.True(context.HasSucceeded);
	}

	/// <summary>
	/// 未认证主体不应通过权限判定。
	/// </summary>
	[Fact]
	public async Task PermissionAuthorizationHandler_Anonymous_DoesNotSucceed()
	{
		var guard = new Mock<IScopeGuard>();
		var accessor = new Mock<IHttpContextAccessor>();
		var handler = new PermissionAuthorizationHandler(guard.Object, accessor.Object);
		var context = new AuthorizationHandlerContext([new PermissionRequirement(["a"])], new ClaimsPrincipal(new ClaimsIdentity()), null);

		await handler.HandleAsync(context);

		Assert.False(context.HasSucceeded);
	}

	/// <summary>
	/// 注册后应能解析权限策略提供程序与授权处理器。
	/// </summary>
	[Fact]
	public void AddPermissionGuard_RegistersPolicyProviderAndHandler()
	{
		var services = new ServiceCollection();

		services.AddPermissionGuard();

		using var provider = services.BuildServiceProvider();

		Assert.IsType<PermissionPolicyProvider>(provider.GetRequiredService<IAuthorizationPolicyProvider>());
		Assert.Contains(provider.GetServices<IAuthorizationHandler>(), item => item is PermissionAuthorizationHandler);
	}
}
