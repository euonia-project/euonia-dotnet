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
		provider.ValidatePermissionSetup();
	}

	[Fact]
	public void AddPermission_Should_Register_The_Engine()
	{
		var services = NewServicesWithUser();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ObjectPermissionOptInTests).Assembly);

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

		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ObjectPermissionOptInTests).Assembly);
		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IScopeGuard>());
		Assert.NotNull(provider.GetService<IObjectFactory>());
	}

	[Fact]
	public void AddPermission_Should_Not_Override_Developer_KeyResolver()
	{
		var services = NewServices();

		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ObjectPermissionOptInTests).Assembly);
		services.AddSingleton<IScopeKeyResolver, FixedKeyResolver>();

		var provider = services.BuildServiceProvider();

		Assert.IsType<FixedKeyResolver>(provider.GetService<IScopeKeyResolver>());
	}

	[Fact]
	public void Missing_Resolver_Should_Fail_Validation_Not_Silently_Permit()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ObjectPermissionOptInTests).Assembly);
		var provider = services.BuildServiceProvider();

		var ex = Assert.Throws<InvalidOperationException>(() => provider.ValidatePermissionSetup());
		Assert.Contains(nameof(IScopeSubjectResolver), ex.Message, StringComparison.Ordinal);
	}

	private sealed class FixedKeyResolver : IScopeKeyResolver
	{
		public string Resolve(object resource, string explicitKey) => explicitKey ?? "fixed";
	}
}
