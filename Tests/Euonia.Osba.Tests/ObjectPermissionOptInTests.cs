using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using System.Security.Claims;
using Xunit;

namespace Nerosoft.Euonia.Osba.Tests;

public class ObjectPermissionOptInTests
{
	private static IServiceCollection NewServices() => new ServiceCollection();

	private static IServiceCollection NewServicesWithUser()
	{
		var services = NewServices();

		services.AddSingleton(new UserPrincipal(new ClaimsPrincipal(
			new ClaimsIdentity(authenticationType: "Bearer"))));

		return services;
	}

	[Fact]
	public void AddBusinessObject_Alone_Should_Not_Register_Permission_Engine()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		Assert.Null(provider.GetService<IScopeGuard>());
		Assert.Null(provider.GetService<IPermissionChecker>());
		Assert.Null(provider.GetService<ScopeModelRegistry>());
		Assert.Null(provider.GetService<IScopeKeyResolver>());

		// Osba 自己的权限契约同样不由 AddBusinessObject 注册：
		// 没有来源、没有判定实现，工厂边界在「声明了要求」时会报错而不是静默放行
		Assert.Null(provider.GetService<IPermissionCodeSource>());
		Assert.Null(provider.GetService<IPermissionChecker>());
		Assert.Null(provider.GetService<IObjectScopeAuthorizer>());
	}

	[Fact]
	public void AddBusinessObject_Alone_Should_Not_Fail_Validation()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		// 未启用权限体系：没有守卫可解析，也没有任何校验会触发
		Assert.Null(provider.GetService<IScopeGuard>());
	}

	[Fact]
	public void AddPermission_Should_Register_The_Engine()
	{
		var services = NewServicesWithUser();
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionRequirementProvider.Instance); });

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IScopeGuard>());
		Assert.NotNull(provider.GetService<IPermissionChecker>());
		Assert.NotNull(provider.GetService<ScopeModelRegistry>());
		Assert.NotNull(provider.GetService<IScopeKeyResolver>());
	}

	[Fact]
	public void AddPermission_Before_AddBusinessObject_Should_Work_Too()
	{
		var services = NewServicesWithUser();
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());

		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionRequirementProvider.Instance); });
		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IScopeGuard>());
		Assert.NotNull(provider.GetService<IObjectFactory>());
	}

	[Fact]
	public void AddPermission_Should_Not_Override_Developer_KeyResolver()
	{
		var services = NewServices();

		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionRequirementProvider.Instance); });
		services.AddSingleton<IScopeKeyResolver, FixedKeyResolver>();

		var provider = services.BuildServiceProvider();

		Assert.IsType<FixedKeyResolver>(provider.GetService<IScopeKeyResolver>());
	}

	[Fact]
	public void Missing_Resolver_Should_Fail_Validation_Not_Silently_Permit()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionRequirementProvider.Instance); });
		var provider = services.BuildServiceProvider();

		var ex = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);
		Assert.Contains(nameof(IScopeSubjectResolver), ex.Message, StringComparison.Ordinal);
	}

	private sealed class FixedKeyResolver : IScopeKeyResolver
	{
		public string Resolve(object resource, string explicitKey) => explicitKey ?? "fixed";
	}

	private sealed class FixedSubjectResolver : IScopeSubjectResolver
	{
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
			=> ValueTask.FromResult(ScopeSubjectSet.Empty);
	}
}
