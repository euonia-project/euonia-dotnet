using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 Euonia.Osba 权限控制功能：
/// 操作权限（PermissionRequirement 特性 + 权限检查器 + 工厂强制执行）与
/// 数据权限（ScopeTag + IDataScoped + IDataScopeService + IUserScopeProvider + DataScopeRule，
/// 授权值从数据实时解析、不固化）。
/// </summary>
public class PermissionTests
{
	#region 操作权限

	[Fact]
	public async Task SaveAsync_Insert_WithoutPermission_ShouldThrowSecurityException()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider);

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Insert_WithPermission_ShouldSucceed()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:create")), out var provider);

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();

		var result = await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Same(obj, result);
		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Update_WithoutPermission_ShouldThrowSecurityException()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:create")), out var provider);

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Update_WithWildcardPermission_ShouldSucceed()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:*")), out var provider);

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsChanged();

		var result = await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Delete_WithoutPermission_ShouldThrowSecurityException()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:update")), out var provider);

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsDeleted();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ExecuteAsync_WithoutPermission_ShouldThrowSecurityException()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var command = new SecuredCommand { BusinessContext = provider.GetRequiredService<BusinessContext>() };

		await Assert.ThrowsAsync<SecurityException>(() => factory.ExecuteAsync(command, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ExecuteAsync_WithPermission_ShouldSucceed()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "report:export")), out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var command = new SecuredCommand { BusinessContext = provider.GetRequiredService<BusinessContext>() };

		var result = await factory.ExecuteAsync(command, TestContext.Current.CancellationToken);

		Assert.True(result.Executed);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void ClassLevelRequirement_ShouldDenyAllOperations_WithoutPermission()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider);

		var obj = new AdminEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		Assert.False(obj.CanReadObject());
		Assert.False(obj.CanCreateObject());
		Assert.False(obj.CanUpdateObject());
		Assert.False(obj.CanDeleteObject());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void ClassLevelRequirement_ShouldAllowAllOperations_WithPermission()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "admin")), out var provider);

		var obj = new AdminEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		Assert.True(obj.CanReadObject());
		Assert.True(obj.CanCreateObject());
		Assert.True(obj.CanUpdateObject());
		Assert.True(obj.CanDeleteObject());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ClassLevelRequirement_Denied_ShouldThrowSecurityException()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider);

		var obj = new AdminEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsNew();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void HasPermission_HasRole_ShouldReflectClaims()
	{
		using var scope = CreatePermissionScope(UserWith(
			(UserClaimTypes.Permission, "order:create"),
			(UserClaimTypes.Role, "operator")), out var provider);

		var obj = new ProbeObject { BusinessContext = provider.GetRequiredService<BusinessContext>() };

		Assert.True(obj.ProbePermission("order:create"));
		Assert.False(obj.ProbePermission("order:delete"));
		Assert.True(obj.ProbeRole("operator"));
		Assert.False(obj.ProbeRole("admin"));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 数据权限

	// 数据权限的授权值必须从应用数据实时解析，不能固化在 Token/声明/代码里。
	// 以下测试用一个内存“授权数据”存储（MemoryScopeStore）模拟数据库中的成员/授权关系：
	// 增删改立即影响判定。

	[Fact]
	public void IsGranted_ShouldUseResolvedScopesFromData()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));
		store.Grant("dev", new ScopeTag("team", "TeamB"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.IsGranted(new ScopeTag("team", "TeamA")));
		Assert.True(service.IsGranted(new ScopeTag("team", "TeamB")));
		Assert.False(service.IsGranted(new ScopeTag("team", "TeamC")));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void IsGranted_OpaqueDatabaseIdValue_ShouldMatchExactString()
	{
		var teamId = Guid.NewGuid().ToString();
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", teamId));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.IsGranted(new ScopeTag("team", teamId)));
		Assert.False(service.IsGranted(new ScopeTag("team", Guid.NewGuid().ToString())));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void IsGranted_DimensionWildcard_ShouldMatchAnyValue()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("region", "*"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.IsGranted(new ScopeTag("region", Guid.NewGuid().ToString())));
		Assert.False(service.IsGranted(new ScopeTag("team", "anything")));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void IsGranted_GlobalWildcard_ShouldBypassAllDimensions()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", ScopeTag.Any);

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.IsGranted(new ScopeTag("any-dimension", "any-value")));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void DevScenario_ShouldAccessOnlyOwnTeamsRepos()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));
		store.Grant("dev", new ScopeTag("team", "TeamB"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		// Dev 属于 TeamA 和 TeamB：可访问两团队下的仓库
		Assert.True(service.CanAccess(new RepoRow { Name = "RepoA1", TeamId = "TeamA" }));
		Assert.True(service.CanAccess(new RepoRow { Name = "RepoA2", TeamId = "TeamA" }));
		Assert.True(service.CanAccess(new RepoRow { Name = "RepoB1", TeamId = "TeamB" }));
		Assert.True(service.CanAccess(new RepoRow { Name = "RepoB2", TeamId = "TeamB" }));

		// Dev 不在 TeamC：无法访问 TeamC 下面的仓库
		Assert.False(service.CanAccess(new RepoRow { Name = "RepoC1", TeamId = "TeamC" }));
		Assert.False(service.CanAccess(new RepoRow { Name = "RepoC2", TeamId = "TeamC" }));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void AuthorizationChange_ShouldTakeEffectImmediately()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		var repoC1 = new RepoRow { Name = "RepoC1", TeamId = "TeamC" };
		Assert.False(service.CanAccess(repoC1));

		// 授权数据变化：Dev 被加入 TeamC（既不改 Token、也不改代码）
		store.Grant("dev", new ScopeTag("team", "TeamC"));

		Assert.True(service.CanAccess(repoC1));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void ResourceScope_ShouldFollowItsOwnDataColumn()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamB"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		var repo = new RepoRow { Name = "RepoB2", TeamId = "TeamB" };
		Assert.True(service.CanAccess(repo));

		// 行的归属随自身数据列变化：TeamId 改为 TeamC，立即不再可访问
		repo.TeamId = "TeamC";
		Assert.False(service.CanAccess(repo));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_NullResource_ShouldDeny()
	{
		var store = new MemoryScopeStore();

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.False(service.CanAccess(null));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_OwnerFastPath_ShouldPass()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.CanAccess(new ScopedOrder { OwnerId = "dev" }));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_NoOwnerConstraint_ShouldPass()
	{
		var store = new MemoryScopeStore();

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.True(service.CanAccess(new ScopedOrder { OwnerId = null }));
		Assert.True(service.CanAccess(new ScopedOrder { OwnerId = "" }));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_NoScopeTags_ShouldDenyForOthers()
	{
		var store = new MemoryScopeStore();

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		Assert.False(service.CanAccess(new ScopedOrder { OwnerId = "other" }));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_AllDimensionsMustSatisfy_And()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("region", "east"));
		store.Grant("dev", new ScopeTag("team", "backend"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		var resource = new ScopedOrder { OwnerId = "u9" };
		resource.AddScope(new ScopeTag("region", "east"));
		resource.AddScope(new ScopeTag("team", "backend"));
		Assert.True(service.CanAccess(resource));

		var partial = new ScopedOrder { OwnerId = "u9" };
		partial.AddScope(new ScopeTag("region", "east"));
		partial.AddScope(new ScopeTag("team", "design"));
		Assert.False(service.CanAccess(partial));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_SameDimensionMultipleTags_ShouldUseOr()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "backend"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		// 资源同时并入 backend 与 platform 两个团队：用户属于任一团队即可访问
		var resource = new ScopedOrder { OwnerId = "u9" };
		resource.AddScope(new ScopeTag("team", "backend"));
		resource.AddScope(new ScopeTag("team", "platform"));
		Assert.True(service.CanAccess(resource));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Filter_ShouldReturnOnlyAccessibleItems()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("region", "east"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		var east = new ScopedOrder { OwnerId = "u9" };
		east.AddScope(new ScopeTag("region", "east"));

		var west = new ScopedOrder { OwnerId = "u9" };
		west.AddScope(new ScopeTag("region", "west"));

		var items = new[]
		{
			new ScopedOrder { OwnerId = "dev" },
			new ScopedOrder { OwnerId = "u9" },
			east,
			west
		};

		var filtered = service.Filter(items).ToArray();

		Assert.Equal(2, filtered.Length);
		Assert.Contains(filtered, item => item.OwnerId == "dev");
		Assert.Contains(filtered, item => ReferenceEquals(item, east));
		Assert.DoesNotContain(filtered, item => ReferenceEquals(item, west));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CreateScopePredicate_ShouldExcludeOutOfScopeRows()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("region", "east"));
		store.Grant("dev", new ScopeTag("team", "backend"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var service = provider.GetRequiredService<IDataScopeService>();

		var authorizer = service.CreateScopePredicate<ScopedOrder>();

		var accessible = new ScopedOrder { OwnerId = "u9" };
		accessible.AddScope(new ScopeTag("region", "east"));
		accessible.AddScope(new ScopeTag("team", "backend"));

		var denied = new ScopedOrder { OwnerId = "u9" };
		denied.AddScope(new ScopeTag("region", "east"));
		denied.AddScope(new ScopeTag("team", "design"));

		var rows = new[] { new ScopedOrder { OwnerId = "dev" }, accessible, denied };
		var visible = rows.Where(authorizer).ToArray();

		Assert.Equal(2, visible.Length);
		Assert.Contains(visible, item => item.OwnerId == "dev");
		Assert.Contains(visible, item => ReferenceEquals(item, accessible));
		Assert.DoesNotContain(visible, item => ReferenceEquals(item, denied));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_ShouldFailWhenOutOfScope()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));

		var denied = new ScopedOrder
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>(),
			OwnerId = "u9"
		};
		denied.AddScope(new ScopeTag("team", "TeamB"));

		_ = await denied.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.False(denied.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_ShouldPassWhenInScope()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));

		var allowed = new ScopedOrder
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>(),
			OwnerId = "u9"
		};
		allowed.AddScope(new ScopeTag("team", "TeamA"));

		_ = await allowed.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(allowed.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_ShouldPassForOwner()
	{
		var store = new MemoryScopeStore();

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));

		var owned = new ScopedOrder
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>(),
			OwnerId = "dev"
		};
		owned.AddScope(new ScopeTag("team", "TeamB"));

		_ = await owned.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(owned.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_ShouldPassWithoutDataScoped()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));

		var plain = new ProbeObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		plain.PublicRules.AddRule(new DataScopeRule());

		_ = await plain.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.True(plain.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_MissingScopeProvider_ShouldFailClosed()
	{
		// 未注册 IUserScopeProvider：数据权限形同虚设，必须暴露而不是静默放行。
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, null);

		var obj = new ScopedOrder
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>(),
			OwnerId = "u9"
		};
		obj.AddScope(new ScopeTag("team", "TeamC"));

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.False(obj.IsValid);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task DataScopeRule_WithoutBusinessContext_ShouldFailClosed()
	{
		// 未接入业务上下文时无从解析数据范围服务：规则必须失败，而不是静默放行。
		var obj = new ScopedOrder { OwnerId = "u9" };
		obj.AddScope(new ScopeTag("team", "TeamC"));
		obj.PublicRules.AddRule(new DataScopeRule());

		_ = await obj.PublicRules.CheckObjectRulesAsync(true, TestContext.Current.CancellationToken);

		Assert.False(obj.IsValid);
	}

	[Fact]
	public void Filter_ShouldResolveScopesOnceForWholeSequence()
	{
		// 谓词被查询层逐行调用；逐行解析授权数据会造成 N+1 查询。
		var provider = new CountingUserScopeProvider();
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var serviceProvider, provider);
		var dataScope = serviceProvider.GetRequiredService<IDataScopeService>();

		var rows = Enumerable.Range(0, 100).Select(_ => new RepoRow { TeamId = "TeamA" }).ToArray();

		Assert.Equal(100, dataScope.Filter(rows).Count());
		Assert.Equal(1, provider.CallCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CreateScopePredicate_ShouldResolveScopesOnceForWholeSequence()
	{
		var provider = new CountingUserScopeProvider();
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var serviceProvider, provider);
		var dataScope = serviceProvider.GetRequiredService<IDataScopeService>();

		var predicate = dataScope.CreateScopePredicate<RepoRow>();
		var rows = Enumerable.Range(0, 50).Select(_ => new RepoRow { TeamId = "TeamA" }).ToArray();

		Assert.Equal(50, rows.Count(predicate));
		Assert.Equal(1, provider.CallCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_AnonymousUser_ShouldBeDenied()
	{
		// 匿名用户没有任何授权值：默认拒绝，而不是放行全部数据。
		var anonymous = new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));
		Assert.False(anonymous.IsAuthenticated);

		using var scope = CreatePermissionScope(anonymous, out var provider, new CountingUserScopeProvider());
		var dataScope = provider.GetRequiredService<IDataScopeService>();

		Assert.False(dataScope.CanAccess(new RepoRow { TeamId = "TeamA" }));
		Assert.False(dataScope.CanAccess(new RepoRow()));
		Assert.False(dataScope.IsGranted(new ScopeTag("team", "TeamA")));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_AnonymousUser_OnAnonymousAccessibleData_ShouldBeAllowed()
	{
		// 注册、密码重置等场景：数据行显式声明可匿名访问。
		var anonymous = new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));

		using var scope = CreatePermissionScope(anonymous, out var provider, new CountingUserScopeProvider());
		var dataScope = provider.GetRequiredService<IDataScopeService>();

		Assert.True(dataScope.CanAccess(new RegistrationRow()));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void CanAccess_WithoutUserContext_ShouldBeAllowed()
	{
		// 未注册 UserPrincipal（后台任务、系统上下文）：数据权限无从判定，不做限制。
		using var scope = CreatePermissionScope(null, out var provider, new CountingUserScopeProvider());
		var dataScope = provider.GetRequiredService<IDataScopeService>();

		Assert.True(dataScope.CanAccess(new RepoRow { TeamId = "TeamC" }));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void IsGranted_NullTag_ShouldDeny()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new CountingUserScopeProvider());
		var dataScope = provider.GetRequiredService<IDataScopeService>();

		Assert.False(dataScope.IsGranted(null));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Dimension_ShouldBeComparedCaseInsensitively_ValueCaseSensitively()
	{
		var store = new MemoryScopeStore();
		store.Grant("dev", new ScopeTag("Team", "TeamA"));

		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new FakeUserScopeProvider(store));
		var dataScope = provider.GetRequiredService<IDataScopeService>();

		// 维度是代码约定的概念：大小写不敏感
		Assert.True(dataScope.IsGranted(new ScopeTag("team", "TeamA")));

		// 值是数据库标识：大小写敏感
		Assert.False(dataScope.IsGranted(new ScopeTag("team", "teama")));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 操作权限：工厂方法发现与权限要求收集一致

	[Fact]
	public async Task SaveAsync_ConventionNamedFactoryMethod_ShouldEnforcePermission()
	{
		// 工厂方法按命名约定（FactoryUpdateAsync）发现；其上的 [Permission] 必须同样生效，
		// 否则会出现"方法能被调用、权限却被忽略"的越权路径。
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new CountingUserScopeProvider());

		var obj = new ConventionSecuredObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_PlainConventionNamedMethod_ShouldEnforcePermission()
	{
		// 同样按命名约定发现，但用的是文档中的 UpdateAsync（不带 Factory 前缀）写法。
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider, new CountingUserScopeProvider());

		var obj = new PlainNamedSecuredObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ConventionNamedFactoryMethod_WithPermission_ShouldSucceed()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:update")), out var provider, new CountingUserScopeProvider());

		var obj = new ConventionSecuredObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsChanged();

		var result = await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(ObjectEditState.None, result.State);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ShouldInvokeActivator()
	{
		var activator = new RecordingObjectActivator();
		var services = new ServiceCollection();
		services.AddScoped<BusinessContextAccessor>();
		services.AddScoped<BusinessContext>();
		services.AddSingleton<IObjectActivator>(activator);
		services.AddScoped<IObjectFactory, BusinessObjectFactory>();
		services.AddScoped<IPermissionChecker, ClaimPermissionChecker>();
		services.AddScoped<IDataScopeService, DataScopeService>();
		services.AddSingleton<IUserScopeProvider>(new CountingUserScopeProvider());
		services.AddSingleton(UserWith((UserClaimTypes.Permission, "order:update")));

		var built = services.BuildServiceProvider();
		using var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = scope.ServiceProvider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsChanged();

		await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		// 与其它工厂入口一致：保存也应初始化/终结实例
		Assert.Equal(1, activator.InitializeCount);
		Assert.Equal(1, activator.FinalizeCount);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Helpers

	private static IServiceScope CreatePermissionScope(UserPrincipal user, out IServiceProvider provider, IUserScopeProvider scopeProvider = null)
	{
		var services = new ServiceCollection();
		services.AddScoped<BusinessContextAccessor>();
		services.AddScoped<BusinessContext>();
		services.AddScoped<IObjectFactory, BusinessObjectFactory>();
		services.AddScoped<IPermissionChecker, ClaimPermissionChecker>();
		services.AddScoped<IDataScopeService, DataScopeService>();
		if (scopeProvider != null)
		{
			services.AddSingleton(scopeProvider);
		}
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

	private static UserPrincipal UserWith(params (string Type, string Value)[] claims)
	{
		var identity = new ClaimsIdentity(
			claims.Select(c => new Claim(c.Type, c.Value)),
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);
		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 各操作方法带独立权限要求的可编辑业务对象。
/// </summary>
public class SecuredEditableObject : EditableObject<SecuredEditableObject>
{
	[FactoryInsert]
	[Permission("order:create")]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	[Permission("order:update")]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	[Permission("order:delete")]
	protected override async Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 类型级权限要求的可编辑业务对象，该要求适用于全部操作。
/// </summary>
[Permission("admin")]
public class AdminEditableObject : EditableObject<AdminEditableObject>
{
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
/// 按命名约定（而非工厂方法特性）声明更新方法的可编辑业务对象，用于验证
/// "工厂方法的发现方式"与"权限要求的收集方式"保持一致。
/// </summary>
public class ConventionSecuredObject : EditableObject<ConventionSecuredObject>
{
	/// <summary>
	/// 按命名约定被识别为更新工厂方法，方法级权限要求必须同样生效。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[Permission("order:update")]
	protected internal async Task FactoryUpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 按命名约定（文档中的 <c>UpdateAsync</c> 写法，不带 Factory 前缀）声明更新方法的可编辑业务对象。
/// </summary>
public class PlainNamedSecuredObject : EditableObject<PlainNamedSecuredObject>
{
	/// <summary>
	/// 按命名约定被识别为更新工厂方法，方法级权限要求必须同样生效。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	[Permission("order:update")]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>
/// 带权限要求的命令对象。
/// </summary>
public class SecuredCommand : CommandObject<SecuredCommand>
{
	/// <summary>
	/// 指示命令是否已执行。
	/// </summary>
	public bool Executed { get; private set; }

	[FactoryExecute]
	[Permission("report:export")]
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = true;
		await Task.CompletedTask;
	}
}

/// <summary>
/// 实现 <see cref="IDataScoped"/> 并注册 <see cref="DataScopeRule"/> 的测试业务对象。
/// </summary>
public class ScopedOrder : ObservableObject<ScopedOrder>, IDataScoped
{
	private readonly List<ScopeTag> _scopeTags = [];

	/// <summary>
	/// 公开规则集合以便测试调用。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <inheritdoc />
	public string OwnerId { get; set; }

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ScopeTags => _scopeTags;

	/// <summary>
	/// 添加一个范围标签。
	/// </summary>
	/// <param name="tag">要添加的标签。</param>
	public void AddScope(ScopeTag tag)
	{
		_scopeTags.Add(tag);
	}

	/// <inheritdoc />
	protected override void AddRules()
	{
		Rules.AddRule(new DataScopeRule());
	}
}

/// <summary>
/// 用于访问 <see cref="BusinessObject"/> 受保护的权限辅助方法的测试对象。
/// </summary>
public class ProbeObject : ObservableObject<ProbeObject>
{
	/// <summary>
	/// 公开规则集合以便测试调用。
	/// </summary>
	public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;

	/// <summary>
	/// 调用 <see cref="BusinessObject.HasPermission(string)"/>。
	/// </summary>
	public bool ProbePermission(string permission) => HasPermission(permission);

	/// <summary>
	/// 调用 <see cref="BusinessObject.HasRole(string)"/>。
	/// </summary>
	public bool ProbeRole(string role) => HasRole(role);
}

/// <summary>
/// 测试仓库行：范围值直接来自自身数据列（<see cref="TeamId"/>），非固化标签。
/// </summary>
public class RepoRow : IDataScoped
{
	/// <summary>
	/// 仓库名称（仅作展示）。
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// 仓库所属团队，作为仓库维度范围的数据来源。
	/// </summary>
	public string TeamId { get; set; }

	/// <inheritdoc />
	public string OwnerId { get; set; }

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ScopeTags
		=> string.IsNullOrWhiteSpace(TeamId) ? Array.Empty<ScopeTag>() : [new ScopeTag("team", TeamId)];
}

/// <summary>
/// 模拟授权关系数据（成员关系表）：每次解析实时读取，修改立即生效。
/// </summary>
public class MemoryScopeStore
{
	private readonly Dictionary<string, List<ScopeTag>> _scopes = new();

	/// <summary>
	/// 授予指定用户一个范围标签。
	/// </summary>
	/// <param name="userId">用户标识。</param>
	/// <param name="tag">范围标签。</param>
	public void Grant(string userId, ScopeTag tag)
	{
		if (!_scopes.TryGetValue(userId, out var list))
		{
			list = [];
			_scopes[userId] = list;
		}

		if (!list.Contains(tag))
		{
			list.Add(tag);
		}
	}

	/// <summary>
	/// 解析指定用户当前被授予的范围标签。
	/// </summary>
	/// <param name="userId">用户标识。</param>
	/// <returns>范围标签序列；无授权时返回空序列。</returns>
	public IReadOnlyList<ScopeTag> Resolve(string userId)
	{
		return _scopes.TryGetValue(userId, out var list) ? list : Array.Empty<ScopeTag>();
	}
}

/// <summary>
/// 允许匿名访问的数据行（注册、密码重置等场景）。
/// </summary>
public class RegistrationRow : IDataScoped, IAnonymousAccessible
{
	/// <inheritdoc />
	public string OwnerId => null;

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ScopeTags => Array.Empty<ScopeTag>();
}

/// <summary>
/// 记录调用次数的范围提供者，用于验证一次过滤只解析一次授权数据。
/// </summary>
public class CountingUserScopeProvider : IUserScopeProvider
{
	/// <summary>
	/// 获取 <see cref="ResolveScopes"/> 被调用的次数。
	/// </summary>
	public int CallCount { get; private set; }

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user)
	{
		CallCount++;
		return [new ScopeTag("team", "TeamA")];
	}
}

/// <summary>
/// 记录初始化/终结调用次数的对象激活器。
/// </summary>
public class RecordingObjectActivator : IObjectActivator
{
	/// <summary>
	/// 获取 <see cref="InitializeInstance"/> 被调用的次数。
	/// </summary>
	public int InitializeCount { get; private set; }

	/// <summary>
	/// 获取 <see cref="FinalizeInstance"/> 被调用的次数。
	/// </summary>
	public int FinalizeCount { get; private set; }

	/// <inheritdoc />
	public void InitializeInstance(object obj) => InitializeCount++;

	/// <inheritdoc />
	public void FinalizeInstance(object obj) => FinalizeCount++;
}

/// <summary>
/// 从 <see cref="MemoryScopeStore"/> 解析用户范围的测试提供者。
/// </summary>
public class FakeUserScopeProvider : IUserScopeProvider
{
	private readonly MemoryScopeStore _store;

	/// <summary>
	/// 初始化 <see cref="FakeUserScopeProvider"/> 的新实例。
	/// </summary>
	/// <param name="store">授权数据存储。</param>
	public FakeUserScopeProvider(MemoryScopeStore store)
	{
		_store = store;
	}

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user)
	{
		if (user == null || !user.IsAuthenticated)
		{
			return Array.Empty<ScopeTag>();
		}

		return _store.Resolve(user.UserId);
	}
}