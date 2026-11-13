using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using System.Security.Claims;
using Xunit;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证权限体系必须显式启用：单独注册业务对象不会装配权限引擎，只有调用 <c>AddPermission</c> 才启用。
/// </summary>
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

	/// <summary>
	/// 验证仅注册业务对象时不会装配范围守卫、权限检查器与范围模型注册表等权限组件。
	/// </summary>
	[Fact]
	public void AddBusinessObject_Alone_Should_Not_Register_Permission_Engine()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		Assert.Null(provider.GetService<IScopeGuard>());
		Assert.Null(provider.GetService<IPermissionChecker>());
		Assert.Null(provider.GetService<ScopeModelRegistry>());

		// Osba 自己的权限契约同样不由 AddBusinessObject 注册：
		// 没有来源、没有判定实现，工厂边界在「声明了要求」时会报错而不是静默放行
		Assert.Null(provider.GetService<IPermissionCodeSource>());
		Assert.Null(provider.GetService<IPermissionChecker>());
		Assert.Null(provider.GetService<IObjectScopeAuthorizer>());
	}

	/// <summary>
	/// 验证仅注册业务对象时不会触发任何权限校验，也不会因缺少权限组件而失败。
	/// </summary>
	[Fact]
	public void AddBusinessObject_Alone_Should_Not_Fail_Validation()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		// 未启用权限体系：没有守卫可解析，也没有任何校验会触发
		Assert.Null(provider.GetService<IScopeGuard>());
	}

	/// <summary>
	/// 验证调用 <c>AddPermission</c> 后会装配范围守卫、权限检查器与范围模型注册表。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Register_The_Engine()
	{
		var services = NewServicesWithUser();
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IScopeGuard>());
		Assert.NotNull(provider.GetService<IPermissionChecker>());
		Assert.NotNull(provider.GetService<ScopeModelRegistry>());
	}

	/// <summary>
	/// 验证先启用权限、后注册业务对象的顺序同样能装配出守卫与对象工厂。
	/// </summary>
	[Fact]
	public void AddPermission_Before_AddBusinessObject_Should_Work_Too()
	{
		var services = NewServicesWithUser();
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());

		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IScopeGuard>());
		Assert.NotNull(provider.GetService<IObjectFactory>());
	}

	/// <summary>
	/// 验证开发者注册的操作解析器优先级更高，不会被权限默认实现覆盖。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Not_Override_Developer_Operation_Resolver()
	{
		var services = NewServices();

		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddSingleton<IObjectOperationResolver, FixedOperationResolver>();

		var provider = services.BuildServiceProvider();

		Assert.IsType<FixedOperationResolver>(provider.GetService<IObjectOperationResolver>());
	}

	/// <summary>
	/// 验证缺少主体解析器时解析范围守卫会抛出 <see cref="InvalidOperationException"/>，而不是静默放行。
	/// </summary>
	[Fact]
	public void Missing_Resolver_Should_Fail_Validation_Not_Silently_Permit()
	{
		var services = NewServices();

		services.AddBusinessObject(typeof(ObjectPermissionOptInTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ObjectPermissionOptInTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		var provider = services.BuildServiceProvider();

		var ex = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);
		Assert.Contains(nameof(IScopeSubjectResolver), ex.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 始终返回固定操作名 <c>fixed</c> 的操作解析器替身。
	/// </summary>
	private sealed class FixedOperationResolver : IObjectOperationResolver
	{
		public bool TryResolve(object resource, out string operation)
		{
			operation = "fixed";
			return true;
		}
	}

	/// <summary>
	/// 返回空范围主体集合的解析器替身，使守卫装配测试不依赖具体授权数据。
	/// </summary>
	private sealed class FixedSubjectResolver : IScopeSubjectResolver
	{
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
			=> ValueTask.FromResult(ScopeSubjectSet.Empty);
	}
}
