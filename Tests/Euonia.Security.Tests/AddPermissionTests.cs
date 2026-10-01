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
		var provider = Build(s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.NotNull(provider.GetService<ScopeModelRegistry>());
		Assert.NotNull(provider.GetService<PermissionRegistration>());
		Assert.NotNull(provider.GetService<IPermissionChecker>());
		Assert.NotNull(provider.GetService<IScopeGuard>());
	}

	[Fact]
	public void AddPermission_Should_Not_Require_Object_Model()
	{
		// 只引用 Euonia.Security 的独立使用：注册、建模、判定全链路可用。
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new Asset { Id = "1", DeptId = "team-a", OwnerId = "other", Level = "normal" }));
		Assert.False(guard.Allows(new Asset { Id = "2", DeptId = "team-x", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public void AddPermission_Should_Register_Model_Registry_As_Singleton()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.Same(registry, provider.GetRequiredService<ScopeModelRegistry>());
	}

	[Fact]
	public void AddPermission_Should_Return_Same_Service_Collection()
	{
		var services = new ServiceCollection();

		Assert.Same(services, services.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));
	}

	[Fact]
	public void AddPermission_Should_Throw_On_Null_Code_Source()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentNullException>(() => services.AddPermission(p => p.Source(null)));
	}

	[Fact]
	public void AddPermission_Should_Throw_On_Null_Service_Collection()
	{
		IServiceCollection services = null;

		Assert.Throws<ArgumentNullException>(() => services.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));
	}

	#endregion

	#region 扩展点不被覆盖

	[Fact]
	public void AddPermission_Should_Make_The_Source_Answer_Requirements_For_Hosts()
	{
		// 宿主不必自己写「要求来源」的转换：引擎把容器里的权限码来源回答成要求（角色等原样保留）
		var provider = Build(s => s.AddPermission(p =>
		{
			p.Scan(TestAssembly);
			p.OnAttributeOrName(BusinessOperation.Read, null, typeof(AssetApproveAttribute));
		}));

		var requirements = provider.GetRequiredService<IPermissionCodeSource>()
		                           .RequirementsFor(typeof(ApprovableAsset), BusinessOperation.Read);

		Assert.Contains(requirements, requirement => requirement.Permission == "asset:approve" && requirement.Roles.Length > 0);
	}

	[Fact]
	public void Code_Source_Should_Synthesize_CodeOnly_Requirements()
	{
		// 只给权限码的来源折算成「有码、无角色」——回答不了角色不等于没有要求，
		// 把它当成空要求会让闸门比来源本身更宽松
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));

		var requirements = provider.GetRequiredService<IPermissionCodeSource>()
		                           .RequirementsFor(typeof(GuardedAsset), BusinessOperation.Execute);

		Assert.Contains(requirements, requirement => requirement.Permission == "guarded:run" && requirement.Roles.Length == 0);
	}

	[Fact]
	public void AddPermission_Should_Not_Overwrite_Host_Subject_Resolver()
	{
		var expected = new FixedSubjectResolver();
		var provider = Build(
			s => s.AddSingleton<IScopeSubjectResolver>(expected),
			s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.Same(expected, provider.GetRequiredService<IScopeSubjectResolver>());
	}

	[Fact]
	public void AddPermission_Should_Register_Code_Source()
	{
		var expected = new ConventionCodeSource();
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.Source(expected); }));

		Assert.Same(expected, provider.GetRequiredService<IPermissionCodeSource>());
	}

	[Fact]
	public void ScopeGuard_Should_Resolve_Without_Operation_Resolver()
	{
		// 未注册 IObjectOperationResolver 时必须按模型的默认策略判定（AssetModel 只声明了默认策略），
		// 而不是因缺少依赖而解析失败。
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
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
		var provider = Build(s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.False(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
		provider.GetRequiredService<IScopeGuard>();
	}

	[Fact]
	public void AddPermission_Without_Any_Assembly_Should_Fail_Validation_Without_Explicit_Assertion()
	{
		// 零程序集 ⇒ 扫描范围为空 ⇒ 行级数据权限静默失效，而 RequiresSubjectResolver 恒为 false
		// 会让下面的校验一并短路。与 EmptyCodeSource 同一条规则：空输入必须显式做出选择。
		var provider = Build(s => s.AddPermission(p => p.NoOperationCodes()));

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains("without any assembly", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("NoModels", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void AddPermission_Without_Any_Assembly_Should_Pass_Validation_After_Explicit_Assertion()
	{
		var provider = Build(
			s => s.AddPermission(p => p.NoModels()));

		provider.GetRequiredService<IScopeGuard>();
	}

	[Fact]
	public void Explicit_Assertion_Should_Be_Honored_When_Declared_Before_AddPermission()
	{
		// 顺序无关：断言与 AddPermission 谁先谁后都要生效（两者都落在同一份累积状态上）
		var provider = Build(
			s => s.AddPermission(p => { p.NoOperationCodes(); p.NoModels(); }));

		provider.GetRequiredService<IScopeGuard>();
	}

	[Fact]
	public void Scanning_Any_Assembly_Should_Count_As_The_Assertion_And_Need_No_Extra_Step()
	{
		// 「扫过但确实没有声明」与「从没扫过」是两回事：前者本身就是显式断言
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		provider.GetRequiredService<IScopeGuard>();
	}

	[Fact]
	public void AddPermission_Should_Require_Subject_Resolver_When_Declared()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		Assert.True(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
	}

	[Fact]
	public void Guard_Resolution_Should_Throw_When_Declared_Without_Resolver()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	[Fact]
	public void Guard_Resolution_Should_Pass_When_Resolver_Registered_After_AddPermission()
	{
		// 解析器允许在 AddPermission 之后注册：只有容器定稿才能判断它是否存在。
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		provider.GetRequiredService<IScopeGuard>();
	}

	[Fact]
	public void AddPermission_Should_Detect_Method_Level_Permission_With_Code_Source()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));

		Assert.True(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
	}

	[Fact]
	public void AddPermission_Should_Key_Execute_From_Model_Declaration()
	{
		// 键由模型为操作声明，与权限码来源无关：GuardedAssetModel 为 execute 显式指定了 "guarded:run"。
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	[Fact]
	public void AddPermission_Should_Key_Execute_From_Operation_When_Model_Declares_None()
	{
		// 模型没为某操作声明策略时，该操作以自身为键（以 @execute 为键的话授予写不进去）。
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		Assert.Equal(BusinessOperation.Execute, ResolveExecuteKey(provider, typeof(Asset)));
	}

	[Fact]
	public void AddPermission_Should_Not_Key_Declared_Operation_From_Other_Declarations()
	{
		// 声明是按标识生效的：模型只为 execute 声明了键，read 一律以自身为键。
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));
		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.TryGet(typeof(GuardedAsset), out var registration));

		registration.TryResolve(BusinessOperation.Read, out _, out var key);

		Assert.Equal(BusinessOperation.Read, key);
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
	public void Scan_Should_Extend_Scan_Scope()
	{
		// 模型分散在多个程序集：Scan 可多次调用，程序集按并集累积。
		var services = new ServiceCollection();

		services.AddPermission(p =>
		{
			p.Scan(TestAssembly);
			p.NoOperationCodes();
		});

		var provider = services.BuildServiceProvider();

		Assert.True(provider.GetRequiredService<ScopeModelRegistry>().IsDeclared(typeof(Asset)));
	}

	[Fact]
	public void Registering_Twice_Should_Keep_One_Descriptor_Per_Service()
	{
		// 注册是「替换」而非「追加」：多次注册后每种类型只留一条描述符，
		// 容器里不留失效的中间注册表。
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); });
		services.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); });

		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ScopeModelRegistry)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(IPermissionCodeSource)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(PermissionRegistration)));
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

		Assert.True(registry.TryGet(resourceType, out var registration));

		registration.TryResolve(BusinessOperation.Execute, out _, out var key);

		return key;
	}

	private static ServiceProvider Build(params Action<IServiceCollection>[] configure)
	{
		var services = new ServiceCollection();
		services.AddSingleton(User());

		foreach (var action in configure)
		{
			action(services);
		}

		return services.BuildServiceProvider().Warm();
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
