using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Hosting.Webapi.Tests;

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

	/// <summary>
	/// 含策略分隔符 <c>|</c> 的权限码应在构造时被拒绝：否则策略提供程序会把 "a|b" 拆成
	/// “持有 a 或 b 即通过”，静默放宽判定（fail-open）。
	/// </summary>
	[Fact]
	public void RequiresPermissionAttribute_CodeWithSeparator_Throws()
	{
		var exception = Assert.Throws<ArgumentException>(() => new RequiresPermissionAttribute("a|b"));

		Assert.Contains("|", exception.Message, StringComparison.Ordinal);
		Assert.Throws<ArgumentException>(() => new RequiresPermissionAttribute("a", "b|c"));
	}

	/// <summary>
	/// 框架保留前缀 <c>@</c> 的码应在构造时被拒绝，与启动期声明扫描（ScopeKeys.Validate）同一口径；
	/// 首尾空白不得绕过保留前缀校验。
	/// </summary>
	[Fact]
	public void RequiresPermissionAttribute_ReservedPrefix_Throws()
	{
		Assert.Throws<InvalidOperationException>(() => new RequiresPermissionAttribute("@x"));
		Assert.Throws<InvalidOperationException>(() => new RequiresPermissionAttribute(" @x"));
	}

	/// <summary>
	/// 权限码首尾空白应在构造时裁剪，策略名与 Permissions 都用裁剪后的码。
	/// </summary>
	[Fact]
	public void RequiresPermissionAttribute_TrimsCodes()
	{
		var attribute = new RequiresPermissionAttribute(" account.user.create ");

		Assert.Equal(RequiresPermissionAttribute.PolicyPrefix + "account.user.create", attribute.Policy);
		Assert.Equal(["account.user.create"], attribute.Permissions);
	}

	/// <summary>
	/// 空白权限码项应在构造时被拒绝（与特性口径对齐）；null 集合抛 ArgumentNullException。
	/// </summary>
	[Fact]
	public void PermissionRequirement_WhitespaceCode_Throws()
	{
		Assert.Throws<ArgumentException>(() => new PermissionRequirement(["a", " "]));
		Assert.Throws<ArgumentException>(() => new PermissionRequirement([null!]));
		Assert.Throws<ArgumentNullException>(() => new PermissionRequirement(null!));
	}

	/// <summary>
	/// 权限码集合应是构造时的防御性副本且只读：策略实例被静态缓存，
	/// 构造后修改源数组不得污染要求。
	/// </summary>
	[Fact]
	public void PermissionRequirement_DefensivelyCopiesCodes()
	{
		var codes = new[] { "a", "b" };
		var requirement = new PermissionRequirement(codes);

		codes[0] = "evil";

		Assert.Equal(["a", "b"], requirement.Permissions);
		Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<string>>(requirement.Permissions);
	}

	/// <summary>
	/// 宿主预注册的策略提供程序不应被 AddPermissionGuard 覆盖（TryAdd 语义）。
	/// </summary>
	[Fact]
	public void AddPermissionGuard_ShouldNotOverwriteHostPolicyProvider()
	{
		var services = new ServiceCollection();
		var hostProvider = new StubPolicyProvider();
		services.AddSingleton<IAuthorizationPolicyProvider>(hostProvider);

		services.AddPermissionGuard();

		using var provider = services.BuildServiceProvider();

		Assert.Same(hostProvider, provider.GetRequiredService<IAuthorizationPolicyProvider>());
	}

	/// <summary>
	/// 宿主自定义策略提供程序的桩实现：仅用于验证注册不被覆盖。
	/// </summary>
	private sealed class StubPolicyProvider : IAuthorizationPolicyProvider
	{
		/// <inheritdoc />
		public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
		{
			return Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
		}

		/// <inheritdoc />
		public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
		{
			return Task.FromResult<AuthorizationPolicy?>(null);
		}

		/// <inheritdoc />
		public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
		{
			return Task.FromResult<AuthorizationPolicy?>(null);
		}
	}
}
