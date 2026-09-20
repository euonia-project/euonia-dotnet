using System.Linq.Expressions;
using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 Euonia.Osba 的数据权限（ScopePolicy / IScopeModel / ScopeSubjectSet）：
/// 允许-拒绝代数、表达式下推、单一真值来源、审计、缓存与写侧强制。
/// </summary>
public class ScopeTests
{
	#region 表达式下推

	[Fact]
	public void Grant_ShouldCompileToContainsExpression_NotDelegate()
	{
		var model = Descriptor<ScopedRepo>();
		var policy = ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept);

		var compiled = ScopePolicyCompiler.Compile(policy, model, Subjects(("dept", "team-a")));

		// 必须是表达式，且形态是可翻译的集合成员判断
		var call = Assert.IsAssignableFrom<MethodCallExpression>(compiled.Allow.Body);
		Assert.Equal(nameof(Enumerable.Contains), call.Method.Name);
		Assert.Equal(typeof(Enumerable), call.Method.DeclaringType);
		Assert.Contains("x.TeamId", compiled.Allow.ToString());
	}

	[Fact]
	public void Apply_ShouldProduceExpressionBasedWhere_NotEnumerableWhere()
	{
		var model = Descriptor<ScopedRepo>();
		var compiled = ScopePolicyCompiler.Compile(Policy(), model, Subjects(("dept", "team-a")));

		var rows = new[] { Repo("team-a"), Repo("team-b") };
		var query = ScopeFilter.Apply(rows.AsQueryable(), compiled);

		var where = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);

		// Queryable.Where（表达式）而不是 Enumerable.Where（委托）——这是能下推的前提
		Assert.Equal(typeof(Queryable), where.Method.DeclaringType);
		Assert.Equal(nameof(Queryable.Where), where.Method.Name);

		// 参数是引用包装的 LambdaExpression（表达式树），不是 Func 委托常量
		var quoted = Assert.IsAssignableFrom<UnaryExpression>(where.Arguments[1]);
		Assert.Equal(ExpressionType.Quote, quoted.NodeType);
		Assert.IsAssignableFrom<LambdaExpression>(quoted.Operand);
	}

	[Fact]
	public void Apply_ShouldNotIntroduceInvokeOrClientEvaluation()
	{
		var model = Descriptor<ScopedRepo>();
		var compiled = ScopePolicyCompiler.Compile(Policy(), model, Subjects(("dept", "team-a"), ("owner", "dev")));

		var query = ScopeFilter.Apply(new[] { Repo("team-a") }.AsQueryable(), compiled);

		var visitor = new ForbiddenNodeVisitor();
		visitor.Visit(query.Expression);

		Assert.Empty(visitor.Forbidden);
	}

	[Fact]
	public void EmptySubjects_ShouldProduceConstantFalse_NotEmptyIn()
	{
		var model = Descriptor<ScopedRepo>();
		var compiled = ScopePolicyCompiler.Compile(ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept), model, ScopeSubjectSet.Empty);

		// 未授予任何值：直接恒假，不生成空 IN ()
		Assert.IsAssignableFrom<ConstantExpression>(compiled.Allow.Body);
		Assert.False((bool)((ConstantExpression)compiled.Allow.Body).Value);
	}

	#endregion

	#region 单一真值来源：下推与内存求值必须一致

	[Fact]
	public void Pushdown_And_InMemoryEvaluation_ShouldAgree()
	{
		var model = Descriptor<ScopedRepo>();
		var subjects = Subjects(("dept", "team-a"), ("region", "east"));

		var rows = new[]
		{
			Repo("team-a", level: "normal"),
			Repo("team-a", level: "secret"),   // 命中 deny
			Repo("team-b", region: "east"),
			Repo("team-b", region: "west"),
			Repo("team-c", owner: "dev"),
			Repo("team-c", owner: "other"),
		};

		foreach (var policy in new[] { Policy(), AnyRegionOrDept(), DenyOnly() })
		{
			var compiled = ScopePolicyCompiler.Compile(policy, model, subjects);

			var pushed = ScopeFilter.Apply(rows.AsQueryable(), compiled).ToList();
			var inMemory = rows.Where(row => ScopeFilter.Allows(row, compiled)).ToList();

			Assert.Equal(pushed.Count, inMemory.Count);
			foreach (var row in pushed)
			{
				Assert.Contains(row, inMemory);
			}
		}
	}

	#endregion

	#region 允许 / 拒绝代数

	[Fact]
	public void Any_WithDenyOnlyBranch_ShouldNotDegradeToAllowAll()
	{
		// 关键回归：若把 Deny 分支的允许条件记作恒真，这里会变成「除机密外全放行」的静默提权
		var model = Descriptor<ScopedRepo>();
		var subjects = Subjects(("dept", "team-b"));

		var policy = ScopePolicy<ScopedRepo>.Any(
			ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept),
			ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret")));

		var compiled = ScopePolicyCompiler.Compile(policy, model, subjects);

		// 本部门的普通行：放行
		Assert.True(ScopeFilter.Allows(Repo("team-b", level: "normal"), compiled));

		// 本部门的机密行：被 deny 拦下
		Assert.False(ScopeFilter.Allows(Repo("team-b", level: "secret"), compiled));

		// 非本部门且非机密的行：不得因为「带了一个 deny 分支」而被放行
		Assert.False(ScopeFilter.Allows(Repo("team-z", level: "normal"), compiled));
	}

	[Fact]
	public void Any_OfOnlyDenyBranches_ShouldDenyEverything()
	{
		var model = Descriptor<ScopedRepo>();
		var compiled = ScopePolicyCompiler.Compile(
			ScopePolicy<ScopedRepo>.Any(
				ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "a")),
				ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "b"))),
			model,
			Subjects(("dept", "team-a"), ("owner", "dev")));

		Assert.False(compiled.HasAllow);
		Assert.False(ScopeFilter.Allows(Repo("team-a"), compiled));
	}

	[Fact]
	public void Deny_ShouldOverrideGrant_EvenInNestedAny()
	{
		var model = Descriptor<ScopedRepo>();
		var subjects = Subjects(("dept", "team-a"));

		var policy = ScopePolicy<ScopedRepo>.All(
			ScopePolicy<ScopedRepo>.Any(
				ScopePolicy<ScopedRepo>.Self(),
				ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept)),
			ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret")));

		var compiled = ScopePolicyCompiler.Compile(policy, model, subjects);

		// 在部门范围内 → 放行；但同一个部门下的机密行，deny 依然压过 grant
		Assert.True(ScopeFilter.Allows(Repo("team-a", level: "normal"), compiled));
		Assert.False(ScopeFilter.Allows(Repo("team-a", level: "secret"), compiled));

		// 不在任何范围内 → 仍然拒绝
		Assert.False(ScopeFilter.Allows(Repo("team-z", level: "normal"), compiled));
	}

	[Fact]
	public void All_OfOnlyDenyBranches_ShouldActAsDenyList()
	{
		// 只有拒绝条件是合法的「拒绝清单」语义：除命中项外全部放行
		var model = Descriptor<ScopedRepo>();
		var compiled = ScopePolicyCompiler.Compile(
			ScopePolicy<ScopedRepo>.All(ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret"))),
			model,
			ScopeSubjectSet.Empty);

		Assert.False(compiled.HasAllow);
		Assert.True(ScopeFilter.Allows(Repo("any-team", level: "normal"), compiled));
		Assert.False(ScopeFilter.Allows(Repo("any-team", level: "secret"), compiled));
	}

	[Fact]
	public void Any_CrossDimension_ShouldUseOr()
	{
		var model = Descriptor<ScopedRepo>();
		var subjects = Subjects(("region", "east"));

		var compiled = ScopePolicyCompiler.Compile(
			ScopePolicy<ScopedRepo>.Any(
				ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Region),
				ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Project)),
			model,
			subjects);

		// 命中区域维度即可，不需要同时命中项目维度
		Assert.True(ScopeFilter.Allows(Repo("team-z", region: "east"), compiled));
		Assert.False(ScopeFilter.Allows(Repo("team-z", region: "west"), compiled));

		// 项目维度未被授予 → 该分支不成立，但区域维度成立依然放行
		Assert.True(ScopeFilter.Allows(Repo("team-z", region: "east", project: "p2"), compiled));
		Assert.False(ScopeFilter.Allows(Repo("team-z", region: null, project: "p1"), compiled));
	}

	[Fact]
	public void Self_ShouldRequireOwnerGrant_AndBeRevocable()
	{
		var model = Descriptor<ScopedRepo>();
		var policy = ScopePolicy<ScopedRepo>.Self();

		// 解析器授予了 owner 维度 → 本人可访问
		Assert.True(ScopeFilter.Allows(Repo("team-z", owner: "dev"), ScopePolicyCompiler.Compile(policy, model, Subjects(("owner", "dev")))));

		// 解析器未授予 owner 维度 → 本人也不可访问（所有者关系可撤销，不再是硬编码特例）
		Assert.False(ScopeFilter.Allows(Repo("team-z", owner: "dev"), ScopePolicyCompiler.Compile(policy, model, Subjects(("dept", "team-a")))));
	}

	[Fact]
	public void Classification_ShouldNotGrantAccess()
	{
		var model = Descriptor<ScopedRepo>();

		// 分类属性不参与授权：把 level 当作维度名去 Grant 会落到未映射维度上
		Assert.Throws<InvalidOperationException>(() =>
			ScopePolicyCompiler.Compile(ScopePolicy<ScopedRepo>.Grant("level"), model, Subjects(("level", "public"))));
	}

	[Fact]
	public void Compile_UnmappedDimension_ShouldThrow()
	{
		var exception = Assert.Throws<InvalidOperationException>(() =>
			ScopePolicyCompiler.Compile(ScopePolicy<ScopedRepo>.Grant("nonexistent"), Descriptor<ScopedRepo>(), Subjects(("nonexistent", "x"))));

		Assert.Contains("nonexistent", exception.Message);
	}

	#endregion

	#region 构造期校验

	[Fact]
	public void All_Or_Any_WithoutChild_ShouldThrow()
	{
		Assert.Throws<InvalidOperationException>(() => ScopePolicy<ScopedRepo>.All());
		Assert.Throws<InvalidOperationException>(() => ScopePolicy<ScopedRepo>.Any());
	}

	[Fact]
	public void Deny_Nested_ShouldThrow()
	{
		var inner = ScopePolicy<ScopedRepo>.Where(x => x.Level == "a");

		Assert.Throws<InvalidOperationException>(() => ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Deny(inner)));
	}

	#endregion

	#region 审计

	[Fact]
	public void Explain_ShouldReportMatchingPaths()
	{
		var model = Descriptor<ScopedRepo>();
		var subjects = Subjects(("dept", "team-a"));

		var decision = ScopeFilter.Explain(Repo("team-a", level: "secret"), Policy(), model, subjects);

		Assert.False(decision.Allowed);
		Assert.Contains(decision.MatchedAllows, item => item.Contains("dept"));
		Assert.Contains(decision.MatchedDenies, item => item.Contains("secret") || item.Contains("Where"));
	}

	#endregion

	#region 缓存契约

	[Fact]
	public void Guard_ShouldResolveSubjectsOncePerScope()
	{
		var resolver = new CountingScopeResolver();
		using var scope = CreateScope(User("dev"), resolver, out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();

		for (var index = 0; index < 5; index++)
		{
			_ = guard.Apply(new[] { Repo("team-a") }.AsQueryable()).ToList();
			_ = guard.Allows(Repo("team-a"));
		}

		Assert.Equal(1, resolver.CallCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Guard_Refresh_ShouldPickUpAuthorizationChanges()
	{
		var resolver = new CountingScopeResolver();
		using var scope = CreateScope(User("dev"), resolver, out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.False(guard.Allows(Repo("team-c")));

		// 授权数据变化：改数据即可，无需改代码
		resolver.Grant(ScopeDimensions.Dept, "team-c");
		guard.Refresh();

		Assert.True(guard.Allows(Repo("team-c")));
		Assert.Equal(2, resolver.CallCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Guard_UnmodeledType_ShouldBeUnconstrained()
	{
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.Null(guard.GetPolicy<ScopedRepoOther>());
		Assert.True(guard.AllowsObject(new ScopedRepoOther()));
		Assert.Equal(0, new CountingScopeResolver().CallCount);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 写侧强制

	[Fact]
	public async Task CreateAsync_ShouldNotBeScopeChecked_BeforeCallerPopulates()
	{
		// Create 只构造对象、不落库，且按设计由调用方随后填充字段（框架自带示例 User.CreateAsync
		// 也只填 Username）。此时若做数据范围判定，正常流程会被误杀，而它又保护不了任何东西。
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		var repo = await factory.CreateAsync<ScopedRepo>();

		Assert.NotNull(repo);
		Assert.Equal(ObjectEditState.New, repo.State);

		// 调用方填充完成后再保存 —— 此时才做判定
		repo.TeamId = "team-a";
		await repo.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task CreateAsync_ThenSaveOutOfScope_ShouldStillBeDenied()
	{
		// 上一条不是「Create 路径没有权限」：落库那一刻仍会被拦下
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var repo = await factory.CreateAsync<ScopedRepo>();

		repo.TeamId = "team-c";      // 不在授予范围内

		// 落库前被拦下（新增路径：自动注入的范围规则先命中，故为验证错误）
		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => repo.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(exception.Errors, error => error.ErrorMessage.Contains("数据范围"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_OutOfScope_OnUpdate_ShouldFailWithValidationException()
	{
		// 框架对已声明模型的类型自动注入范围规则，越权「更新」在规则阶段即以验证错误暴露。
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var repo = Repo("team-c");
		repo.BusinessContext = provider.GetRequiredService<BusinessContext>();
		repo.MarkAsChanged();

		var exception = await Assert.ThrowsAsync<ValidationException>(() => repo.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(exception.Errors, error => error.ErrorMessage.Contains("数据范围"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_OutOfScope_OnDelete_ShouldFailWithSecurityException()
	{
		// 不对称是既有事实：EditableObject 在 IsDeleted 时默认跳过对象级规则，
		// 因此越权「删除」由工厂边界兜住，抛的是 SecurityException。
		// 这条断言把该行为钉住——若哪天规则覆盖了删除，这里会红，提醒同步更新文档。
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var repo = Repo("team-c");
		repo.BusinessContext = provider.GetRequiredService<BusinessContext>();
		repo.MarkAsDeleted();

		var exception = await Assert.ThrowsAsync<SecurityException>(() => repo.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("Data scope denied", exception.Message);
		Assert.Contains("code=", exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_InScope_ShouldSucceed()
	{
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var repo = Repo("team-a");
		repo.BusinessContext = provider.GetRequiredService<BusinessContext>();
		repo.MarkAsChanged();

		var result = await repo.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_UnmodeledType_ShouldNotBeAffected()
	{
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var other = new ScopedRepoOther { TeamId = "whatever" };
		other.BusinessContext = provider.GetRequiredService<BusinessContext>();
		other.MarkAsChanged();

		var result = await other.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task UpdateAsync_Criteria_ShouldNotMisfire_WhenRowIsInScope()
	{
		// criteria 入口在调用前是空对象，范围列尚未赋值：
		// 若按「前置」判定会误杀一切，这里断言后置判定不会误杀。
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		var updated = await factory.UpdateAsync<ScopedTask>("team-a");

		Assert.Equal("team-a", updated.TeamId);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task UpdateAsync_Criteria_OutOfScope_ShouldThrowAfterwards()
	{
		using var scope = CreateScope(User("dev"), new CountingScopeResolver(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		await Assert.ThrowsAsync<SecurityException>(() => factory.UpdateAsync<ScopedTask>("team-c"));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Helpers

	private static ScopePolicy<ScopedRepo> Policy()
	{
		return ScopePolicy<ScopedRepo>.All(
			ScopePolicy<ScopedRepo>.Any(
				ScopePolicy<ScopedRepo>.Self(),
				ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept)),
			ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret")));
	}

	private static ScopePolicy<ScopedRepo> AnyRegionOrDept()
	{
		return ScopePolicy<ScopedRepo>.Any(
			ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Region),
			ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept));
	}

	private static ScopePolicy<ScopedRepo> DenyOnly()
	{
		return ScopePolicy<ScopedRepo>.All(
			ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret")));
	}

	private static ScopeModelDescriptor Descriptor<T>()
		where T : class
	{
		var model = (IScopeModel<T>)Activator.CreateInstance(
			typeof(T).Assembly.GetTypes().First(type => type.IsClass && !type.IsAbstract && typeof(IScopeModel<T>).IsAssignableFrom(type)));

		return ScopeModelDescriptor.Create(model);
	}

	private static ScopeSubjectSet Subjects(params (string Dimension, string Value)[] subjects)
	{
		var builder = ScopeSubjectSet.CreateBuilder();
		foreach (var (dimension, value) in subjects)
		{
			builder.Add(dimension, value);
		}

		return builder.Build();
	}

	private static ScopedRepo Repo(string teamId, string region = null, string owner = null, string level = "normal", string project = null)
	{
		return new ScopedRepo { TeamId = teamId, Region = region, OwnerId = owner, Level = level, ProjectId = project };
	}

	private static IServiceScope CreateScope(UserPrincipal user, IScopeSubjectResolver resolver, out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopedRepo).Assembly);
		services.AddSingleton(resolver);
		if (user != null)
		{
			services.AddSingleton(user);
		}

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;
		return scope;
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[
				new Claim(UserClaimTypes.Subject, userId)
			],
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 断言表达式中不含客户端求值痕迹的访问器。
/// </summary>
internal sealed class ForbiddenNodeVisitor : ExpressionVisitor
{
	public List<string> Forbidden { get; } = [];

	protected override Expression VisitMethodCall(MethodCallExpression node)
	{
		if (node.Method.Name is nameof(Enumerable.AsEnumerable) or "ToList" or "ToArray" or "Compile" or "Invoke")
		{
			Forbidden.Add(node.Method.Name);
		}

		return base.VisitMethodCall(node);
	}

	protected override Expression VisitInvocation(InvocationExpression node)
	{
		Forbidden.Add("Invoke");
		return base.VisitInvocation(node);
	}
}

/// <summary>
/// 记录调用次数、并可在运行期增删授予的测试范围解析器（模拟授权数据变化）。
/// </summary>
public class CountingScopeResolver : IScopeSubjectResolver
{
	private readonly List<ScopeSubject> _subjects = [];

	public int CallCount { get; private set; }

	public void Grant(string dimension, string value)
	{
		_subjects.Add(new ScopeSubject(dimension, value));
	}

	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		CallCount++;

		var builder = ScopeSubjectSet.CreateBuilder();
		builder.AddSelf("dev");
		builder.Add(ScopeDimensions.Dept, "team-a");

		foreach (var subject in _subjects)
		{
			builder.Add(subject.Dimension, subject.Value);
		}

		return ValueTask.FromResult(builder.Build());
	}
}

/// <summary>
/// 受数据权限约束的可编辑业务对象。
/// </summary>
public class ScopedRepo : EditableObject<ScopedRepo>
{
	public string OwnerId { get; set; }

	public string TeamId { get; set; }

	public string Region { get; set; }

	public string ProjectId { get; set; }

	public string Level { get; set; }

	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	protected override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 未声明权限模型的业务对象：不受数据权限约束。
/// </summary>
public class ScopedRepoOther : EditableObject<ScopedRepoOther>
{
	public string TeamId { get; set; }

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="ScopedRepo"/> 的权限模型与策略。
/// </summary>
public sealed class ScopedRepoModel : ScopeModel<ScopedRepo>
{
	public override void Define(ScopeModelBuilder<ScopedRepo> builder)
	{
		builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
		       .Map(ScopeDimensions.Dept, x => x.TeamId)
		       .Map(ScopeDimensions.Region, x => x.Region)
		       .Map(ScopeDimensions.Project, x => x.ProjectId)
		       .Classify("level", x => x.Level);
	}

	public override ScopePolicy<ScopedRepo> Policy =>
		ScopePolicy<ScopedRepo>.All(
			ScopePolicy<ScopedRepo>.Any(
				ScopePolicy<ScopedRepo>.Self(),
				ScopePolicy<ScopedRepo>.Grant(ScopeDimensions.Dept)),
			ScopePolicy<ScopedRepo>.Deny(ScopePolicy<ScopedRepo>.Where(x => x.Level == "secret")));
}

/// <summary>
/// 具备 criteria 形式工厂方法的受控资源：用于验证「目标由工厂方法填充」路径的写侧判定。
/// </summary>
public class ScopedTask : EditableObject<ScopedTask>
{
	public string TeamId { get; set; }

	/// <summary>
	/// 按条件更新：范围列由本方法内部填充。
	/// </summary>
	/// <param name="teamId">团队标识。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[FactoryUpdate]
	protected async Task UpdateAsync(string teamId, CancellationToken cancellationToken = default)
	{
		TeamId = teamId;
		await Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="ScopedTask"/> 的权限模型与策略。
/// </summary>
public sealed class ScopedTaskModel : ScopeModel<ScopedTask>
{
	public override void Define(ScopeModelBuilder<ScopedTask> builder)
	{
		builder.Map(ScopeDimensions.Dept, x => x.TeamId);
	}

	public override ScopePolicy<ScopedTask> Policy => ScopePolicy<ScopedTask>.Grant(ScopeDimensions.Dept);
}
