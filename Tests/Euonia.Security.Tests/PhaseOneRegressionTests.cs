using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 权限引擎第一批修复的回归护栏：把「关掉修复就会红」的判定钉在测试里。
/// </summary>
/// <remarks>
/// 这里每条测试都对应一个具体的越权/失真路径，而不是泛泛的用例覆盖；
/// 判定口径类的改动（大小写、null、只读视图）一旦回退，应当由本文件直接报红。
/// </remarks>
public class PhaseOneRegressionTests
{
	private static readonly Assembly TestAssembly = typeof(PhaseOneRegressionTests).Assembly;

	private static readonly Assembly FixturesAssembly = typeof(Widget).Assembly;

	#region 行级授权的接线

	[Fact]
	public void ObjectScopeAuthorizer_Should_See_Models_Registered_By_A_Later_AddPermission()
	{
		// 回归：IObjectScopeAuthorizer 曾在首次注册时被直接 new 出实例并捕获当时的注册表；
		// 第二次 AddPermission 换掉了容器里的 ScopeModelRegistry，TryAddSingleton 却因描述符已存在
		// 而不再登记 —— 于是第二个模块的模型被判为「不受约束」，行级数据权限被静默跳过（fail-open）。
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(TestAssembly); p.Source(RunCodeSource()); });
		services.AddPermission(p => { p.Scan(FixturesAssembly); p.NoOperationCodes(); });

		using var provider = services.BuildServiceProvider();

		var authorizer = provider.GetRequiredService<IObjectScopeAuthorizer>();

		Assert.True(authorizer.IsConstrained(typeof(Widget)));
	}

	#endregion

	#region 授权数据不得被调用方回写

	[Fact]
	public void SubjectSet_Should_Not_Hand_Out_Writable_Collections()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
									  .AddCode("repo:delete")
									  .Add(ScopeDimensions.Dept, "team-a")
									  .Build();

		// Codes 原本直接返回底层 HashSet：强转回去 Add 一个 'admin:*' 就等于凭空发权限码
		var codes = Assert.IsAssignableFrom<ICollection<string>>(subjects.Codes);
		Assert.Throws<NotSupportedException>(() => codes.Add("admin:*"));
		Assert.DoesNotContain("admin:*", subjects.Codes);

		// KeysWithGrants 同理
		var keys = Assert.IsAssignableFrom<ICollection<string>>(subjects.KeysWithGrants);
		Assert.Throws<NotSupportedException>(() => keys.Add("repo:edit"));
		Assert.DoesNotContain("repo:edit", subjects.KeysWithGrants);

		// 维度值集合原本直接返回底层 HashSet：改写它等于改写别人的授权数据。
		// 现在返回副本 —— 强转后仍然写得动，但写的是副本，回不到授权数据上。
		var values = Assert.IsAssignableFrom<ICollection<string>>(subjects.ValuesOf(ScopeKeys.Default, ScopeDimensions.Dept));
		values.Add("team-forged");
		Assert.DoesNotContain("team-forged", subjects.ValuesOf(ScopeKeys.Default, ScopeDimensions.Dept));
	}

	[Fact]
	public void Shared_Empty_Instance_Should_Not_Be_Writable()
	{
		// Empty 是全体未认证用户共享的静态实例，向它注入一个码就是给匿名用户发权限
		var codes = Assert.IsAssignableFrom<ICollection<string>>(ScopeSubjectSet.Empty.Codes);

		Assert.Throws<NotSupportedException>(() => codes.Add("admin:*"));

		Assert.Empty(ScopeSubjectSet.Empty.Codes);
	}

	[Fact]
	public void Build_Should_Publish_A_Snapshot_Not_The_Builder_State()
	{
		var builder = ScopeSubjectSet.CreateBuilder()
									 .Add(ScopeDimensions.Dept, "team-a");

		var snapshot = builder.Build();

		// 构建之后继续追加，不得改动已经发布出去的那份授权数据
		builder.AddCode("repo:edit");
		builder.AddGrant("repo:edit", ScopeDimensions.Dept, "team-b");

		Assert.DoesNotContain("repo:edit", snapshot.Codes);
		Assert.Equal(["team-a"], snapshot.ValuesOf("repo:edit", ScopeDimensions.Dept));
	}

	#endregion

	#region 权限码大小写口径

	[Fact]
	public void Type_Gate_And_Row_Grant_Lookup_Should_Agree_On_Case()
	{
		// 回归：HoldsPermission 一直按 OrdinalIgnoreCase 比较，而按码取授予是 Ordinal。
		// 差值 = "REPO:DELETE" 通过类型级闸门、却在行级落空并回落到更宽的 @default。
		var subjects = ScopeSubjectSet.CreateBuilder()
									  .AddCode("repo:delete")
									  .Add(ScopeDimensions.Dept, "team-default")
									  .AddGrant("repo:delete", ScopeDimensions.Dept, "team-scoped")
									  .Build();

		Assert.True(subjects.HoldsPermission("REPO:DELETE"));
		Assert.True(subjects.HoldsPermission("repo:delete"));

		// 两个入口必须给同一个码同一个答案：既然闸门放行，行级就该取到该码自己的授予
		Assert.Equal(["team-scoped"], subjects.ValuesOf("REPO:DELETE", ScopeDimensions.Dept));
		Assert.Equal(["team-scoped"], subjects.ValuesOf("repo:delete", ScopeDimensions.Dept));
	}

	[Fact]
	public void Dimension_Name_Case_Insensitive_Value_Case_Sensitive_Rule_Should_Hold()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
									  .Add("Dept", "TeamA")
									  .Build();

		Assert.True(subjects.Contains(ScopeKeys.Default, "DEPT", "TeamA"));
		Assert.False(subjects.Contains(ScopeKeys.Default, "dept", "teama"));
	}

	#endregion

	#region null 资源的判定与解释必须同口径

	[Fact]
	public void Null_Resource_Should_Be_Refused_By_Both_Judging_And_Explaining()
	{
		// 回归：Explain(null) 曾返回「未注册权限模型，不受数据权限约束 allowed=true」，
		// 而 AllowsObject(null) 返回 false —— 审计给出的解释比判定本身更宽松，排障会读反。
		using var provider = BuildGuard();

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.False(guard.AllowsObject(null));
		Assert.Contains("Decision: deny", guard.ExplainObject(null), StringComparison.OrdinalIgnoreCase);
		// 未注册权限模型的类型：null 资源同样判不出，不得因「类型不受约束」而放行
		Assert.False(guard.Allows<ClassLevelAsset>(null));
		Assert.False(guard.Explain<ClassLevelAsset>(null).Allowed);

		// 已注册权限模型的类型：两个入口一致
		Assert.False(guard.Allows<Asset>(null));
		Assert.False(guard.Explain<Asset>(null).Allowed);
	}

	#endregion

	#region 注册期

	[Fact]
	public void Repeating_An_Idempotent_AddPermission_Should_Not_Rebuild_The_Registry()
	{
		// 回归：每次 AddPermission 都整表重建 —— 重扫全部程序集、重新实例化并逐条重新编译校验
		// 每一个模型。输入一个字节都没变时，重复调用应当是真正的空操作。
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); });

		using (var first = services.BuildServiceProvider())
		{
			var before = first.GetRequiredService<ScopeModelRegistry>();

			services.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); });

			using var second = services.BuildServiceProvider();
			var after = second.GetRequiredService<ScopeModelRegistry>();

			Assert.Same(before, after);
		}

		// 空操作之后注册内容依旧完整
		using var third = services.BuildServiceProvider();
		var registry = third.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(Asset)));
		Assert.True(third.GetRequiredService<PermissionRegistration>().RequiresSubjectResolver);
	}

	[Fact]
	public void Adding_A_New_Module_Should_Still_Rebuild()
	{
		// 反向护栏：签名跳过只覆盖「输入没变」，新增模块必须照常重建
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(TestAssembly); p.Source(RunCodeSource()); });

		using var before = services.BuildServiceProvider();
		var registryBefore = before.GetRequiredService<ScopeModelRegistry>();

		services.AddPermission(p => { p.Scan(FixturesAssembly); p.NoOperationCodes(); });

		using var after = services.BuildServiceProvider();
		var registryAfter = after.GetRequiredService<ScopeModelRegistry>();

		Assert.NotSame(registryBefore, registryAfter);
		Assert.True(registryAfter.IsDeclared(typeof(Widget)));
	}

	#endregion

	#region 撤销生效

	[Fact]
	public void Refresh_Should_Recompile_The_Policy_From_Fresh_Authorization_Data()
	{
		// 契约护栏：撤销之后，后续判定拿到的编译策略必须基于撤销后的授权数据。
		//（「在途编译把陈旧策略回写进缓存」这条竞态由 ScopeGuard.GetPolicy 的失效代数比对挡住，
		//  它的窗口只存在于编译内部，没有可注入的接缝，故无法在这里做成确定性触发。）
		var resolver = new SwappableSubjectResolver();
		using var provider = BuildGuard(resolver);

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new Asset { DeptId = "team-a" }));

		resolver.Grant("team-b");
		guard.Refresh();

		Assert.False(guard.Allows(new Asset { DeptId = "team-a" }));
		Assert.True(guard.Allows(new Asset { DeptId = "team-b" }));
	}

	#endregion

	#region Helpers

	private static ServiceProvider BuildGuard(IScopeSubjectResolver resolver = null)
	{
		var services = new ServiceCollection();

		services.AddSingleton(User());
		services.AddPermission(p => { p.Scan(TestAssembly); p.NoOperationCodes(); });
		services.AddSingleton<IScopeSubjectResolver>(resolver ?? new FixedSubjectResolver(grants: [(ScopeDimensions.Dept, "team-a")]));

		return services.BuildServiceProvider();
	}

	private static UserPrincipal User()
	{
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "tester")],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static IPermissionCodeSource RunCodeSource() =>
		OperationCodeSource.Create()
			.OnMethodName(BusinessOperation.Execute, "Run")
			.Build();

	#endregion
}

/// <summary>
/// 授权数据可中途替换的解析器：用于验证撤销之后的策略重新编译。
/// </summary>
internal sealed class SwappableSubjectResolver : IScopeSubjectResolver
{
	private ScopeSubjectSet _subjects = Build("team-a");

	public void Grant(string dept)
	{
		_subjects = Build(dept);
	}

	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
		=> new(_subjects);

	private static ScopeSubjectSet Build(string dept)
	{
		return ScopeSubjectSet.CreateBuilder()
							  .AddSelf("tester")
							  .Add(ScopeDimensions.Dept, dept)
							  .Build();
	}
}
