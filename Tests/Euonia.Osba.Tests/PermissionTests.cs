using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证 Euonia.Osba 的<b>操作权限</b>：
/// PermissionAttribute 声明 + 权限检查器 + BusinessObjectFactory 边界强制执行，
/// 并覆盖「权限要求的收集范围与工厂方法的发现范围一致」这一约束。
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


	#region 操作权限：工厂方法发现与权限要求收集一致

	[Fact]
	public async Task SaveAsync_ConventionNamedFactoryMethod_ShouldEnforcePermission()
	{
		// 工厂方法按命名约定（FactoryUpdateAsync）发现；其上的 [Permission] 必须同样生效，
		// 否则会出现"方法能被调用、权限却被忽略"的越权路径。
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider);

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
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Subject, "dev")), out var provider);

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
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Permission, "order:update")), out var provider);

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

	private static IServiceScope CreatePermissionScope(UserPrincipal user, out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		services.AddScoped<BusinessContextAccessor>();
		services.AddScoped<BusinessContext>();
		services.AddScoped<IObjectFactory, BusinessObjectFactory>();
		services.AddScoped<IPermissionChecker, ClaimPermissionChecker>();
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
