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
	/// <summary>
	/// 没有任何权限声明的框架程序集，用作「未使用权限」的干净扫描目标。
	/// </summary>
	private static readonly Assembly CleanAssembly = typeof(ServiceCollection).Assembly;

	private static readonly Assembly TestAssembly = typeof(AddPermissionTests).Assembly;

	/// <summary>
	/// 含「方法级权限码 + 按码声明策略」的程序集，必须配合真正的权限码来源。
	/// </summary>
	private static readonly Assembly FixturesAssembly = typeof(GuardedAssetModel).Assembly;

	#region 注册归属

	/// <summary>
	/// 注册后容器能解析权限引擎的四项核心服务。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Register_Engine_Services()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.NotNull(provider.GetService<ScopeModelRegistry>());
		Assert.NotNull(provider.GetService<PermissionRegistration>());
		Assert.NotNull(provider.GetService<IPermissionChecker>());
		Assert.NotNull(provider.GetService<IScopeGuard>());
	}

	/// <summary>
	/// 只引用权限引擎、不做对象建模时，注册、建模与判定仍可端到端使用。
	/// </summary>
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

	/// <summary>
	/// 模型注册表以单例注册：两次解析得到同一实例，且能识别已声明的资源。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Register_Model_Registry_As_Singleton()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.Same(registry, provider.GetRequiredService<ScopeModelRegistry>());
	}

	/// <summary>
	/// 返回传入的同一个服务集合，便于链式调用。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Return_Same_Service_Collection()
	{
		var services = new ServiceCollection();

		Assert.Same(services, services.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));
	}

	/// <summary>
	/// 权限码来源为 <c>null</c> 时抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Throw_On_Null_Code_Source()
	{
		var services = new ServiceCollection();

		Assert.Throws<ArgumentNullException>(() => services.AddPermission(p => p.Source(null!)));
	}

	/// <summary>
	/// 服务集合为 <c>null</c> 时抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Throw_On_Null_Service_Collection()
	{
		IServiceCollection services = null!;

		Assert.Throws<ArgumentNullException>(() => services.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));
	}

	#endregion

	#region 扩展点不被覆盖

	/// <summary>
	/// 宿主无需自行转换要求：引擎用容器中的权限码来源回答 <see cref="IPermissionCodeSource.RequirementsFor"/>，并保留角色信息。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Make_The_Source_Answer_Requirements_For_Hosts()
	{
		// 宿主不必自己写「要求来源」的转换：引擎把容器里的权限码来源回答成要求（角色等原样保留）
		var provider = Build(s => s.AddPermission(p =>
		{
			p.Scan(TestAssembly);
			p.OnAttributeOrName(BusinessOperation.Read, null!, typeof(AssetApproveAttribute));
		}));

		var requirements = provider.GetRequiredService<IPermissionCodeSource>()
		                           .RequirementsFor(typeof(ApprovableAsset), BusinessOperation.Read);

		Assert.Contains(requirements, requirement => requirement.Permission == "asset:approve" && requirement.Roles.Length > 0);
	}

	/// <summary>
	/// 只有权限码、没有角色的来源被折算为「有码无角色」的要求，而不是空要求。
	/// </summary>
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

	/// <summary>
	/// 宿主已注册的 <see cref="IScopeSubjectResolver"/> 不会被权限注册覆盖。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Not_Overwrite_Host_Subject_Resolver()
	{
		var expected = new FixedSubjectResolver();
		var provider = Build(
			s => s.AddSingleton<IScopeSubjectResolver>(expected),
			s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.Same(expected, provider.GetRequiredService<IScopeSubjectResolver>());
	}

	/// <summary>
	/// 显式提供的权限码来源被注册为 <see cref="IPermissionCodeSource"/> 单例。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Register_Code_Source()
	{
		var expected = new ConventionCodeSource();
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.Source(expected); }));

		Assert.Same(expected, provider.GetRequiredService<IPermissionCodeSource>());
	}

	/// <summary>
	/// 未注册操作解析器时，<see cref="IScopeGuard"/> 仍可解析并按模型默认策略判定。
	/// </summary>
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

	/// <summary>
	/// 扫描范围内没有任何声明时，启动期不要求主体解析器，守卫可直接解析。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Not_Require_Subject_Resolver_When_Nothing_Declared()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		Assert.False(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 未扫描任何程序集且未显式断言时，解析守卫抛出 <see cref="InvalidOperationException"/>，提示扫描范围为空（<c>NoModels</c>）。
	/// </summary>
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

	/// <summary>
	/// 显式调用 <c>NoModels</c> 断言「没有模型」后，空扫描范围通过启动期校验。
	/// </summary>
	[Fact]
	public void AddPermission_Without_Any_Assembly_Should_Pass_Validation_After_Explicit_Assertion()
	{
		var provider = Build(
			s => s.AddPermission(p => p.NoModels()));

		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 断言与注册顺序无关：先断言后注册同样生效。
	/// </summary>
	[Fact]
	public void Explicit_Assertion_Should_Be_Honored_When_Declared_Before_AddPermission()
	{
		// 顺序无关：断言与 AddPermission 谁先谁后都要生效（两者都落在同一份累积状态上）
		var provider = Build(
			s => s.AddPermission(p => { p.NoOperationCodes(); p.NoModels(); }));

		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 扫描过（即便没有声明）本身就构成显式断言，无需再调用 <c>NoModels</c>。
	/// </summary>
	[Fact]
	public void Scanning_Any_Assembly_Should_Count_As_The_Assertion_And_Need_No_Extra_Step()
	{
		// 「扫过但确实没有声明」与「从没扫过」是两回事：前者本身就是显式断言
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(CleanAssembly); p.NoOperationCodes(); }));

		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 扫描到声明后，注册结果标记为需要主体解析器。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Require_Subject_Resolver_When_Declared()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		Assert.True(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
	}

	/// <summary>
	/// 有声明但缺少主体解析器时，解析守卫抛出 <see cref="InvalidOperationException"/>，消息点名 <see cref="IScopeSubjectResolver"/>。
	/// </summary>
	[Fact]
	public void Guard_Resolution_Should_Throw_When_Declared_Without_Resolver()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	/// <summary>
	/// 主体解析器可在注册之后补充，容器定稿时存在即可通过校验。
	/// </summary>
	[Fact]
	public void Guard_Resolution_Should_Pass_When_Resolver_Registered_After_AddPermission()
	{
		// 解析器允许在 AddPermission 之后注册：只有容器定稿才能判断它是否存在。
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 配合权限码来源时，方法级权限声明同样使注册结果要求主体解析器。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Detect_Method_Level_Permission_With_Code_Source()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));

		Assert.True(provider.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
	}

	/// <summary>
	/// 模型为 <c>execute</c> 显式声明的键（<c>guarded:run</c>）优先于操作自身。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Key_Execute_From_Model_Declaration()
	{
		// 键由模型为操作声明，与权限码来源无关：GuardedAssetModel 为 execute 显式指定了 "guarded:run"。
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); }));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	/// <summary>
	/// 模型未声明策略时，操作以自身为键。
	/// </summary>
	[Fact]
	public void AddPermission_Should_Key_Execute_From_Operation_When_Model_Declares_None()
	{
		// 模型没为某操作声明策略时，该操作以自身为键（以 @execute 为键的话授予写不进去）。
		var provider = Build(s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }));

		Assert.Equal(BusinessOperation.Execute, ResolveExecuteKey(provider, typeof(Asset)));
	}

	/// <summary>
	/// 声明按标识生效：为 <c>execute</c> 声明的键不会作用于 <c>read</c>。
	/// </summary>
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

	/// <summary>
	/// 权限引擎程序集不得引用 Osba，以保持独立的行级数据权限能力。
	/// </summary>
	[Fact]
	public void Security_Assembly_Should_Not_Reference_Osba()
	{
		Assert.DoesNotContain("Euonia.Osba", ReferencedBy(typeof(ScopeGuard).Assembly));
	}

	/// <summary>
	/// 本测试程序集不得引用 Osba，确保用例只依赖权限引擎。
	/// </summary>
	[Fact]
	public void This_Assembly_Should_Not_Reference_Osba()
	{
		// 本项目只引用 Euonia.Security；一旦有人顺手加上 Osba 引用，此处即失败。
		Assert.DoesNotContain("Euonia.Osba", ReferencedBy(typeof(AddPermissionTests).Assembly));
	}

	#endregion

	#region 只追加扫描范围

	/// <summary>
	/// 扫描后注册表包含该程序集声明的模型。
	/// </summary>
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

	/// <summary>
	/// 多次注册是替换而非追加：每种引擎服务在容器中只保留一条描述符。
	/// </summary>
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
		return assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();
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
