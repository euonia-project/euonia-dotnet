using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// <c>AddPermission</c> 的行为：引擎的注册归属、启动期声明探测、扩展点不被覆盖。
/// </summary>
public class AddPermissionTests
{
	/// <summary>没有任何权限声明的框架程序集，用作「未使用权限」的干净扫描目标。</summary>
	private static readonly Assembly CleanAssembly = typeof(ServiceCollection).Assembly;

	private static readonly Assembly TestAssembly = typeof(AddPermissionTests).Assembly;

	/// <summary>含「方法级权限码 + 按码声明策略」的程序集，必须配合真正的权限码来源。</summary>
	private static readonly Assembly FixturesAssembly = typeof(GuardedAssetModel).Assembly;

	#region 注册归属

	[Fact]
	public void AddPermission_Should_Register_Engine_Services()
	{
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, CleanAssembly));

		Assert.NotNull(provider.GetService<ScopeModelRegistry>());
		Assert.NotNull(provider.GetService<PermissionSetup>());
		Assert.NotNull(provider.GetService<IPermissionChecker>());
		Assert.NotNull(provider.GetService<IScopeGuard>());
	}

	[Fact]
	public void AddPermission_Should_Not_Require_Object_Model()
	{
		// 只引用 Euonia.Security 的独立使用：注册、建模、判定全链路可用。
		var provider = Build(
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new Asset { Id = "1", DeptId = "team-a", OwnerId = "other", Level = "normal" }));
		Assert.False(guard.Allows(new Asset { Id = "2", DeptId = "team-x", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public void AddPermission_Should_Register_Model_Registry_As_Singleton()
	{
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly));

		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.Same(registry, provider.GetRequiredService<ScopeModelRegistry>());
	}

	[Fact]
	public void AddPermission_Should_Return_Same_Service_Collection()
	{
		var services = new ServiceCollection();

		Assert.Same(services, services.AddPermission(EmptyCodeSource.Instance, CleanAssembly));
	}

	[Fact]
	public void AddPermission_Should_Throw_On_Null_Code_Source()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentNullException>(() => services.AddPermission((IPermissionCodeSource)null, CleanAssembly));
	}

	[Fact]
	public void AddPermission_Should_Throw_On_Null_Service_Collection()
	{
		IServiceCollection services = null;

		Assert.Throws<ArgumentNullException>(() => services.AddPermission(EmptyCodeSource.Instance, CleanAssembly));
	}

	#endregion

	#region 扩展点不被覆盖

	[Fact]
	public void AddPermission_Should_Not_Overwrite_Host_Key_Resolver()
	{
		var expected = new FixedKeyResolver();
		var provider = Build(
			s => s.AddSingleton<IScopeKeyResolver>(expected),
			s => s.AddPermission(EmptyCodeSource.Instance, CleanAssembly));

		Assert.Same(expected, provider.GetRequiredService<IScopeKeyResolver>());
	}

	[Fact]
	public void AddPermission_Should_Not_Overwrite_Host_Subject_Resolver()
	{
		var expected = new FixedSubjectResolver();
		var provider = Build(
			s => s.AddSingleton<IScopeSubjectResolver>(expected),
			s => s.AddPermission(EmptyCodeSource.Instance, CleanAssembly));

		Assert.Same(expected, provider.GetRequiredService<IScopeSubjectResolver>());
	}

	[Fact]
	public void AddPermission_Should_Register_Code_Source()
	{
		var expected = new ConventionCodeSource();
		var provider = Build(s => s.AddPermission(expected, TestAssembly));

		Assert.Same(expected, provider.GetRequiredService<IPermissionCodeSource>());
	}

	[Fact]
	public void ScopeGuard_Should_Resolve_Without_Host_Key_Resolver()
	{
		// 未注册 IScopeKeyResolver 时必须回落到 ScopeKeys.Default（AssetModel 只声明了默认策略），
		// 而不是因缺少依赖而解析失败。
		var provider = Build(
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.NotNull(guard.GetPolicy<Asset>());
		Assert.True(guard.Allows(new Asset { DeptId = "team-a", OwnerId = "other", Level = "normal" }));
	}

	#endregion

	#region 启动期声明探测

	[Fact]
	public void AddPermission_Should_Not_Require_Subject_Resolver_When_Nothing_Declared()
	{
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, CleanAssembly));

		Assert.False(provider.GetRequiredService<PermissionSetup>().RequiresSubjectResolver);
		Assert.Same(provider, provider.ValidatePermissionSetup());
	}

	[Fact]
	public void AddPermission_Should_Require_Subject_Resolver_When_Declared()
	{
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly));

		Assert.True(provider.GetRequiredService<PermissionSetup>().RequiresSubjectResolver);
	}

	[Fact]
	public void ValidatePermissionSetup_Should_Throw_When_Declared_Without_Resolver()
	{
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly));

		var exception = Assert.Throws<InvalidOperationException>(() => provider.ValidatePermissionSetup());

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	[Fact]
	public void ValidatePermissionSetup_Should_Pass_When_Resolver_Registered_After_AddPermission()
	{
		// 解析器允许在 AddPermission 之后注册：只有容器定稿才能判断它是否存在。
		var provider = Build(
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		Assert.Same(provider, provider.ValidatePermissionSetup());
	}

	[Fact]
	public void AddPermission_Should_Detect_Method_Level_Permission_With_Code_Source()
	{
		var provider = Build(s => s.AddPermission(new ConventionCodeSource(), FixturesAssembly));

		Assert.True(provider.GetRequiredService<PermissionSetup>().RequiresSubjectResolver);
	}

	[Fact]
	public void AddPermission_Should_Key_Execute_From_Code_Source()
	{
		var provider = Build(s => s.AddPermission(new ConventionCodeSource(), FixturesAssembly));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	[Fact]
	public void AddPermission_Should_Key_Execute_From_Operation_When_Code_Source_Has_No_Codes()
	{
		// 没有方法级权限码时，各操作解析到各自的框架默认键。
		var provider = Build(s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly));

		Assert.Equal(ScopeKeys.Execute, ResolveExecuteKey(provider, typeof(Asset)));
	}

	[Fact]
	public void AddPermission_Should_Reject_Per_Code_Policy_Without_Code_Source()
	{
		// EmptyCodeSource 与按码声明的策略不可同用：没有任何操作能解析到应用自定义的码，
		// 死策略校验必须拒绝启动。锁定这一边界，避免日后被「放宽校验」悄悄放过。
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => Build(s => s.AddPermission(EmptyCodeSource.Instance, FixturesAssembly)));

		Assert.Equal(nameof(GuardedAssetModel), Assert.Single(exception.Diagnostics).ModelName);
	}

	[Fact]
	public void AddPermission_Should_Not_Key_Read_From_Code_Source()
	{
		// codeSource 只影响它声明了权限码的操作，其余操作一律走框架默认键。
		var provider = Build(s => s.AddPermission(new ConventionCodeSource(), FixturesAssembly));
		var registry = provider.GetRequiredService<ScopeModelRegistry>();
		var codeSource = provider.GetRequiredService<IPermissionCodeSource>();

		Assert.True(registry.TryGet(typeof(GuardedAsset), out var registration));
		Assert.Equal(
			ScopeKeys.Read,
			ScopeKeyResolver.Resolve(registration, typeof(GuardedAsset), BusinessOperation.Read, codeSource));
	}

	#endregion

	#region 装配边界

	[Fact]
	public void Security_Assembly_Should_Not_Reference_Osba()
	{
		Assert.DoesNotContain("Euonia.Osba", ReferencedBy(typeof(ScopeGuard).Assembly));
	}

	[Fact]
	public void This_Assembly_Should_Not_Reference_Osba()
	{
		// 本项目只引用 Euonia.Security；一旦有人顺手加上 Osba 引用，此处即失败。
		Assert.DoesNotContain("Euonia.Osba", ReferencedBy(typeof(AddPermissionTests).Assembly));
	}

	#endregion

	#region 只追加扫描范围

	[Fact]
	public void AddPermissionModels_Should_Extend_Scan_Scope()
	{
		// 模型分散在多个程序集、权限码来源只有一处：来源由 AddPermission 给出，
		// 其余程序集用本入口追加，不必重复传同一个来源实例。
		var services = new ServiceCollection();

		services.AddPermission(EmptyCodeSource.Instance);
		services.AddPermissionModels(TestAssembly);

		var provider = services.BuildServiceProvider();

		Assert.True(provider.GetRequiredService<ScopeModelRegistry>().IsDeclared(typeof(Asset)));
		Assert.Same(EmptyCodeSource.Instance, provider.GetRequiredService<IPermissionCodeSource>());
	}

	[Fact]
	public void AddPermissionModels_Should_Not_Be_A_Code_Source_Assertion()
	{
		// 本入口不提供权限码来源，因此不能用来绕过「显式给出来源」的要求（README §3.1）。
		// 只追加程序集时来源缺席、回落到 EmptyCodeSource，按码声明的策略无人能解析到——
		// 死策略校验必须在这里拦住，锁定该边界以免日后被当成缺陷「放宽」。
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ServiceCollection().AddPermissionModels(FixturesAssembly));

		Assert.Equal(nameof(GuardedAssetModel), Assert.Single(exception.Diagnostics).ModelName);
	}

	[Fact]
	public void Registering_Twice_Should_Keep_One_Descriptor_Per_Service()
	{
		// 注册是「替换」而非「追加」：多次注册后每种类型只留一条描述符，
		// 容器里不留失效的中间注册表。
		var services = new ServiceCollection();

		services.AddPermission(EmptyCodeSource.Instance, TestAssembly);
		services.AddPermission(new ConventionCodeSource(), FixturesAssembly);

		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ScopeModelRegistry)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(IPermissionCodeSource)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(PermissionSetup)));
	}

	#endregion

	#region 辅助

	private static string[] ReferencedBy(Assembly assembly)
	{
		return assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
	}

	private static string ResolveExecuteKey(IServiceProvider provider, Type resourceType)
	{
		var registry = provider.GetRequiredService<ScopeModelRegistry>();
		var codeSource = provider.GetRequiredService<IPermissionCodeSource>();

		Assert.True(registry.TryGet(resourceType, out var registration));

		return ScopeKeyResolver.Resolve(registration, resourceType, BusinessOperation.Execute, codeSource);
	}

	private static ServiceProvider Build(params Action<IServiceCollection>[] configure)
	{
		var services = new ServiceCollection();
		services.AddSingleton(User());

		foreach (var action in configure)
		{
			action(services);
		}

		return services.BuildServiceProvider();
	}

	private static UserPrincipal User(string userId = "dev")
	{
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, userId)],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}
