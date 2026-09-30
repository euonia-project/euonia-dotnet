using System.Linq.Expressions;
using System.Security;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

public class ScopeRowPermissionTests
{
	#region 行级操作权限

	[Fact]
	public void RowLevel_SameUserSameType_DifferentRowsDifferentRights()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(Repo("A1"), "repo:push"));
		Assert.True(guard.Allows(Repo("A2"), "repo:push"));
		Assert.False(guard.Allows(Repo("A3"), "repo:push"));

		Assert.True(guard.Allows(Repo("A1"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A3"), "repo:delete"));

		Assert.True(guard.Allows(Repo("A2"), "repo:push"));
		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void RowLevel_CodeGrantShouldOverrideDefault_NotUnion()
	{
		var resolver = new AclResolver();
		resolver.GrantDefault("repo", "A1", "A2", "A3");
		resolver.GrantCode("repo:delete", "repo", "A1");

		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();

		Assert.False(guard.Allows(Repo("A2"), "repo:delete"));
		Assert.False(guard.Allows(Repo("A3"), "repo:delete"));
		Assert.True(guard.Allows(Repo("A1"), "repo:delete"));

		Assert.False(guard.Allows(Repo("A3"), "repo:push"));
		Assert.True(guard.Allows(Repo("A2"), "repo:push"));

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

		var call = Assert.IsAssignableFrom<MethodCallExpression>(compiled.Allow.Body);
		Assert.Equal(nameof(Enumerable.Contains), call.Method.Name);
		Assert.Contains("x.RepoId", compiled.Allow.ToString());

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

	[Fact]
	public void RevokedPermission_ShouldTakeEffectWithoutReissuingToken()
	{
		var resolver = new AclResolver();

		using var scope = CreateScope(resolver, out var provider);
		var guard = provider.GetRequiredService<IScopeGuard>();
		var target = Repo("A2");

		Assert.True(guard.Allows(target, "repo:push"));

		resolver.RevokeCode("repo:push");

		Assert.True(guard.Allows(target, "repo:push"));

		guard.Refresh();
		Assert.False(guard.Allows(target, "repo:push"));

		using var next = CreateScope(resolver, out var nextProvider);
		Assert.False(nextProvider.GetRequiredService<IScopeGuard>().Allows(target, "repo:push"));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Permissions_ShouldComeFromResolver_NotClaims()
	{
		using var scope = CreateScope(new AclResolver(), out var provider);

		var user = provider.GetRequiredService<UserPrincipal>();

		Assert.Empty(user.FindClaims(UserClaimTypes.Permission));

		Assert.Contains("repo:push", provider.GetRequiredService<IScopeGuard>().Permissions);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 权限线与验证线彼此独立

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
	public void GuardResolution_ShouldFailWhenResolverMissing()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ScopeRowPermissionTests).Assembly);

		var provider = services.BuildServiceProvider();

		var exception = Assert.Throws<InvalidOperationException>(provider.GetRequiredService<IScopeGuard>);

		Assert.Contains(nameof(IScopeSubjectResolver), exception.Message);
	}

	[Fact]
	public void GuardResolution_ShouldPassWhenResolverRegistered()
	{
		var services = new ServiceCollection();
		services.AddBusinessObject(typeof(ScopeRowPermissionTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ScopeRowPermissionTests).Assembly);
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

	[Fact]
	public void PolicySet_ShouldRejectReservedPermissionCode()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		var exception = Assert.Throws<InvalidOperationException>(
			() => policies.For("@custom", ScopePolicy<GrantRepo>.Where(_ => true)));

		Assert.Contains(ScopeKeys.Prefix, exception.Message);
	}

	[Fact]
	public void PolicySet_ShouldAllowFrameworkDefaultKeys()
	{
		var policies = new ScopePolicySet<GrantRepo>();

		policies.ForOperation(BusinessOperation.Create, ScopePolicy<GrantRepo>.Where(_ => true));

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
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(ScopeRowPermissionTests).Assembly);
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

public class AclResolver : IScopeSubjectResolver
{
	private readonly HashSet<string> _codes = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _codeGrants = new(StringComparer.Ordinal);
	private readonly Dictionary<string, HashSet<string>> _defaultGrants = new(StringComparer.OrdinalIgnoreCase);

	public AclResolver()
	{
		_codes.Add("repo:push");
		_codes.Add("repo:delete");

		GrantCode("repo:push", "repo", "A1", "A2");
		GrantCode("repo:delete", "repo", "A1");
	}

	public void GrantCode(string code)
	{
		_codes.Add(code);
	}

	public void RevokeCode(string code)
	{
		_codes.Remove(code);

		foreach (var key in _codeGrants.Keys.Where(key => key.StartsWith($"{code}|", StringComparison.Ordinal)).ToArray())
		{
			_codeGrants.Remove(key);
		}
	}

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

public class GrantRepo : EditableObject<GrantRepo>
{
	public string RepoId { get; set; }

	[FactoryInsert]
	protected internal override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	[Permission("repo:push")]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	[Permission("repo:delete")]
	protected internal override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

public sealed class GrantRepoModel : ScopeModel<GrantRepo>
{
	public override void Define(ScopeModelBuilder<GrantRepo> builder)
	{
		builder.Map("repo", x => x.RepoId);
	}

	public override ScopePolicy<GrantRepo> Policy => ScopePolicy<GrantRepo>.Grant("repo");

	public override void Declare(ScopePolicySet<GrantRepo> policies)
	{
		policies.ForOperation(BusinessOperation.Read, ScopePolicy<GrantRepo>.Grant("repo"));

		policies.For("repo:push", ScopePolicy<GrantRepo>.Grant("repo"));
		policies.For("repo:delete", ScopePolicy<GrantRepo>.Grant("repo"));
	}
}

public class GrantRepoProbe : GrantRepo
{
	public bool ProbeRowAccess(string scopeKey) => CanAccessRow(scopeKey);
}

public class MultiModulePermissionTests
{
	public sealed class ReportRow
	{
		public string Id { get; set; }

		public string OwnerId { get; set; }

		public string DeptId { get; set; }
	}

	public sealed class ReportRowModel : ScopeModel<ReportRow>
	{
		public override void Define(ScopeModelBuilder<ReportRow> builder)
		{
			builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
			       .Map(ScopeDimensions.Dept, x => x.DeptId);
		}

		public override ScopePolicy<ReportRow> Policy =>
			ScopePolicy<ReportRow>.Grant(ScopeDimensions.Dept);
	}

	[Fact]
	public void Other_Module_Registered_After_Osba_Should_Also_Be_Enforced()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(MultiModulePermissionTests).Assembly);

		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var registry = services.BuildServiceProvider().GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.IsDeclared(typeof(ReportRow)), "后注册模块的模型必须进入注册表");
		Assert.True(registry.IsDeclared(typeof(GrantRepo)), "Osba 自己的模型也不应被挤掉");
	}

	[Fact]
	public void Other_Module_Row_Policy_Should_Actually_Be_Enforced()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "tester")],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		services.AddSingleton(new UserPrincipal(new ClaimsPrincipal(identity)));
		services.AddSingleton<IScopeSubjectResolver>(new ReportAclResolver());

		var guard = services.BuildServiceProvider().GetRequiredService<IScopeGuard>();

		Assert.True(guard.Allows(new ReportRow { Id = "r1", DeptId = "team-a", OwnerId = "other" }));
		Assert.False(guard.Allows(new ReportRow { Id = "r2", DeptId = "team-b", OwnerId = "other" }));
	}

	[Fact]
	public void Code_Source_Registered_By_Osba_Should_Survive_Other_Modules()
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(MultiModulePermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(MultiModulePermissionTests).Assembly); p.NoOperationCodes(); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Contains(BusinessOperation.Update, source.AllOperations);
		Assert.Equal(["repo:push"], source.CodesFor(typeof(GrantRepo), BusinessOperation.Update));
	}

	private sealed class ReportAclResolver : IScopeSubjectResolver
	{
		public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
		{
			var builder = ScopeSubjectSet.CreateBuilder();
			builder.Add(ScopeDimensions.Dept, "team-a");

			return ValueTask.FromResult(builder.Build());
		}
	}
}
