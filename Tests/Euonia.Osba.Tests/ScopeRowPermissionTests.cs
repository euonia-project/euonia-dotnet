using System.Linq.Expressions;
using System.Security;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证行级操作权限：同一用户对不同行、不同操作的授权彼此独立，并覆盖表达式下推、审计、
/// 写侧强制、启动期校验与多模块注册。
/// </summary>
public class ScopeRowPermissionTests
{
	#region 行级操作权限

	/// <summary>
	/// 同一用户面对同类型的多行时，各行的更新与删除权限应独立判定：
	/// <c>A1</c> 两种操作均可，<c>A2</c> 仅可更新，<c>A3</c> 均不可。
	/// </summary>
	[Fact]
	public void RowLevel_SameUserSameType_DifferentRowsDifferentRights()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(Repo("A1"), BusinessOperation.Update));
		Assert.True(guard.Allows(Repo("A2"), BusinessOperation.Update));
		Assert.False(guard.Allows(Repo("A3"), BusinessOperation.Update));

		Assert.True(guard.Allows(Repo("A1"), BusinessOperation.Delete));
		Assert.False(guard.Allows(Repo("A2"), BusinessOperation.Delete));
		Assert.False(guard.Allows(Repo("A3"), BusinessOperation.Delete));

		Assert.True(guard.Allows(Repo("A2"), BusinessOperation.Update));
		Assert.False(guard.Allows(Repo("A2"), BusinessOperation.Delete));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 权限码授予应覆盖默认行范围而不是与其取并集：默认可见 <c>A1</c>、<c>A2</c>、<c>A3</c>，
	/// 但 <c>repo:delete</c> 仅授予 <c>A1</c> 时，只有 <c>A1</c> 允许删除。
	/// </summary>
	[Fact]
	public void RowLevel_CodeGrantShouldOverrideDefault_NotUnion()
	{
		var resolver = new AclResolver();
		resolver.GrantDefault("repo", "A1", "A2", "A3");
		resolver.GrantCode("repo:delete", "repo", "A1");

		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.False(guard.Allows(Repo("A2"), BusinessOperation.Delete));
		Assert.False(guard.Allows(Repo("A3"), BusinessOperation.Delete));
		Assert.True(guard.Allows(Repo("A1"), BusinessOperation.Delete));

		Assert.False(guard.Allows(Repo("A3"), BusinessOperation.Update));
		Assert.True(guard.Allows(Repo("A2"), BusinessOperation.Update));

		Assert.True(guard.Allows(Repo("A3"), null));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 按操作下推得到的查询结果应与逐行内存判定完全一致。
	/// </summary>
	[Fact]
	public void Pushdown_PerOperation_ShouldAgreeWithInMemory()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var rows = new[] { Repo("A1"), Repo("A2"), Repo("A3") };

		foreach (var operation in new[] { BusinessOperation.Update, BusinessOperation.Delete })
		{
			var pushed = guard.Apply(rows.AsQueryable(), operation).ToList();
			var inMemory = rows.Where(row => guard.Allows(row, operation)).ToList();

			Assert.Equal(pushed.Count, inMemory.Count);
			foreach (var row in pushed)
			{
				Assert.Contains(row, inMemory);
			}
		}

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 下推表达式应保持可翻译形态：允许条件是 <c>Contains</c> 调用、查询由 <see cref="Queryable"/>
	/// 的 <c>Where</c> 构成，且权限码文本不会泄漏到表达式里。
	/// </summary>
	[Fact]
	public void Pushdown_PerOperation_ShouldKeepExpressionShape()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var compiled = guard.GetPolicy<GrantRepo>(BusinessOperation.Delete)!;

		var call = Assert.IsAssignableFrom<MethodCallExpression>(compiled.Allow.Body);
		Assert.Equal(nameof(Enumerable.Contains), call.Method.Name);
		Assert.Contains("x.RepoId", compiled.Allow.ToString());

		var query = guard.Apply(new[] { Repo("A1") }.AsQueryable(), BusinessOperation.Delete);
		var where = Assert.IsAssignableFrom<MethodCallExpression>(query.Expression);

		Assert.Equal(typeof(Queryable), where.Method.DeclaringType);
		Assert.DoesNotContain("repo:delete", query.Expression.ToString());

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 审计结果应报告命中的范围键 <c>repo:delete</c>，并将该行标记为拒绝。
	/// </summary>
	[Fact]
	public void Explain_ShouldReportScopeKey()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var decision = guard.Explain(Repo("A2"), BusinessOperation.Delete);

		Assert.False(decision.Allowed);
		Assert.Equal("repo:delete", decision.ScopeKey);
		Assert.Contains("repo:delete", decision.ToString());

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 用权限码寻址与用操作寻址必须落到同一条声明，判定结果与范围键都应一致。
	/// </summary>
	[Fact]
	public void Addressing_By_Grant_Key_ShouldMatch_Addressing_By_Operation()
	{
		// 调用点常写权限码（<c>AllowsObject(row, RepositoryPermissions.Delete)</c>），
		// 而模型是按操作声明并指定了键。两个名字必须落到同一条声明上：
		// 用码寻址若退回默认策略，判定结果会比声明的行范围宽，而且没有任何征兆。
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var byOperation = guard.Explain(Repo("A2"), BusinessOperation.Delete);
		var byCode = guard.Explain(Repo("A2"), "repo:delete");

		Assert.Equal(byOperation.Allowed, byCode.Allowed);
		Assert.Equal(byOperation.ScopeKey, byCode.ScopeKey);
		Assert.Equal("repo:delete", byCode.ScopeKey);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 泛型与非泛型入口对同一对象应给出一致结果，包括未变更、待更新与待删除三种状态。
	/// </summary>
	[Fact]
	public void GenericAndNonGenericEntries_ShouldAgreeOnSameObject()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		var idle = Repo("A2");
		Assert.Equal(guard.Allows(idle), guard.AllowsObject(idle));

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

	/// <summary>
	/// 更新保存应使用声明的 <c>repo:push</c> 码策略：处于授予范围内的行可成功保存。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Update_ShouldUseDeclaredCodePolicy()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var allowed = Repo("A2");
		allowed.BusinessContext = provider.GetRequiredService<BusinessContext>();
		allowed.MarkAsChanged();

		await allowed.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 删除保存应执行 <c>repo:delete</c> 策略：无权时抛出 <see cref="SecurityException"/>，
	/// 且异常消息包含该权限码。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Delete_ShouldEnforceDeletePolicy()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var denied = Repo("A2");
		denied.BusinessContext = provider.GetRequiredService<BusinessContext>();
		denied.MarkAsDeleted();

		var exception = await Assert.ThrowsAsync<SecurityException>(() => denied.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("repo:delete", exception.Message);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 操作权限数据来源：撤销立即生效

	/// <summary>
	/// 撤销权限码后无需重新签发令牌：刷新守卫即应生效，新创建的作用域同样拒绝该操作。
	/// </summary>
	[Fact]
	public async Task RevokedPermission_ShouldTakeEffectWithoutReissuingToken()
	{
		var resolver = new AclResolver();

		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();
		var target = Repo("A2");

		Assert.True(guard.Allows(target, BusinessOperation.Update));

		resolver.RevokeCode("repo:push");

		Assert.True(guard.Allows(target, BusinessOperation.Update));

		await guard.RefreshAsync(TestContext.Current.CancellationToken);
		Assert.False(guard.Allows(target, BusinessOperation.Update));

		using var next = CreateScope(resolver, out var nextProvider);
		Assert.False(nextProvider.GetRequiredService<IScopeGuard>().Allows(target, "repo:push"));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 操作权限应来自 <see cref="IScopeSubjectResolver"/> 而非用户的 Claims：用户没有权限声明，
	/// 但守卫仍能读到解析器给出的 <c>repo:push</c>。
	/// </summary>
	[Fact]
	public void Permissions_ShouldComeFromResolver_NotClaims()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var user = provider.GetRequiredService<UserPrincipal>();

		Assert.Empty(user.FindClaims("permission"));

		Assert.Contains("repo:push", provider.GetRequiredService<IScopeGuard>().Permissions);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 权限线与验证线彼此独立

	/// <summary>
	/// 越权更新应抛出 <see cref="SecurityException"/>，消息包含 <c>Data scope denied</c>。
	/// </summary>
	[Fact]
	public async Task SaveAsync_Update_OutOfScope_ShouldFailWithSecurityException()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var denied = Repo("A3");   // 任何码下都无权
		denied.BusinessContext = provider.GetRequiredService<BusinessContext>();
		denied.MarkAsChanged();

		var exception = await Assert.ThrowsAsync<SecurityException>(
			() => denied.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("Data scope denied", exception.Message);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 业务方法内部可用行可见性检查做分支：示例行对更新可见、对删除不可见。
	/// </summary>
	[Fact]
	public void Row_Visible_Check_ShouldAllowBranchingInsideBusinessMethods()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var probe = new GrantRepoProbe { RepoId = "A2", BusinessContext = provider.GetRequiredService<BusinessContext>() };

		Assert.True(probe.ProbeRowAccess(BusinessOperation.Update));
		Assert.False(probe.ProbeRowAccess(BusinessOperation.Delete));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 启动期校验

	/// <summary>
	/// 未注册 <see cref="IScopeSubjectResolver"/> 时解析 <see cref="IScopeGuard"/> 应抛出
	/// <see cref="InvalidOperationException"/>，并在消息中指明缺失的类型。
	/// </summary>
	[Fact]
	public void GuardResolution_ShouldFailWhenResolverMissing()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ScopeRowPermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });

		var provider = services.BuildServiceProvider();

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	/// <summary>
	/// 注册解析器与当前用户后，应能成功解析 <see cref="IScopeGuard"/>。
	/// </summary>
	[Fact]
	public void GuardResolution_ShouldPassWhenResolverRegistered()
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ScopeRowPermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddSingleton<IScopeSubjectResolver>(new AclResolver());
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "tester")],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		services.AddSingleton(new UserPrincipal(new ClaimsPrincipal(identity)));

		var provider = services.BuildServiceProvider();

		provider.GetRequiredService<IScopeGuard>();
	}

	/// <summary>
	/// 策略集应拒绝以框架保留前缀 <c>@</c> 开头的自定义范围键，并抛出
	/// <see cref="InvalidOperationException"/>。
	/// </summary>
	[Fact]
	public void PolicySet_ShouldRejectReservedScopeKey()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		var exception = Assert.Throws<InvalidOperationException>(
			() => policies.ForOperation(BusinessOperation.Update, ScopePolicy<GrantRepo>.Where(_ => true), "@custom"));

		Assert.Contains(ScopeKeys.Prefix, exception.Message);
	}

	/// <summary>
	/// 策略集应允许使用框架默认键（如 <see cref="BusinessOperation.Create"/>）声明操作策略。
	/// </summary>
	[Fact]
	public void PolicySet_ShouldAllowFrameworkDefaultKeys()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		policies.ForOperation(BusinessOperation.Create, ScopePolicy<GrantRepo>.Where(_ => true));

		Assert.Contains(BusinessOperation.Create, policies.Identifiers);
	}

	/// <summary>
	/// 同一操作重复声明策略应抛出 <see cref="InvalidOperationException"/>。
	/// </summary>
	[Fact]
	public void PolicySet_ShouldRejectDuplicateOperation()
	{
		var policies = new ScopePolicySet<GrantRepo>();
		policies.ForOperation(BusinessOperation.Update, ScopePolicy<GrantRepo>.Where(_ => true));

		Assert.Throws<InvalidOperationException>(() => policies.ForOperation(BusinessOperation.Update, ScopePolicy<GrantRepo>.Where(_ => true)));
	}

	#endregion

	#region Helpers

	private static IServiceScope CreateScope(IScopeSubjectResolver resolver, out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ScopeRowPermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddSingleton(resolver);
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider.Warm();
		return scope;
	}

	private static GrantRepo Repo(string repoId)
	{
		return new GrantRepo { RepoId = repoId };
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
/// 测试用访问控制列表解析器：持有权限码，以及按权限码或按维度默认授予的行范围。
/// </summary>
public class AclResolver : IScopeSubjectResolver
{
	private readonly HashSet<string> _codes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _codeGrants = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _defaultGrants = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 初始化解析器：默认授予 <c>repo:push</c>（<c>A1</c>、<c>A2</c>）与 <c>repo:delete</c>（<c>A1</c>）。
	/// </summary>
	public AclResolver()
	{
		_codes.Add("repo:push");
		_codes.Add("repo:delete");

		GrantCode("repo:push", "repo", "A1", "A2");
		GrantCode("repo:delete", "repo", "A1");
	}

	/// <summary>
	/// 授予权限码但不附带任何行范围。
	/// </summary>
	/// <param name="code">要授予的权限码。</param>
	public void GrantCode(string code)
	{
		_codes.Add(code);
	}

	/// <summary>
	/// 撤销权限码及其对应的所有行范围授予。
	/// </summary>
	/// <param name="code">要撤销的权限码。</param>
	public void RevokeCode(string code)
	{
		_codes.Remove(code);

		foreach (var key in _codeGrants.Keys.Where(key => key.StartsWith($"{code}|", StringComparison.Ordinal)).ToArray())
		{
			_codeGrants.Remove(key);
		}
	}

	/// <summary>
	/// 授予权限码，并为该码附加指定维度上可见的行范围。
	/// </summary>
	/// <param name="code">要授予的权限码。</param>
	/// <param name="dimension">范围维度名称。</param>
	/// <param name="values">该维度下可见的值。</param>
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
	/// 登记指定维度的默认行范围，供没有专属权限码授予的请求使用。
	/// </summary>
	/// <param name="dimension">范围维度名称。</param>
	/// <param name="values">该维度下默认可见的值。</param>
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

	/// <summary>
	/// 解析当前用户的权限码与行范围授予，并组合为 <see cref="ScopeSubjectSet"/>。
	/// </summary>
	/// <param name="user">当前用户主体，可为 <see langword="null"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>包含权限码与行范围授予的主体集合。</returns>
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
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
/// 受行级权限约束的测试仓储对象：更新与删除分别要求 <c>repo:push</c> 与 <c>repo:delete</c>。
/// </summary>
public class GrantRepo : EditableObject<GrantRepo>
{
	/// <summary>
	/// 仓储标识，映射到 <c>repo</c> 范围维度。
	/// </summary>
	public string RepoId { get; set; } = string.Empty;

	/// <summary>
	/// 插入工厂方法：本测试对象不落库，直接完成。
	/// </summary>
	[FactoryInsert]
	protected internal override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 更新工厂方法：需要 <c>repo:push</c> 权限，本测试对象不落库。
	/// </summary>
	[FactoryUpdate]
	[Permission("repo:push")]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 删除工厂方法：需要 <c>repo:delete</c> 权限，本测试对象不落库。
	/// </summary>
	[FactoryDelete]
	[Permission("repo:delete")]
	protected internal override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 为 <see cref="GrantRepo"/> 声明 <c>repo</c> 维度及按操作的行级策略。
/// </summary>
public sealed class GrantRepoModel : ScopeModel<GrantRepo>
{
	/// <summary>
	/// 将 <c>repo</c> 维度映射到 <see cref="GrantRepo.RepoId"/>。
	/// </summary>
	/// <param name="builder">范围模型构建器。</param>
	public override void Define(ScopeModelBuilder<GrantRepo> builder)
	{
		builder.Map("repo", x => x.RepoId);
	}

	/// <summary>
	/// 默认策略：要求 <c>repo</c> 维度的授予。
	/// </summary>
	public override ScopePolicy<GrantRepo> Policy => ScopePolicy<GrantRepo>.Grant("repo");

	/// <summary>
	/// 按操作声明策略：更新绑定 <c>repo:push</c>，删除绑定 <c>repo:delete</c>，读取使用默认策略。
	/// </summary>
	/// <param name="policies">当前类型的策略集。</param>
	public override void Declare(ScopePolicySet<GrantRepo> policies)
	{
		policies.ForOperation(BusinessOperation.Read, ScopePolicy<GrantRepo>.Grant("repo"));

		policies.ForOperation(BusinessOperation.Update, ScopePolicy<GrantRepo>.Grant("repo"), "repo:push");
		policies.ForOperation(BusinessOperation.Delete, ScopePolicy<GrantRepo>.Grant("repo"), "repo:delete");
	}
}

/// <summary>
/// 在业务方法内部探测行可见性的 <see cref="GrantRepo"/> 派生探针。
/// </summary>
public class GrantRepoProbe : GrantRepo
{
	/// <summary>
	/// 业务方法内部的行级分支：直接问 <c>IScopeGuard</c> 本行是否可见（查询语义，不抛异常）。
	/// </summary>
	public bool ProbeRowAccess(string scopeKey)
	{
		var guard = BusinessContext?.CurrentServiceProvider.GetService<IScopeGuard>();

		return guard == null || guard.AllowsObject(this, scopeKey);
	}
}

/// <summary>
/// 验证多个模块先后注册权限时，范围模型与权限码来源不会互相覆盖。
/// </summary>
public class MultiModulePermissionTests
{
	/// <summary>
	/// 多模块场景下的报表行，按所有者与部门映射范围维度。
	/// </summary>
	public sealed class ReportRow
	{
		/// <summary>
		/// 报表行标识。
		/// </summary>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// 所有者标识，映射到 <see cref="ScopeDimensions.Owner"/> 维度。
		/// </summary>
		public string OwnerId { get; set; } = string.Empty;

		/// <summary>
		/// 部门标识，映射到 <see cref="ScopeDimensions.Dept"/> 维度。
		/// </summary>
		public string DeptId { get; set; } = string.Empty;
	}

	/// <summary>
	/// 为 <see cref="ReportRow"/> 声明所有者与部门维度，并采用部门授权策略。
	/// </summary>
	public sealed class ReportRowModel : ScopeModel<ReportRow>
	{
		/// <summary>
		/// 将所有者与部门维度分别映射到 <see cref="ReportRow.OwnerId"/> 与 <see cref="ReportRow.DeptId"/>。
		/// </summary>
		/// <param name="builder">范围模型构建器。</param>
		public override void Define(ScopeModelBuilder<ReportRow> builder)
		{
			builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
			       .Map(ScopeDimensions.Dept, x => x.DeptId);
		}

		/// <summary>
		/// 默认策略：要求 <see cref="ScopeDimensions.Dept"/> 维度的授予。
		/// </summary>
		public override ScopePolicy<ReportRow> Policy =>
			ScopePolicy<ReportRow>.Grant(ScopeDimensions.Dept);
	}

	/// <summary>
	/// 在 Osba 之后注册的模块，其范围模型也应进入注册表，同时不得挤掉 Osba 自身已注册的模型。
	/// </summary>
	[Fact]
	public void Other_Module_Registered_After_Osba_Should_Also_Be_Enforced()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });

		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var registry = services.BuildServiceProvider().GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(ReportRow)), "后注册模块的模型必须进入注册表");
		Assert.True(registry.IsDeclared(typeof(GrantRepo)), "Osba 自己的模型也不应被挤掉");
	}

	/// <summary>
	/// 后注册模块声明的行策略应真正参与判定：本部门的行放行，他部门的行拒绝。
	/// </summary>
	[Fact]
	public void Other_Module_Row_Policy_Should_Actually_Be_Enforced()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "tester")],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		services.AddSingleton(new UserPrincipal(new ClaimsPrincipal(identity)));
		services.AddSingleton<IScopeSubjectResolver>(new ReportAclResolver());

		var guard = services.BuildServiceProvider().Warm().GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new ReportRow { Id = "r1", DeptId = "team-a", OwnerId = "other" }));
		Assert.False(guard.Allows(new ReportRow { Id = "r2", DeptId = "team-b", OwnerId = "other" }));
	}

	/// <summary>
	/// Osba 注册的权限码来源应保留：更新操作仍映射到 <c>repo:push</c>。
	/// </summary>
	[Fact]
	public void Code_Source_Registered_By_Osba_Should_Survive_Other_Modules()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Contains(BusinessOperation.Update, source.AllOperations);
		Assert.Equal(["repo:push"], source.CodesFor(typeof(GrantRepo), BusinessOperation.Update));
	}

	/// <summary>
	/// 授予 <c>team-a</c> 部门维度的解析器，用于验证后注册模块声明的行策略按部门生效。
	/// </summary>
	private sealed class ReportAclResolver : IScopeSubjectResolver
	{
		/// <inheritdoc />
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
		{
			var builder = ScopeSubjectSet.CreateBuilder();
			builder.Add(ScopeDimensions.Dept, "team-a");

			return ValueTask.FromResult(builder.Build());
		}
	}
}
