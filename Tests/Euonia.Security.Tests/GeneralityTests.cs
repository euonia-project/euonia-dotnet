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
			.Build();

		Assert.True(registry.TryGet(typeof(Asset), out var registration));

		// 未声明的操作以自身为键：以 @approve 为键的话，AddGrant 会拒绝这个保留码，
		// 于是「approve 这个操作的行级授予」永远写不进去。
		Assert.False(registration.TryResolve(approve, out var policy, out var key));
		Assert.Null(policy);
		Assert.Equal(approve, key);
	}

	[Fact]
	public void Custom_Operation_Key_Should_Not_Collide_With_Default_Operations()
	{
		Assert.NotEqual(ScopeKeys.KeyFor("approve"), ScopeKeys.KeyFor(BusinessOperation.Read));
		Assert.Equal("approve", ScopeKeys.KeyFor("approve"));
	}

	[Fact]
	public void Operation_Name_With_Reserved_Prefix_Should_Be_Rejected()
	{
		var exception = Assert.Throws<InvalidOperationException>(() =>
			new ScopePolicySet<Asset>().ForOperation("@approve", ScopePolicy<Asset>.Grant(ScopeDimensions.Owner)));

		Assert.Contains("@", exception.Message);
	}

	[Fact]
	public void ScopePolicySet_Should_Accept_An_Explicit_Grant_Key()
	{
		// 策略挂在操作上，键只决定授予从哪来：显式给出键即可让授予仍写在权限码下。
		var policies = new ScopePolicySet<Asset>();

		policies.ForOperation(BusinessOperation.Read, ScopePolicy<Asset>.Grant(ScopeDimensions.Owner));
		policies.ForOperation(BusinessOperation.Update, ScopePolicy<Asset>.Grant(ScopeDimensions.Dept), "asset:edit");

		Assert.Contains(BusinessOperation.Read, policies.Identifiers);
		Assert.Contains(BusinessOperation.Update, policies.Identifiers);
	}

	[Fact]
	public void Declared_Grant_Key_Should_Be_Addressable_As_An_Identifier()
	{
		// 键与标识共用一个命名空间：为某个操作声明策略、键写权限码之后，用那个码寻址必须命中同一对（策略，键）。
		// 少了这一步，「按权限码调用一个按操作声明的策略」会静默回落到默认策略——
		// 调用方以为拿到了窄化后的行范围，实际拿到的是最宽的那条。
		var registry = new ScopeModelRegistryBuilder()
			.Add<GuardedAssetModel>()
			.Build();

		Assert.True(registry.TryGet(typeof(GuardedAsset), out var registration));

		Assert.True(registration.TryResolve(BusinessOperation.Execute, out var byOperation, out var operationKey));
		Assert.True(registration.TryResolve("guarded:run", out var byCode, out var codeKey));

		Assert.Same(byOperation, byCode);
		Assert.Equal("guarded:run", operationKey);
		Assert.Equal(operationKey, codeKey);
	}

	[Fact]
	public void Addressed_By_Grant_Key_Should_Use_The_Declared_Policy_Not_The_Default()
	{
		// 示例工程的写法：策略按操作声明、授予键写权限码，调用点传的是权限码。
		// 传码必须命中那条声明；命中失败会静默回落到默认策略——调用方以为拿到了窄化后的行范围，
		// 实际拿到的是最宽的那条。GuardedAssetModel 的默认策略是 Grant(Owner)，
		// 为 execute 声明的却是 Grant(Dept)，因此两者在行为上可区分。
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Owner, "dev")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		var byOperation = guard.GetPolicy<GuardedAsset>(BusinessOperation.Execute);
		var byCode = guard.GetPolicy<GuardedAsset>("guarded:run");

		Assert.NotNull(byOperation);
		Assert.Same(byOperation, byCode);
		Assert.Equal("guarded:run", byCode.ScopeKey);

		var asset = new GuardedAsset { OwnerId = "dev", DeptId = "team-a" };

		Assert.False(guard.Allows(asset, "guarded:run"));

		// 没声明过的标识才回落到默认策略（Grant(Owner)，此时 owner 恰好是 dev）——
		// 上一条断言之所以为 false，正是因为传码命中了声明，而不是命中默认策略。
		Assert.True(guard.Allows(asset, "not-declared"));
	}

	[Fact]
	public void For_Should_Declare_A_Code_As_Both_Identifier_And_Grant_Key()
	{
		// 只用 For 声明的模型：标识与授予键都是权限码，不需要经由「操作」这一层。
		var registry = new ScopeModelRegistryBuilder()
			.Add<KeyedAssetModel>()
			.Build();

		Assert.True(registry.TryGet(typeof(KeyedAsset), out var registration));

		Assert.True(registration.TryResolve(KeyedAssetModel.View, out var declared, out var key));

		Assert.NotNull(declared);
		Assert.Equal(KeyedAssetModel.View, key);

		// 没声明过的码回落默认策略，但键仍是它自己——在这个码下写的行级授予照样生效。
		Assert.False(registration.TryResolve("keyed:unlisted", out var fallback, out var fallbackKey));

		Assert.Null(fallback);
		Assert.Equal("keyed:unlisted", fallbackKey);
	}

	[Fact]
	public void For_Declared_Policy_Should_Drive_Judgment_End_To_End()
	{
		// 按码声明的策略必须真的参与判定，而不只是进了注册表：
		// KeyedAssetModel 的默认策略是 Self()，为 keyed:view 声明的却是 Any(Self(), Grant(Dept))，
		// 两者对「别人建的、但属于我所在部门」的行给出相反结论。
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); }),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Owner, "dev"), (ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();
		var asset = new KeyedAsset { OwnerId = "other", DeptId = "team-a" };

		Assert.False(guard.Allows(asset));
		Assert.True(guard.Allows(asset, KeyedAssetModel.View));

		// keyed:delete 只按负责人：行的负责人不是当前用户，即便同部门也拒。
		Assert.False(guard.Allows(asset, KeyedAssetModel.Delete));
	}

	[Fact]
	public void Identifier_And_Grant_Key_Should_Share_One_Namespace()
	{
		// 三个方向都要挡，否则被撞的那条声明的行权限会被另一条悄悄接管（拿它的键去取别人的授予）。
		var identifierTaken = new ScopePolicySet<Asset>();
		identifierTaken.For("asset:view", ScopePolicy<Asset>.Grant(ScopeDimensions.Owner));

		Assert.Throws<InvalidOperationException>(() =>
			identifierTaken.ForOperation("asset:view", ScopePolicy<Asset>.Grant(ScopeDimensions.Dept)));

		var keyTaken = new ScopePolicySet<Asset>();
		keyTaken.ForOperation(BusinessOperation.Read, ScopePolicy<Asset>.Grant(ScopeDimensions.Owner), "asset:view");

		Assert.Throws<InvalidOperationException>(() =>
			keyTaken.For("asset:view", ScopePolicy<Asset>.Grant(ScopeDimensions.Dept)));

		Assert.Throws<InvalidOperationException>(() =>
			keyTaken.ForOperation(BusinessOperation.Update, ScopePolicy<Asset>.Grant(ScopeDimensions.Dept), "asset:view"));
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
			.Build();

		Assert.True(registry.IsDeclared(typeof(Asset)));
	}

	[Fact]
	public void Builder_Should_Register_Model_Type()
	{
		var registry = new ScopeModelRegistryBuilder()
			.Add<AssetModel>()
			.Build();

		Assert.True(registry.IsDeclared(typeof(Asset)));
	}

	[Fact]
	public void Builder_Should_Combine_Instance_And_Assembly()
	{
		var registry = new ScopeModelRegistryBuilder()
			.AddFrom(TestAssembly)
			.Add(new WidgetModel())
			.Build();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.True(registry.IsDeclared(typeof(Widget)));
	}

	[Fact]
	public void Builder_Should_Return_Empty_When_Nothing_Registered()
	{
		var registry = new ScopeModelRegistryBuilder().Build();

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
				.Build());

		Assert.Contains(exception.Diagnostics, x => x.Message.Contains("more than one permission model"));
	}

	[Fact]
	public void Scanned_And_Manual_Models_Should_Be_Validated_Identically()
	{
		var scanned = Assert.Throws<ScopeModelValidationException>(
			() => ScopeModelRegistry.Create(BrokenFixturesAssembly));

		var manual = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<AlwaysDenyModel>()
				.Add<UnmappedDimensionModel>()
				.Add<UnsupportedCollectionModel>()
				.Build());

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
				.Build());

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
				.Build());

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

		var guard = services.BuildServiceProvider().Warm().GetRequiredService<IScopeGuard>();

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

		// 空来源不再冒充一份默认词表：它的操作全集是空的，合并后只剩另一个模块声明的那些。
		Assert.Equal([BusinessOperation.Read], source.AllOperations);
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

		return services.BuildServiceProvider().Warm();
	}

	#endregion
}
