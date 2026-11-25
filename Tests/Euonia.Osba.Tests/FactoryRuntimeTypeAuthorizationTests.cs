using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// B1 回归：工厂按静态类型查找工厂方法、反射调用却按运行时类型虚分派，
/// 权限要求必须按目标的<b>运行时类型</b>收集，并在运行时类型上解析实际执行的方法
/// （派生 override / <c>new</c> 遮蔽），取「静态方法声明 ∪ 实际实现声明」的并集。
/// </summary>
/// <remarks>
/// 修复前：类型级要求按 typeof(TTarget) 收集、方法级要求只读静态方法的基类链，
/// DI 把基类映射到派生类（或调用方传入派生实例）时，派生类上的 [Permission] 被静默放行（fail-open）。
/// 本类用例在修复前全部表现为「无权限也能执行」，修复后无权限必须抛 <see cref="SecurityException"/>。
/// </remarks>
public class FactoryRuntimeTypeAuthorizationTests
{
	/// <summary>
	/// DI 把基类映射到派生类时，派生 override 上的方法级权限必须生效：无权限的读取被拒绝。
	/// </summary>
	[Fact]
	public async Task FetchAsync_DerivedOverride_WithoutPermission_ShouldThrow()
	{
		using var scope = CreateScope(out var provider, [], services => services.AddTransient<RuntimeFetchBase, RuntimeFetchDerived>());
		var factory = provider.GetRequiredService<IObjectFactory>();

		await Assert.ThrowsAsync<SecurityException>(() => factory.FetchAsync<RuntimeFetchBase>("id"));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 持有派生 override 声明的权限码时，DI 映射到派生类的读取正常完成。
	/// </summary>
	[Fact]
	public async Task FetchAsync_DerivedOverride_WithPermission_ShouldSucceed()
	{
		using var scope = CreateScope(out var provider, ["runtime:read"], services => services.AddTransient<RuntimeFetchBase, RuntimeFetchDerived>());
		var factory = provider.GetRequiredService<IObjectFactory>();

		var result = await factory.FetchAsync<RuntimeFetchBase>("id");

		Assert.IsType<RuntimeFetchDerived>(result);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 保存路径（SaveAsync）：按基类静态类型查找到的更新方法在派生实例上虚分派到 override，
	/// override 上的权限必须生效，无权限时拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_DerivedOverride_WithoutPermission_ShouldThrow()
	{
		using var scope = CreateScope(out var provider, []);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeUpdateBase target = new RuntimeUpdateDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => factory.SaveAsync(target, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 持有派生 override 声明的权限码时，保存正常完成。
	/// </summary>
	[Fact]
	public async Task SaveAsync_DerivedOverride_WithPermission_ShouldSucceed()
	{
		using var scope = CreateScope(out var provider, ["runtime:update"]);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeUpdateBase target = new RuntimeUpdateDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		var result = await factory.SaveAsync(target, TestContext.Current.CancellationToken);

		Assert.Same(target, result);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 派生类用 <c>new</c> 遮蔽基类工厂方法并声明权限时，要求并集必须覆盖遮蔽方法上的声明，无权限时拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_NewShadowedMethod_WithoutPermission_ShouldThrow()
	{
		using var scope = CreateScope(out var provider, []);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeShadowBase target = new RuntimeShadowDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => factory.SaveAsync(target, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 持有遮蔽方法声明的权限码时，保存正常完成。
	/// </summary>
	[Fact]
	public async Task SaveAsync_NewShadowedMethod_WithPermission_ShouldSucceed()
	{
		using var scope = CreateScope(out var provider, ["runtime:shadow"]);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeShadowBase target = new RuntimeShadowDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		var result = await factory.SaveAsync(target, TestContext.Current.CancellationToken);

		Assert.Same(target, result);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 类型级 [Permission] 声明在派生类上时同样生效：按基类静态类型保存派生实例，无权限必须拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_DerivedTypeLevelRequirement_WithoutPermission_ShouldThrow()
	{
		using var scope = CreateScope(out var provider, []);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeTypeLevelBase target = new RuntimeTypeLevelDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		await Assert.ThrowsAsync<SecurityException>(() => factory.SaveAsync(target, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 持有派生类类型级权限码时，保存正常完成。
	/// </summary>
	[Fact]
	public async Task SaveAsync_DerivedTypeLevelRequirement_WithPermission_ShouldSucceed()
	{
		using var scope = CreateScope(out var provider, ["runtime:admin"]);
		var factory = provider.GetRequiredService<IObjectFactory>();

		RuntimeTypeLevelBase target = new RuntimeTypeLevelDerived
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		target.MarkAsChanged();

		var result = await factory.SaveAsync(target, TestContext.Current.CancellationToken);

		Assert.Same(target, result);

		BusinessContextAccessor.Clear();
	}

	#region Helpers

	/// <summary>
	/// 构建权限判定作用域：只走内建声明路径（不登记 ObjectPermissionCodeSource），
	/// 确保用例钉住的是 PermissionRequirements 的收集口径本身。
	/// </summary>
	/// <param name="provider">作用域的服务提供程序（已预热）。</param>
	/// <param name="permissions">当前用户持有的权限码。</param>
	/// <param name="configure">额外的服务注册（如基类到派生类的 DI 映射）。</param>
	/// <returns>服务作用域。</returns>
	private static IServiceScope CreateScope(out IServiceProvider provider, string[] permissions, Action<IServiceCollection>? configure = null)
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(FactoryRuntimeTypeAuthorizationTests).Assembly);
		services.AddPermission(p => p.Scan(typeof(FactoryRuntimeTypeAuthorizationTests).Assembly));
		services.AddSingleton<IScopeSubjectResolver>(new TestSubjectResolver(permissions));
		services.AddSingleton(UserWith());

		configure?.Invoke(services);

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider.Warm();
		return scope;
	}

	private static UserPrincipal UserWith()
	{
		var identity = new ClaimsIdentity([], "Bearer", ClaimTypes.Name, UserClaimTypes.Role);
		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 读取工厂基类：工厂方法本身不带权限声明。
/// </summary>
public class RuntimeFetchBase : EditableObject<RuntimeFetchBase>
{
	/// <summary>
	/// 装载工厂方法：按 <see cref="FactoryFetchAttribute"/> 发现，本身不带权限声明。
	/// </summary>
	/// <param name="id">对象标识。</param>
	/// <returns>表示异步装载的任务。</returns>
	[FactoryFetch]
	protected internal virtual Task LoadAsync(string id)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 读取工厂派生类：在 override 上追加权限声明；DI 把基类映射到本类型时该声明必须生效。
/// </summary>
public class RuntimeFetchDerived : RuntimeFetchBase
{
	/// <summary>
	/// 装载工厂方法的 override，要求 <c>runtime:read</c> 权限。
	/// </summary>
	/// <param name="id">对象标识。</param>
	/// <returns>表示异步装载的任务。</returns>
	[Permission("runtime:read")]
	protected internal override Task LoadAsync(string id)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 更新工厂基类：更新方法本身不带权限声明。
/// </summary>
public class RuntimeUpdateBase : EditableObject<RuntimeUpdateBase>
{
	/// <summary>
	/// 更新工厂方法：本身不带权限声明。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步更新的任务。</returns>
	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 更新工厂派生类：在 override 上追加权限声明。
/// </summary>
public class RuntimeUpdateDerived : RuntimeUpdateBase
{
	/// <summary>
	/// 更新工厂方法的 override，要求 <c>runtime:update</c> 权限。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步更新的任务。</returns>
	[Permission("runtime:update")]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 遮蔽场景基类：更新方法本身不带权限声明。
/// </summary>
public class RuntimeShadowBase : EditableObject<RuntimeShadowBase>
{
	/// <summary>
	/// 更新工厂方法：本身不带权限声明。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步更新的任务。</returns>
	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 遮蔽场景派生类：用 <c>new</c> 遮蔽基类更新方法并追加权限声明。
/// </summary>
public class RuntimeShadowDerived : RuntimeShadowBase
{
	/// <summary>
	/// 以 <c>new</c> 遮蔽的更新方法，要求 <c>runtime:shadow</c> 权限。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步更新的任务。</returns>
	[Permission("runtime:shadow")]
	protected internal new Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 类型级声明场景基类：不带任何权限声明。
/// </summary>
public class RuntimeTypeLevelBase : EditableObject<RuntimeTypeLevelBase>
{
	/// <summary>
	/// 更新工厂方法：本身不带权限声明。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步更新的任务。</returns>
	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 类型级声明场景派生类：类型级权限声明必须对按基类静态类型发起的操作生效。
/// </summary>
[Permission("runtime:admin")]
public class RuntimeTypeLevelDerived : RuntimeTypeLevelBase
{
}
