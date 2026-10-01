using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.BrokenFixtures;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

public class GeneralityTests
{
	private static readonly Assembly TestAssembly = typeof(GeneralityTests).Assembly;

	private static readonly Assembly FixturesAssembly = typeof(Widget).Assembly;

	private static readonly Assembly BrokenFixturesAssembly = typeof(AlwaysDenyModel).Assembly;

	#region 操作集可扩展

	[Fact]
	public void Host_Defined_Operation_Should_Resolve_Its_Own_Key()
	{
		const string approve = "approve";

		var source = OperationCodeSource.Create()
			.OnAttribute(approve, typeof(AssetApproveAttribute))
			.Build();

		var registry = new ScopeModelRegistryBuilder()
			.Add<AssetModel>()
			.Build(source);

		Assert.True(registry.TryGet(typeof(Asset), out var registration));

		Assert.Equal("@approve", ScopeKeyResolver.Resolve(registration, typeof(Asset), approve, source));
	}

	[Fact]
	public void Custom_Operation_Key_Should_Not_Collide_With_Default_Operations()
	{
		Assert.NotEqual(ScopeKeys.For("approve"), ScopeKeys.For(BusinessOperation.Read));
		Assert.Equal("@approve", ScopeKeys.For("approve"));
	}

	[Fact]
	public void Operation_Name_With_Reserved_Prefix_Should_Be_Rejected()
	{
		var exception = Assert.Throws<InvalidOperationException>(() => ScopeKeys.For("@approve"));

		Assert.Contains("@", exception.Message);
	}

	[Fact]
	public void ScopePolicySet_Should_Separate_Operation_Key_From_Permission_Code()
	{
		var byOperation = new ScopePolicySet<Asset>();
		var byCode = new ScopePolicySet<Asset>();

		byOperation.ForOperation(BusinessOperation.Read, ScopePolicy<Asset>.Grant(ScopeDimensions.Owner));
		byCode.For(BusinessOperation.Read, ScopePolicy<Asset>.Grant(ScopeDimensions.Dept));

		Assert.NotNull(byOperation);
		Assert.NotNull(byCode);
	}

	#endregion

	#region 通用权限码来源

	[Fact]
	public void OperationCodeSource_Should_Match_By_Attribute()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.Build();

		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Match_By_Convention_Name()
	{
		var source = OperationCodeSource.Create()
			.OnMethodName(BusinessOperation.Execute, "Approve", "ApproveAsync")
			.Build();

		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Always_Include_Type_Level_Codes()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		Assert.Contains("asset:read", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
		Assert.DoesNotContain("asset:read", source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Distinguish_Operations()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.Build();

		Assert.DoesNotContain("asset:approve", source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Read));
	}

	[Fact]
	public void OperationCodeSource_Should_Deduplicate_Codes()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.OnAttribute(BusinessOperation.Execute, typeof(AssetSecondApproveAttribute))
			.Build();

		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Expose_Full_Requirements_For_Roles()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.Build();

		var requirements = source.RequirementsFor(typeof(ApprovableAsset), BusinessOperation.Execute);

		Assert.Contains(requirements, x => x.Roles is { Length: > 0 });
	}

	[Fact]
	public void OperationCodeSource_Should_Report_Only_Declared_Operations()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		Assert.Equal([BusinessOperation.Read], source.AllOperations);
	}

	[Fact]
	public void OperationCodeSource_Should_Reject_Empty_Rule_Set()
	{
		Assert.Throws<InvalidOperationException>(() => OperationCodeSource.Create().Build());
	}

	[Fact]
	public void OperationConventions_Should_Derive_Names_With_And_Without_Prefix()
	{
		var withPrefix = OperationConventions.Names(typeof(AssetApproveAttribute), "Asset");
		var withoutPrefix = OperationConventions.Names(typeof(AssetApproveAttribute));

		Assert.Equal(["Approve", "ApproveAsync", "AssetApprove", "AssetApproveAsync"], withPrefix);
		Assert.Equal(["AssetApprove", "AssetApproveAsync"], withoutPrefix);
	}

	#endregion

	#region 程序化注册模型

	[Fact]
	public void Builder_Should_Register_Model_Instance()
	{
		var registry = new ScopeModelRegistryBuilder()
			.Add(new AssetModel())
			.Build(EmptyCodeSource.Instance);

		Assert.True(registry.IsDeclared(typeof(Asset)));
	}

	[Fact]
	public void Builder_Should_Register_Model_Type()
	{
		var registry = new ScopeModelRegistryBuilder()
			.Add<AssetModel>()
			.Build(EmptyCodeSource.Instance);

		Assert.True(registry.IsDeclared(typeof(Asset)));
	}

	[Fact]
	public void Builder_Should_Combine_Instance_And_Assembly()
	{
		var registry = new ScopeModelRegistryBuilder()
			.AddFrom(TestAssembly)
			.Add(new WidgetModel())
			.Build(EmptyCodeSource.Instance);

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.True(registry.IsDeclared(typeof(Widget)));
	}

	[Fact]
	public void Builder_Should_Return_Empty_When_Nothing_Registered()
	{
		var registry = new ScopeModelRegistryBuilder().Build(EmptyCodeSource.Instance);

		Assert.Same(ScopeModelRegistry.Empty, registry);
		Assert.False(registry.HasDeclarations);
	}

	[Fact]
	public void Builder_Should_Reject_Duplicate_Model_For_One_Resource()
	{
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.AddFrom(TestAssembly)
				.Add(new AssetModel())
				.Build(EmptyCodeSource.Instance));

		Assert.Contains(exception.Diagnostics, x => x.Message.Contains("more than one permission model"));
	}

	[Fact]
	public void Scanned_And_Manual_Models_Should_Be_Validated_Identically()
	{
		var scanned = Assert.Throws<ScopeModelValidationException>(
			() => ScopeModelRegistry.Create(EmptyCodeSource.Instance, BrokenFixturesAssembly));

		var manual = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<AlwaysDenyModel>()
				.Add<UnmappedDimensionModel>()
				.Add<UnsupportedCollectionModel>()
				.Build(EmptyCodeSource.Instance));

		Assert.Equal(
			scanned.Diagnostics.Select(x => x.Message).OrderBy(x => x),
			manual.Diagnostics.Select(x => x.Message).OrderBy(x => x));
	}

	#endregion

	#region 诊断聚合

	[Fact]
	public void Validation_Should_Report_All_Problems_At_Once()
	{
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<UnmappedDimensionModel>()
				.Add<AlwaysDenyModel>()
				.Build(EmptyCodeSource.Instance));

		Assert.Equal(2, exception.Diagnostics.Count);
		Assert.Contains("unmapped dimension", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("can never allow structurally", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidationException_Should_Carry_Each_Diagnostic()
	{
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<AlwaysDenyModel>()
				.Build(EmptyCodeSource.Instance));

		var diagnostic = Assert.Single(exception.Diagnostics);

		Assert.Equal(nameof(AlwaysDenyModel), diagnostic.ModelName);
		Assert.Contains("can never allow structurally", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
	}

	#endregion

	#region 用户主体

	[Fact]
	public void Authenticated_User_Should_Pass_Grant()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				codes: ["asset:approve"],
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.Contains("asset:approve", guard.Permissions);
		Assert.True(guard.Allows(new Asset { DeptId = "team-a", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public async Task GetSubjectsAsync_Should_Return_The_Cached_Snapshot_Without_Blocking()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				codes: ["asset:approve"],
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		// 冷缓存先走异步入口：它 await 解析完成后再返回快照，
		// 让异步调用链不必像 GetSubjects 那样在冷路径上被 AsyncContext.Run 阻塞
		var snapshot = await guard.GetSubjectsAsync(TestContext.Current.CancellationToken);

		Assert.True(snapshot.HoldsPermission("asset:approve"));

		// 随后的同步入口命中同一条暖路径，必须返回同一份快照（判定口径不因入口而异）
		Assert.Same(snapshot, guard.GetSubjects());
	}

	[Fact]
	public void Unauthenticated_User_Should_Deny_Even_When_Resolver_Returns_Grants()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: false)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.Empty(guard.Permissions);
		Assert.False(guard.Allows(new Asset { DeptId = "team-a", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public void Missing_UserPrincipal_Should_Be_Reported_When_Guard_Resolved()
	{
		var provider = Build(
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains("UserPrincipal", exception.Message);
	}

	[Fact]
	public void Setup_Validation_Should_Pass_When_User_Registered()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		provider.GetRequiredService<IScopeGuard>();
	}

	#endregion

	#region 派生类型（代理）

	/// <summary>
	/// 实体框架代理的替身：注册的是声明类型 <see cref="ProxyableAsset"/>，实例却是派生类型。
	/// </summary>
	private sealed class ProxyableAssetProxy : ProxyableAsset
	{
	}

	[Fact]
	public void Allows_And_AllowsObject_Should_Agree_For_Proxy_Instance()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		var visible = new ProxyableAssetProxy { DeptId = "team-a" };
		var invisible = new ProxyableAssetProxy { DeptId = "team-b" };

		Assert.True(guard.Allows(visible));
		Assert.True(guard.AllowsObject(visible));

		// 代理类型本身未注册，但基类注册了：必须按声明类型判定，不得静默放行。
		Assert.False(guard.Allows(invisible));
		Assert.False(guard.AllowsObject(invisible));
	}

	#endregion

	#region 授权数据校验

	[Fact]
	public void AddCode_Should_Reject_Reserved_Namespace()
	{
		var builder = ScopeSubjectSet.CreateBuilder();

		Assert.Throws<InvalidOperationException>(() => builder.AddCode("@read"));
		Assert.Throws<InvalidOperationException>(() => builder.AddCode(ScopeKeys.Default));
	}

	#endregion

	#region 多模块合并

	[Fact]
	public void Second_Module_Should_Not_Be_Silently_Dropped()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(typeof(Asset).Assembly); p.Source(RunCodeSource()); });
		services.AddPermission(p => { p.Scan(FixturesAssembly); p.NoOperationCodes(); });

		var registry = services.BuildServiceProvider().GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.True(registry.IsDeclared(typeof(Widget)));
	}

	[Fact]
	public void Guard_Should_See_Late_Registered_Models()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(typeof(Asset).Assembly); p.Source(RunCodeSource()); });
		services.AddPermission(p => { p.Scan(FixturesAssembly); p.NoOperationCodes(); });

		services.AddSingleton(User(authenticated: true));
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
			grants: [(ScopeDimensions.Dept, "team-a")]));

		var guard = services.BuildServiceProvider().GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new Widget { DeptId = "team-a", OwnerId = "other" }));
		Assert.False(guard.Allows(new Widget { DeptId = "team-b", OwnerId = "other" }));
	}

	[Fact]
	public void Code_Sources_From_Different_Modules_Should_Merge()
	{
		var first = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		var second = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetSecondApproveAttribute))
			.Build();

		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.Source(first); });
		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.Source(second); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(["asset:read", "asset:approve"], source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	[Fact]
	public void Merged_Operations_Should_Be_Union_In_Declaration_Order()
	{
		var first = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		var second = OperationCodeSource.Create()
			.OnAttribute("approve", typeof(AssetApproveAttribute))
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		var services = new ServiceCollection();

		services.AddPermission(p => p.Source(first));
		services.AddPermission(p => p.Source(second));

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal([BusinessOperation.Read, "approve"], source.AllOperations);
	}

	[Fact]
	public void Empty_Source_Should_Not_Suppress_Other_Modules()
	{
		var other = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		var services = new ServiceCollection();

		services.AddPermission(p => p.NoOperationCodes());
		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.Source(other); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(BusinessOperation.All, source.AllOperations);
		Assert.Equal(["asset:read", "asset:approve"], source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	[Fact]
	public void Registration_Should_Cover_All_Modules_Declarations()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => p.NoOperationCodes());
		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.NoOperationCodes(); });

		var registration = services.BuildServiceProvider().GetRequiredService<PermissionRegistration>();

		Assert.True(registration.RequiresSubjectResolver);
	}

	[Fact]
	public void Registering_Same_Source_Instance_Twice_Should_Be_Idempotent()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute))
			.Build();

		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.Source(source); });
		services.AddPermission(p => { p.Scan(typeof(ApprovableAsset).Assembly); p.Source(source); });

		var merged = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal([BusinessOperation.Read], merged.AllOperations);
		Assert.Equal(["asset:read", "asset:approve"], merged.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	[Fact]
	public void Assemblies_Should_Be_Scanned_Once_Even_If_Shared()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(typeof(Asset).Assembly); p.NoOperationCodes(); });
		services.AddPermission(p => { p.Scan(typeof(Asset).Assembly); p.NoOperationCodes(); });

		var registry = services.BuildServiceProvider().GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
	}

	#endregion

	#region 辅助

	private static UserPrincipal User(bool authenticated, params string[] roles)
	{
		var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		var identity = new ClaimsIdentity(
			claims,
			authenticated ? "Bearer" : null,
			ClaimTypes.Name,
			ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static IPermissionCodeSource RunCodeSource() =>
		OperationCodeSource.Create()
			.OnMethodName(BusinessOperation.Execute, "Run")
			.Build();

	private static ServiceProvider Build(params Action<IServiceCollection>[] configure)
	{
		var services = new ServiceCollection();

		foreach (var action in configure)
		{
			action(services);
		}

		return services.BuildServiceProvider();
	}

	#endregion
}
