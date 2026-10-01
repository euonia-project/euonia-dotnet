using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

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
		using var scope = CreatePermissionScope(UserWith(), out var provider, "order:create");

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
		using var scope = CreatePermissionScope(UserWith(), out var provider, "order:create");

		var obj = new SecuredEditableObject();
		obj.BusinessContext = provider.GetRequiredService<BusinessContext>();
		obj.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Update_WithWildcardPermission_ShouldSucceed()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider, "order:*");

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
		using var scope = CreatePermissionScope(UserWith(), out var provider, "order:update");

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
		using var scope = CreatePermissionScope(UserWith(), out var provider, "report:export");
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

		Assert.False(obj.CanPerformOperation(BusinessOperation.Read));
		Assert.False(obj.CanPerformOperation(BusinessOperation.Create));
		Assert.False(obj.CanPerformOperation(BusinessOperation.Update));
		Assert.False(obj.CanPerformOperation(BusinessOperation.Delete));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void ClassLevelRequirement_ShouldAllowAllOperations_WithPermission()
	{
		using var scope = CreatePermissionScope(UserWith(), out var provider, "admin");

		var obj = new AdminEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		Assert.True(obj.CanPerformOperation(BusinessOperation.Read));
		Assert.True(obj.CanPerformOperation(BusinessOperation.Create));
		Assert.True(obj.CanPerformOperation(BusinessOperation.Update));
		Assert.True(obj.CanPerformOperation(BusinessOperation.Delete));

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
	public void Permission_Checker_Should_Reflect_Grants_And_Roles()
	{
		using var scope = CreatePermissionScope(UserWith((UserClaimTypes.Role, "operator")), out var provider, "order:create");

		var checker = provider.GetRequiredService<IPermissionChecker>();

		Assert.True(checker.IsGranted("order:create"));
		Assert.False(checker.IsGranted("order:delete"));
		Assert.True(checker.IsInRole("operator"));
		Assert.False(checker.IsInRole("admin"));

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
		using var scope = CreatePermissionScope(UserWith(), out var provider, "order:update");

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
		services.AddBusinessObject(typeof(PermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(PermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddSingleton<IObjectActivator>(activator);
		services.AddSingleton<IScopeSubjectResolver>(new TestSubjectResolver("order:update"));
		services.AddSingleton(UserWith());

		var built = services.BuildServiceProvider();
		using var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		scope.ServiceProvider.Warm();

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

	#region 无法判定时必须失败，不能静默放行

	[Fact]
	public async Task SaveAsync_WithRequirementsButNoBusinessContext_ShouldFailInsteadOfBypassing()
	{
		// 目标声明了权限要求，却没接入 BusinessContext —— 此时解析不到权限检查器。
		// 这是配置错误（多半是调用方 new 出对象后忘了接线），必须暴露而不是静默放行。
		using var scope = CreatePermissionScope(UserWith(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var obj = new SecuredEditableObject();      // 刻意不设 BusinessContext
		obj.MarkAsChanged();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => factory.SaveAsync(obj, TestContext.Current.CancellationToken));

		Assert.Contains("BusinessContext", exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_WithoutRequirements_ShouldNotForceWiring()
	{
		// 反向护栏：没有任何权限要求的类型不受影响，不强制要求接线
		using var scope = CreatePermissionScope(UserWith(), out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var obj = new UnsecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsChanged();

		// 不抛异常即为通过（状态复位由 EditableObject.SaveAsync 负责，工厂不做）
		var result = await factory.SaveAsync(obj, TestContext.Current.CancellationToken);

		Assert.Same(obj, result);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region Helpers

	private static IServiceScope CreatePermissionScope(UserPrincipal user, out IServiceProvider provider, params string[] permissions)
	{
		var services = new ServiceCollection();

		// 走真实的注册路径：权限检查器、数据权限守卫与模型注册表都由 AddBusinessObject 装配
		services.AddBusinessObject(typeof(PermissionTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(PermissionTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });

		// 权限码来自授权数据（解析器），而不是令牌声明
		services.AddSingleton<IScopeSubjectResolver>(new TestSubjectResolver(permissions));
		if (user != null)
		{
			services.AddSingleton(user);
		}

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider.Warm();
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
	protected internal override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	[Permission("order:update")]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	[Permission("order:delete")]
	protected internal override async Task DeleteAsync(CancellationToken cancellationToken = default)
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
	protected internal override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryUpdate]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryDelete]
	protected internal override async Task DeleteAsync(CancellationToken cancellationToken = default)
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
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
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
	protected internal override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Executed = true;
		await Task.CompletedTask;
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
/// 测试用授权数据解析器：权限码由测试直接给定，不经过令牌声明。
/// </summary>
public class TestSubjectResolver : IScopeSubjectResolver
{
	private readonly string[] _permissions;

	/// <summary>
	/// 初始化 <see cref="TestSubjectResolver"/> 的新实例。
	/// </summary>
	/// <param name="permissions">用户持有的权限码。</param>
	public TestSubjectResolver(params string[] permissions)
	{
		_permissions = permissions ?? [];
	}

	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder().AddCodes(_permissions).Build());
	}
}

/// <summary>
/// 不带任何权限要求的可编辑业务对象，用于验证「无要求时不强制接线」。
/// </summary>
public class UnsecuredEditableObject : EditableObject<UnsecuredEditableObject>
{
	[FactoryUpdate]
	protected internal override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}
