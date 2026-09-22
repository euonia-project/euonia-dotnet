using System.Linq.Expressions;
using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证三点增强：行级操作权限（同一用户、同一类型、不同行权限不同）、
/// 操作权限改为数据来源（撤销立即生效）、以及权限与 Rule 体系的适配。
/// </summary>
public class ScopeRowPermissionTests
{
	#region 行级操作权限

	[Fact]
	public void RowLevel_SameUserSameType_DifferentRowsDifferentRights()
	{
		// A1 可 push + delete；A2 仅可 push；A3 都不行
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		// push：A1、A2 通过，A3 拒绝
		Assert.True(guard.Allows(Repo("A1"), "repo:push"));
		Assert.True(guard.Allows(Repo("A2"), "repo:push"));
		Assert.False(guard.Allows(Repo("A3"), "repo:push"));

		// delete：仅 A1 通过
		Assert.True(guard.Allows(Repo("A1"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A3"), "repo:delete"));

		// 同一行 A2 在两个码下结论不同 —— 这正是「行级操作权限」的验收点
		Assert.True(guard.Allows(Repo("A2"), "repo:push"));
		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void RowLevel_CodeGrantShouldOverrideDefault_NotUnion()
	{
		// 默认键上给了 repo = {A1,A2,A3}，但 repo:delete 码上只给了 {A1}。
		// 若实现成「并集」，delete 会拿到 {A1,A2,A3} —— 行级差异直接失效。
		var resolver = new AclResolver();
		resolver.GrantDefault("repo", "A1", "A2", "A3");
		resolver.GrantCode("repo:delete", "repo", "A1");

		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		// delete 在码上被收窄到 {A1}：默认键上的 {A1,A2,A3} 不得「并」进来
		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A3"), "repo:delete"));
		Assert.True(guard.Allows(Repo("A1"), "repo:delete"));

		// 同理，push 在码上是 {A1,A2}，A3 不会因为默认键里有它而通过
		Assert.False(guard.Allows(Repo("A3"), "repo:push"));
		Assert.True(guard.Allows(Repo("A2"), "repo:push"));

		// 未在码上单独授予时才会回落到默认键
		Assert.True(guard.Allows(Repo("A3"), null));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Pushdown_PerCode_ShouldAgreeWithInMemory()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var rows = new[] { Repo("A1"), Repo("A2"), Repo("A3") };

		foreach (var code in new[] { "repo:push", "repo:delete" })
		{
			var pushed = guard.Apply(rows.AsQueryable(), code).ToList();
			var inMemory = rows.Where(row => guard.Allows(row, code)).ToList();

			Assert.Equal(pushed.Count, inMemory.Count);
			foreach (var row in pushed)
			{
				Assert.Contains(row, inMemory);
			}
		}

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Pushdown_PerCode_ShouldKeepExpressionShape()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var compiled = guard.GetPolicy<GrantRepo>("repo:delete");

		// 仍是可翻译的集合成员判断
		var call = Assert.IsAssignableFrom<MethodCallExpression>(compiled.Allow.Body);
		Assert.Equal(nameof(Enumerable.Contains), call.Method.Name);
		Assert.Contains("x.RepoId", compiled.Allow.ToString());

		// 下推仍走表达式重载；且表达式里不得出现权限码本身
		var query = guard.Apply(new[] { Repo("A1") }.AsQueryable(), "repo:delete");
		var where = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);

		Assert.Equal(typeof(Queryable), where.Method.DeclaringType);
		Assert.DoesNotContain("repo:delete", query.Expression.ToString());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Explain_ShouldReportScopeKey()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var decision = guard.Explain(Repo("A2"), "repo:delete");

		Assert.False(decision.Allowed);
		Assert.Equal("repo:delete", decision.ScopeKey);
		Assert.Contains("repo:delete", decision.ToString());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void GenericAndNonGenericEntries_ShouldAgreeOnSameObject()
	{
		// guard.Allows<T>(row) 与 guard.AllowsObject(row) 必须给出同一答案：
		// 二者都用「对象当前操作」解析策略键，否则同一对象会有两套结论。
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		// 无未决操作 → 回落到默认键
		var idle = Repo("A2");
		Assert.Equal(guard.Allows(idle), guard.AllowsObject(idle));

		// 有未决操作 → 按该操作解析：A2 可 push（Update）但不可 delete
		var pushable = Repo("A2");
		pushable.MarkAsChanged();
		Assert.Equal(guard.Allows(pushable), guard.AllowsObject(pushable));
		Assert.True(guard.Allows(pushable));

		var deletable = Repo("A2");
		deletable.MarkAsDeleted();
		Assert.Equal(guard.Allows(deletable), guard.AllowsObject(deletable));
		Assert.False(guard.Allows(deletable));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 写侧：行级操作权限在工厂边界生效

	[Fact]
	public async Task SaveAsync_Update_ShouldUseDeclaredCodePolicy()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		// A2 可 push：更新通过
		var allowed = Repo("A2");
		allowed.BusinessContext = provider.GetRequiredService<BusinessContext>();
		allowed.MarkAsChanged();

		await allowed.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Delete_ShouldEnforceDeletePolicy()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		// A2 不可 delete：越权删除由工厂边界兜住（IsDeleted 时默认跳过对象级规则）
		var denied = Repo("A2");
		denied.BusinessContext = provider.GetRequiredService<BusinessContext>();
		denied.MarkAsDeleted();

		var exception = await Assert.ThrowsAsync<SecurityException>(() => denied.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("repo:delete", exception.Message);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 操作权限数据来源：撤销立即生效

	[Fact]
	public void RevokedPermission_ShouldTakeEffectWithoutReissuingToken()
	{
		var resolver = new AclResolver();

		// 关键安全断言：全程使用同一个既不含任何 perm 声明的 ClaimsPrincipal，只改授权数据
		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();
		var target = Repo("A2");

		Assert.True(guard.Allows(target, "repo:push"));

		// 撤销授权：数据层移除该码
		resolver.RevokeCode("repo:push");

		// 同一作用域内仍是旧快照——钉住「按请求缓存」这一事实，避免文档写出「立即生效」的假承诺
		Assert.True(guard.Allows(target, "repo:push"));

		// 显式失效后立即生效
		guard.Refresh();
		Assert.False(guard.Allows(target, "repo:push"));

		// 新作用域自然拿到新授权
		using var next = CreateScope(resolver, out var nextProvider);
		Assert.False(nextProvider.GetRequiredService<IScopeGuard>().Allows(target, "repo:push"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Permissions_ShouldComeFromResolver_NotClaims()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var user = provider.GetRequiredService<UserPrincipal>();

		// 用户在令牌里没有任何权限声明
		Assert.Empty(user.FindClaims(UserClaimTypes.Permission));

		// 但权限码仍然可用——来自授权数据
		Assert.Contains("repo:push", provider.GetRequiredService<IScopeGuard>().Permissions);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Rule 体系适配

	[Fact]
	public async Task AutoInjectedScopeRule_ShouldFailUpdateWithValidationError()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var denied = Repo("A3");   // 任何码下都无权
		denied.BusinessContext = provider.GetRequiredService<BusinessContext>();
		denied.MarkAsChanged();

		// 框架对已声明模型的类型自动注入范围规则：越权更新在保存前以验证错误暴露
		var exception = await Assert.ThrowsAsync<Nerosoft.Euonia.Validation.ValidationException>(
			() => denied.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(exception.Errors, error => error.ErrorMessage.Contains("数据范围"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ManualPermissionRule_ShouldReportMissingPermission()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		// 规则按类型共享（进程级静态存储）：每个测试必须使用独立的对象类型，
		// 否则上一个测试注册的规则会泄漏到本测试的规则检查里（GitHub Actions 顺序不定即因此失败）。
		var obj = new MissingRuleProbeObject { BusinessContext = provider.GetRequiredService<BusinessContext>() };
		obj.PublicRules.AddRule(new PermissionRule("repo:force-push"));

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.False(obj.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ManualPermissionRule_ShouldPassWhenGranted()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var obj = new GrantedRuleProbeObject { BusinessContext = provider.GetRequiredService<BusinessContext>() };
		obj.PublicRules.AddRule(new PermissionRule("repo:push"));

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(obj.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccessRow_ShouldAllowBranchingInsideBusinessMethods()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var probe = new GrantRepoProbe { RepoId = "A2", BusinessContext = provider.GetRequiredService<BusinessContext>() };

		Assert.True(probe.ProbeRowAccess("repo:push"));
		Assert.False(probe.ProbeRowAccess("repo:delete"));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 启动期校验

	[Fact]
	public void ValidatePermissionSetup_ShouldFailWhenResolverMissing()
	{
		var services = new ServiceCollection();

		// 测试程序集里存在 [Permission] 声明与权限模型 => 必须有解析器
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);

		var provider = services.BuildServiceProvider();

		var exception = Assert.Throws<InvalidOperationException>(() => provider.ValidatePermissionSetup());

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	[Fact]
	public void ValidatePermissionSetup_ShouldPassWhenResolverRegistered()
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddSingleton<IScopeSubjectResolver>(new AclResolver());

		var provider = services.BuildServiceProvider();

		Assert.Same(provider, provider.ValidatePermissionSetup());
	}

	[Fact]
	public void PolicySet_ShouldRejectReservedPermissionCode()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		// 保留命名空间属于框架，应用不得声明
		var exception = Assert.Throws<InvalidOperationException>(
			() => policies.For("@custom", ScopePolicy<GrantRepo>.Where(_ => true)));

		Assert.Contains(ScopeKeys.Prefix, exception.Message);
	}

	[Fact]
	public void PolicySet_ShouldAllowFrameworkDefaultKeys()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		// 框架自身用 For(BusinessOperation) 落在保留键上，不得被上面的校验拦住
		policies.For(BusinessOperation.Create, ScopePolicy<GrantRepo>.Where(_ => true));

		Assert.Contains(ScopeKeys.Create, policies.Codes);
	}

	[Fact]
	public void PolicySet_ShouldRejectDuplicateCode()
	{
		var policies = new ScopePolicySet<GrantRepo>();
		policies.For("repo:push", ScopePolicy<GrantRepo>.Where(_ => true));

		Assert.Throws<InvalidOperationException>(() => policies.For("repo:push", ScopePolicy<GrantRepo>.Where(_ => true)));
	}

	#endregion

	#region Helpers

	private static IServiceScope CreateScope(IScopeSubjectResolver resolver, out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddSingleton(resolver);
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;
		return scope;
	}

	private static GrantRepo Repo(string repoId)
	{
		return new GrantRepo { RepoId = repoId };
	}

	/// <summary>
	/// 构造一个<b>不含任何权限声明</b>的已认证用户：权限码只能来自授权数据。
	/// </summary>
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
/// 模拟资源级 ACL 的授权数据解析器：权限码与「按码分组的资源授予」都在数据侧，可随时变更。
/// </summary>
public class AclResolver : IScopeSubjectResolver
{
	private readonly HashSet<string> _codes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _codeGrants = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _defaultGrants = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 初始化 <see cref="AclResolver"/> 的新实例，默认给出 push 与 delete 两个码的行级授予。
	/// </summary>
	public AclResolver()
	{
		_codes.Add("repo:push");
		_codes.Add("repo:delete");

		GrantCode("repo:push", "repo", "A1", "A2");
		GrantCode("repo:delete", "repo", "A1");
	}

	/// <summary>
	/// 授予权限码。
	/// </summary>
	/// <param name="code">权限码。</param>
	public void GrantCode(string code)
	{
		_codes.Add(code);
	}

	/// <summary>
	/// 撤销权限码。
	/// </summary>
	/// <param name="code">权限码。</param>
	public void RevokeCode(string code)
	{
		_codes.Remove(code);

		// 一并清掉该码上的行级授予：撤销授权意味着这个码上什么都不能做
		foreach (var key in _codeGrants.Keys.Where(key => key.StartsWith($"{code}|", StringComparison.Ordinal)).ToArray())
		{
			_codeGrants.Remove(key);
		}
	}

	/// <summary>
	/// 在指定权限码下授予维度值。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <param name="dimension">维度名。</param>
	/// <param name="values">值。</param>
	public void GrantCode(string code, string dimension, params string[] values)
	{
		GrantCode(code);

		if (!_codeGrants.TryGetValue($"{code}|{dimension}", out var set))
		{
			set = new HashSet<string>(StringComparer.Ordinal);
			_codeGrants[$"{code}|{dimension}"] = set;
		}

		foreach (var value in values)
		{
			set.Add(value);
		}
	}

	/// <summary>
	/// 在默认键上授予维度值（对所有权限码生效，除非该码有自己的授予）。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="values">值。</param>
	public void GrantDefault(string dimension, params string[] values)
	{
		if (!_defaultGrants.TryGetValue(dimension, out var set))
		{
			set = new HashSet<string>(StringComparer.Ordinal);
			_defaultGrants[dimension] = set;
		}

		foreach (var value in values)
		{
			set.Add(value);
		}
	}

	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		var builder = ScopeSubjectSet.CreateBuilder().AddCodes(_codes);

		foreach (var (dimension, values) in _defaultGrants)
		{
			builder.AddGrant(null, dimension, values);
		}

		foreach (var (key, values) in _codeGrants)
		{
			var separator = key.IndexOf('|');
			builder.AddGrant(key[..separator], key[(separator + 1)..], values);
		}

		return ValueTask.FromResult(builder.Build());
	}
}

/// <summary>
/// 受行级操作权限约束的资源：A1 可 push+delete、A2 仅可 push。
/// </summary>
public class GrantRepo : EditableObject<GrantRepo>
{
	public string RepoId { get; set; }

	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	[Permission("repo:push")]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	[Permission("repo:delete")]
	protected override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="GrantRepo"/> 的权限模型：资源标识本身也是一个维度，因此可以按行授权。
/// </summary>
public sealed class GrantRepoModel : ScopeModel<GrantRepo>
{
	public override void Define(ScopeModelBuilder<GrantRepo> builder)
	{
		// 把「资源标识」映射为维度，行级 ACL 才可表达
		builder.Map("repo", x => x.RepoId);
	}

	/// <summary>
	/// 默认策略：在默认键上没有 repo 授予时一律拒绝。
	/// </summary>
	public override ScopePolicy<GrantRepo> Policy => ScopePolicy<GrantRepo>.Grant("repo");

	public override void Declare(ScopePolicySet<GrantRepo> policies)
	{
		// 只为行级操作权限声明策略；Create 会落到默认策略（Grant("repo")）——
		// 保存新行时字段已填完，判定才有意义。
		policies.For(BusinessOperation.Read, ScopePolicy<GrantRepo>.Grant("repo"));

		// 行级操作权限：各自的行范围由解析器按码给出
		policies.For("repo:push", ScopePolicy<GrantRepo>.Grant("repo"));
		policies.For("repo:delete", ScopePolicy<GrantRepo>.Grant("repo"));
	}
}

/// <summary>
/// 暴露 <see cref="BusinessObject.CanAccessRow"/> 的探针。
/// 必须派生自受控类型本身，否则该类型不参与数据权限。
/// </summary>
public class GrantRepoProbe : GrantRepo
{
	/// <summary>
	/// 调用 <see cref="BusinessObject.CanAccessRow(string)"/>。
	/// </summary>
	/// <param name="scopeKey">权限码。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool ProbeRowAccess(string scopeKey) => CanAccessRow(scopeKey);
}

/// <summary>
/// 用于手工注册规则、断言「缺少权限」的测试对象（独立类型，避免规则跨测试泄漏）。
/// </summary>
public class MissingRuleProbeObject : ObservableObject<MissingRuleProbeObject>
{
	/// <summary>
	/// 公开规则集合以便测试调用。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;
}

/// <summary>
/// 用于手工注册规则、断言「权限已授予」的测试对象（独立类型，避免规则跨测试泄漏）。
/// </summary>
public class GrantedRuleProbeObject : ObservableObject<GrantedRuleProbeObject>
{
	/// <summary>
	/// 公开规则集合以便测试调用。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;
}
