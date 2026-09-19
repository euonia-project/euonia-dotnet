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