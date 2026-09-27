using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.BrokenFixtures;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 通用性回归：这些用例断言的是「换一个宿主框架也能用」这件事本身。
/// </summary>
public class GeneralityTests
{
	private static readonly Assembly TestAssembly = typeof(GeneralityTests).Assembly;

	private static readonly Assembly BrokenFixturesAssembly = typeof(AlwaysDenyModel).Assembly;

	#region 操作集可扩展

	[Fact]
	public void Host_Defined_Operation_Should_Resolve_Its_Own_Key()
	{
		// 宿主自定义操作（此处模拟审批流），框架不要求它出现在 BusinessOperation 里。
		const string approve = "approve";

		var source = OperationCodeSource.Create()
			.OnAttribute(approve, typeof(AssetApproveAttribute))
			.Build();

		var registry = new ScopeModelRegistryBuilder()
			.Add<AssetModel>()
			.Build(source);

		Assert.True(registry.TryGet(typeof(Asset), out var registration));

		// Asset 上没有声明过权限码 ⇒ 落到该操作自己的默认键。
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
		// 操作名的默认键由 '@' 派生，操作名本身再带前缀就会与保留命名空间打架。
		var exception = Assert.Throws<InvalidOperationException>(() => ScopeKeys.For("@approve"));

		Assert.Contains("@", exception.Message);
	}

	[Fact]
	public void ScopePolicySet_Should_Separate_Operation_Key_From_Permission_Code()
	{
		// 「read 是操作名」与「read 是权限码」在字符串层面无法区分，
		// 因此必须是两个方法——否则调用方只能靠猜。
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
		// 只有命名约定、没有特性：只按特性收集会让这半边约定形同虚设。
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

		// 类型级声明对本来源已声明的每个操作都生效，与该操作有没有方法级声明无关
		Assert.Contains("asset:read", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
		Assert.DoesNotContain("asset:read", source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Distinguish_Operations()
	{
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.Build();

		Assert.Empty(source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Read).Where(c => c == "asset:approve"));
	}

	[Fact]
	public void OperationCodeSource_Should_Deduplicate_Codes()
	{
		// 两个入口方法都带同一个 [Permission]：重复会让「同一操作解析出多个码」的歧义检查误报。
		var source = OperationCodeSource.Create()
			.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute))
			.OnAttribute(BusinessOperation.Execute, typeof(AssetSecondApproveAttribute))
			.Build();

		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void OperationCodeSource_Should_Expose_Full_Requirements_For_Roles()
	{
		// 只给权限码的来源无法支撑运行期判定：角色等信息在特性上。
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
		// 「没有规则」会让所有方法级声明静默失效，与 EmptyCodeSource 是两件事。
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
		// 手动注册实例：模型需要构造参数或按配置生成时唯一的路径。
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
			.Add(new ReportAssetModel())
			.Build(EmptyCodeSource.Instance);

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.True(registry.IsDeclared(typeof(ReportAsset)));
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
		// 重复不是「后者胜出」——那会让作者以为先注册的那个生效了。
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.AddFrom(TestAssembly)
				.Add(new AssetModel())
				.Build(EmptyCodeSource.Instance));

		Assert.Contains(exception.Diagnostics, x => x.Message.Contains("多个权限模型"));
	}

	[Fact]
	public void Scanned_And_Manual_Models_Should_Be_Validated_Identically()
	{
		// 两条路径共用 Build()，因此不存在「扫描进来的查得严、手动注册的查得松」。
		var scanned = Assert.Throws<ScopeModelValidationException>(
			() => ScopeModelRegistry.Create(EmptyCodeSource.Instance, BrokenFixturesAssembly));

		var manual = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<AlwaysDenyModel>()
				.Add<UnmappedDimensionModel>()
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
		// 一次报全部：逐个报出等于让人反复重启。
		var exception = Assert.Throws<ScopeModelValidationException>(
			() => new ScopeModelRegistryBuilder()
				.Add<UnmappedDimensionModel>()
				.Add<AlwaysDenyModel>()
				.Build(EmptyCodeSource.Instance));

		Assert.Equal(2, exception.Diagnostics.Count);
		Assert.Contains("未映射的维度", exception.Message);
		Assert.Contains("恒不放行", exception.Message);
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
		Assert.Contains("恒不放行", diagnostic.Message);
	}

	#endregion


	#region 用户主体

	[Fact]
	public void Authenticated_User_Should_Pass_Grant()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				codes: ["asset:approve"],
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Permissions.Contains("asset:approve"));
		Assert.True(guard.Allows(new Asset { DeptId = "team-a", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public void Unauthenticated_User_Should_Deny_Even_When_Resolver_Returns_Grants()
	{
		// 这是曾经真实存在的缺口：解析器一旦返回了授予集合，而调用方其实并无身份，
		// 就会退化成「匿名即放行」。授权数据解析与身份判定必须都过。
		var provider = Build(
			s => s.AddSingleton(User(authenticated: false)),
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
				grants: [(ScopeDimensions.Dept, "team-a")])));

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.Empty(guard.Permissions);
		Assert.False(guard.Allows(new Asset { DeptId = "team-a", OwnerId = "other", Level = "normal" }));
	}

	[Fact]
	public void Missing_UserPrincipal_Should_Be_Reported_By_ValidatePermissionSetup()
	{
		// 「接了数据权限却所有人都被拒」不能没有任何提示，否则会被误判成策略写错。
		var provider = Build(
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		var exception = Assert.Throws<InvalidOperationException>(() => provider.ValidatePermissionSetup());

		Assert.Contains("UserPrincipal", exception.Message);
	}

	[Fact]
	public void ValidatePermissionSetup_Should_Pass_When_User_Registered()
	{
		var provider = Build(
			s => s.AddSingleton(User(authenticated: true)),
			s => s.AddPermission(EmptyCodeSource.Instance, TestAssembly),
			s => s.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver()));

		Assert.Same(provider, provider.ValidatePermissionSetup());
	}

	#endregion

	#region 辅助

	private static UserPrincipal User(bool authenticated, params string[] roles)
	{
		var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		// ClaimsIdentity 的 IsAuthenticated 就是「认证类型是否非空」，用它精确控制认证状态
		var identity = new ClaimsIdentity(
			claims,
			authenticated ? "Bearer" : null,
			ClaimTypes.Name,
			ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

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
