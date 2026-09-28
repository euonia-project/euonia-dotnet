using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.BrokenFixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 子表维度（<see cref="ScopeModelBuilder{T}.MapMany"/>）：取值来自子表（关系表）的数据权限。
/// <para>
/// 场景就是「查询我加入的团队 / 家庭 / 组织」——「谁属于这个资源」存在子表里，
/// 判定因此下推为 <c>EXISTS</c> 子查询，而不是由解析器把关系反向展开成扁平 id 集合。
/// </para>
/// </summary>
public class CollectionDimensionTests
{
	#region 编译形态：只产出可下推的那一种形状

	[Fact]
	public void MapMany_ShouldCompileTo_AnyOverContains_NotProjectedAny()
	{
		var compiled = Compile<Workspace, WorkspaceModel>(
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Member),
			(ScopeDimensions.Member, "dev"));

		var any = Assert.IsAssignableFrom<MethodCallExpression>(compiled.Allow.Body);

		Assert.Equal(typeof(Enumerable), any.Method.DeclaringType);
		Assert.Equal(nameof(Enumerable.Any), any.Method.Name);

		// 谓词是「元素的值属于授予集合」，而不是先投射再判存在
		var predicate = Assert.IsAssignableFrom<LambdaExpression>(any.Arguments[1]);
		var contains = Assert.IsAssignableFrom<MethodCallExpression>(predicate.Body);

		Assert.Equal(nameof(Enumerable.Contains), contains.Method.Name);

		var text = compiled.Allow.ToString();

		Assert.Contains("x.Members", text);
		Assert.DoesNotContain("Select", text);
	}

	[Fact]
	public void MapMany_ShouldKeepChildFilter_InsideAny_AndDropProjection()
	{
		var compiled = Compile<Channel, ChannelModel>(
			ScopePolicy<Channel>.Grant(ScopeDimensions.Member),
			(ScopeDimensions.Member, "dev"));

		var text = compiled.Allow.ToString();

		// 子表属性（status）留在子查询里由数据库求值，而不是解析期的一次性快照
		Assert.Contains("Where", text);
		Assert.Contains("Any", text);
		Assert.Contains("Contains", text);
		Assert.DoesNotContain("Select", text);
		Assert.DoesNotContain("Convert", text);
	}

	[Fact]
	public void MapMany_ShouldSupport_BareCollectionSelector()
	{
		var compiled = Compile<SharedDocument, SharedDocumentModel>(
			ScopePolicy<SharedDocument>.Grant(SharedDocumentModel.Reader),
			(SharedDocumentModel.Reader, "dev"));

		Assert.True(ScopeFilter.Allows(new SharedDocument { ReaderIds = ["dev"] }, compiled));
		Assert.False(ScopeFilter.Allows(new SharedDocument { ReaderIds = ["other"] }, compiled));
		Assert.False(ScopeFilter.Allows(new SharedDocument { ReaderIds = [] }, compiled));
	}

	#endregion

	#region 单一真值来源：下推与内存求值必须一致

	[Fact]
	public void MapMany_Pushdown_And_InMemoryEvaluation_ShouldAgree()
	{
		var compiled = Compile<Workspace, WorkspaceModel>(
			new WorkspaceModel().Policy,
			(ScopeDimensions.Member, "dev"),
			(ScopeDimensions.Owner, "dev"));

		var rows = new[]
		{
			Workspace("w1", ["dev", "other"]),           // 成员命中
			Workspace("w2", ["other"]),                  // 都不是
			Workspace("w3", [], owner: "dev"),           // 负责人命中
			Workspace("w4", ["dev"], owner: "other")     // 成员命中
		};

		var pushed = ScopeFilter.Apply(rows.AsQueryable(), compiled).ToList();
		var inMemory = rows.Where(row => ScopeFilter.Allows(row, compiled)).ToList();

		Assert.Equal(3, pushed.Count);
		Assert.Equal(inMemory.Count, pushed.Count);

		foreach (var row in pushed)
		{
			Assert.Contains(row, inMemory);
		}
	}

	[Fact]
	public void MapMany_ChildAttributeFilter_ShouldApply_ToBothPaths()
	{
		var compiled = Compile<Channel, ChannelModel>(
			ScopePolicy<Channel>.Grant(ScopeDimensions.Member),
			(ScopeDimensions.Member, "dev"));

		var active = Channel("c1", ("dev", "active"));
		var inactive = Channel("c2", ("dev", "inactive"));

		Assert.True(ScopeFilter.Allows(active, compiled));
		Assert.False(ScopeFilter.Allows(inactive, compiled));

		var pushed = ScopeFilter.Apply(new[] { active, inactive }.AsQueryable(), compiled).ToList();

		Assert.Same(active, Assert.Single(pushed));
	}

	#endregion

	#region 允许 / 拒绝代数不受影响

	[Fact]
	public void MapMany_EmptyGrant_ShouldProduceConstantFalse()
	{
		var compiled = ScopePolicyCompiler.Compile(
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Member),
			ScopeModelDescriptor.Create(new WorkspaceModel()),
			ScopeSubjectSet.Empty);

		// 未授予任何值：恒假常量——不生成空 IN ()，也不生成空 EXISTS
		var body = Assert.IsAssignableFrom<ConstantExpression>(compiled.Allow.Body);

		Assert.False((bool)body.Value);
	}

	[Fact]
	public void MapMany_Deny_ShouldOverride_AndCompileToExistenceCondition()
	{
		var policy = ScopePolicy<Workspace>.All(
			ScopePolicy<Workspace>.Where(_ => true),
			ScopePolicy<Workspace>.Deny(ScopePolicy<Workspace>.Grant(ScopeDimensions.Member)));

		var compiled = Compile<Workspace, WorkspaceModel>(policy, (ScopeDimensions.Member, "dev"));

		Assert.Contains("Any", compiled.Deny.ToString());

		// 命中子表关系的行被否决，其余放行（拒绝清单语义）
		Assert.False(ScopeFilter.Allows(Workspace("w1", ["dev"]), compiled));
		Assert.True(ScopeFilter.Allows(Workspace("w2", ["other"]), compiled));

		var rows = new[] { Workspace("w1", ["dev"]), Workspace("w2", ["other"]) };
		var pushed = ScopeFilter.Apply(rows.AsQueryable(), compiled).ToList();

		Assert.Same(rows[1], Assert.Single(pushed));
	}

	[Fact]
	public void MapMany_DenyOnlyInsideAny_ShouldStillDenyEverything()
	{
		// fail-closed 不变：Any 之下只有拒绝条件时，「没有允许条件」不等于「允许条件为真」
		var policy = ScopePolicy<Workspace>.Any(
			ScopePolicy<Workspace>.Deny(ScopePolicy<Workspace>.Grant(ScopeDimensions.Member)));

		var compiled = Compile<Workspace, WorkspaceModel>(policy, (ScopeDimensions.Member, "dev"));

		Assert.False(compiled.HasAllow);
		Assert.False(ScopeFilter.Allows(Workspace("w1", ["dev"]), compiled));
		Assert.False(ScopeFilter.Allows(Workspace("w2", ["other"]), compiled));
	}

	#endregion

	#region 单行判定的对象图要求

	[Fact]
	public void MapMany_UnloadedCollection_ShouldFailLoudly_WithDimensionAndFix()
	{
		var guard = Guard();

		// 子集合未加载（空引用）：判定不了就失败，绝不静默拒绝
		var unloaded = new Workspace { Id = "w1", OwnerId = "other" };

		var exception = Assert.Throws<InvalidOperationException>(() => guard.Allows(unloaded));

		Assert.Contains(ScopeDimensions.Member, exception.Message);
		Assert.Contains("Apply", exception.Message);

		// 审计路径给出同一个明确错误，而不是被反射包成 TargetInvocationException
		var audit = Assert.Throws<InvalidOperationException>(() => guard.Explain(unloaded));

		Assert.Contains(ScopeDimensions.Member, audit.Message);

		// 下推路径不做任何加载探测：Apply 只搬运表达式，查询由提供程序求值（EF 会翻译成 EXISTS）。
		// 这里只断言构造查询这一步不抛——断言「未加载也能查出结果」属于提供程序的地盘，
		// 由 EfCore 的翻译用例（Euonia.Osba.Tests）钉住。
		Assert.NotNull(guard.Apply(new[] { unloaded }.AsQueryable()));
	}

	[Fact]
	public void MapMany_PolicyWithoutCollectionDimension_ShouldNotRequireLoadedMembers()
	{
		// 加载探测只针对「本策略确实引用到的」子表维度：
		// 同一资源上只按行内列判定的策略（写侧的典型形状）不要求对象图完整。
		var compiled = Compile<Workspace, WorkspaceModel>(
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Owner),
			(ScopeDimensions.Owner, "dev"));

		Assert.True(ScopeFilter.Allows(new Workspace { OwnerId = "dev", Members = null }, compiled));
		Assert.False(ScopeFilter.Allows(new Workspace { OwnerId = "other", Members = null }, compiled));
	}

	[Fact]
	public void MapMany_CollectionInitializedToEmpty_ShouldBeDeniedSilently()
	{
		// 已知边界（DESIGN §2）：实体把集合初始化成空集合时，「未加载」与「确实没有成员」无法区分，
		// 于是表现为拒绝而不是报错。本用例钉住这一行为：它是有意接受的边界，不是可以顺手「修」成放行的缺口。
		var compiled = Compile<Workspace, WorkspaceModel>(
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Member),
			(ScopeDimensions.Member, "dev"));

		Assert.False(ScopeFilter.Allows(new Workspace { Members = [] }, compiled));
	}

	#endregion

	#region 声明期与注册期校验

	[Fact]
	public void MapMany_And_Map_ShouldReject_SameDimension()
	{
		Assert.Throws<InvalidOperationException>(() => new ScopeModelBuilder<Workspace>()
			.Map(ScopeDimensions.Member, x => x.Id)
			.MapMany(ScopeDimensions.Member, x => x.Members.Select(m => m.UserId)));

		Assert.Throws<InvalidOperationException>(() => new ScopeModelBuilder<Workspace>()
			.MapMany(ScopeDimensions.Member, x => x.Members.Select(m => m.UserId))
			.Map(ScopeDimensions.Member, x => x.Id));
	}

	[Fact]
	public void MapMany_UnmappedDimension_ShouldFail_EvenWithoutGrants()
	{
		// 与单值维度一致：即便用户在该维度上没有任何授予，未映射的维度也必须暴露为错误
		var exception = Assert.Throws<InvalidOperationException>(() => ScopePolicyCompiler.Compile(
			ScopePolicy<Workspace>.Grant("nonexistent"),
			ScopeModelDescriptor.Create(new WorkspaceModel()),
			ScopeSubjectSet.Empty));

		Assert.Contains("未映射的维度", exception.Message);
	}

	[Fact]
	public void MapMany_UnsupportedShape_ShouldFail_AtRegistration()
	{
		// 不受支持的取值形状在注册期就失败：可下推的形状只有一种，不能等到查询时让提供程序去猜
		var exception = Assert.Throws<ScopeModelValidationException>(() => new ScopeModelRegistryBuilder()
			.Add<UnsupportedCollectionModel>()
			.Build(EmptyCodeSource.Instance));

		Assert.Contains("不受支持", exception.Message);
		Assert.Contains("tags", exception.Message);
	}

	[Fact]
	public void MapMany_ModelWithOnlyCollectionDimensions_ShouldPassRegistryValidation()
	{
		// 启动期校验用探针值覆盖每个维度，因此子表维度同样会被真实编译一次，
		// 不会误报「策略结构性恒不放行」。
		var registry = new ScopeModelRegistryBuilder()
			.Add<ChannelModel>()
			.Build(EmptyCodeSource.Instance);

		Assert.True(registry.IsDeclared(typeof(Channel)));
	}

	[Fact]
	public void MapMany_Explain_ShouldMarkCollectionLeaf()
	{
		var descriptor = ScopeModelDescriptor.Create(new WorkspaceModel());
		var subjects = Subjects((ScopeDimensions.Member, "dev"));

		var decision = ScopeFilter.Explain(
			Workspace("w1", ["dev"]),
			ScopePolicy<Workspace>.Grant(ScopeDimensions.Member),
			descriptor,
			subjects);

		Assert.True(decision.Allowed);
		Assert.Contains("Grant(member[])", decision.MatchedAllows);
	}

	#endregion

	#region 辅助

	private static CompiledScopePolicy<T> Compile<T, TModel>(ScopePolicy<T> policy, params (string Dimension, string Value)[] grants)
		where T : class
		where TModel : IScopeModel<T>, new()
	{
		return ScopePolicyCompiler.Compile(policy, ScopeModelDescriptor.Create(new TModel()), Subjects(grants));
	}

	private static ScopeSubjectSet Subjects(params (string Dimension, string Value)[] grants)
	{
		var builder = ScopeSubjectSet.CreateBuilder();

		foreach (var (dimension, value) in grants)
		{
			builder.Add(dimension, value);
		}

		return builder.Build();
	}

	private static IScopeGuard Guard()
	{
		var services = new ServiceCollection();

		services.AddSingleton(User());
		services.AddPermission(EmptyCodeSource.Instance, typeof(WorkspaceModel).Assembly);
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(
			grants: [(ScopeDimensions.Member, "dev"), (ScopeDimensions.Owner, "dev")]));

		return services.BuildServiceProvider().GetRequiredService<IScopeGuard>();
	}

	private static UserPrincipal User()
	{
		var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "tester")], "Bearer", ClaimTypes.Name, ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static Workspace Workspace(string id, string[] members, string owner = "other")
	{
		return new Workspace
		{
			Id = id,
			OwnerId = owner,
			Members = [.. members.Select(member => new WorkspaceMember { UserId = member, Status = "active" })]
		};
	}

	private static Channel Channel(string id, params (string UserId, string Status)[] members)
	{
		return new Channel
		{
			Id = id,
			Members = [.. members.Select(member => new ChannelMember { UserId = member.UserId, Status = member.Status })]
		};
	}

	#endregion
}
