using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 运行期判定与注册期校验必须用**同一个**来源。
/// <para>
/// 宿主通过 <c>AddPermission</c> 补充的规则（回调 / 配置节 / 自定义来源）如果只在注册期生效，
/// 工厂边界就会比配置写的更宽松，而启动期不会报错——那正是本库最不能接受的一类失败。
/// 本文件用「只认识权限码的自定义来源」把这条边界钉死在工厂边界上。
/// </para>
/// </summary>
public class RuntimeRequirementSourceTests
{
	/// <summary>
	/// 宿主通过自定义来源补充 <c>host:update</c> 要求后，未授予该权限码的更新必须被工厂边界以
	/// <see cref="SecurityException"/> 拒绝，而不是按 Osba 自身的约定静默通过。
	/// </summary>
	[Fact]
	public async Task Factory_Boundary_Should_Reject_When_Host_Declared_Requirement_Is_Missing()
	{
		using var scope = CreateScope(codes: [], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		// 宿主规则（host:update）由主机补充、Osba 自己的约定完全不知道它：
		// 若运行期只用 Osba 的来源，这里会静默通过
		await Assert.ThrowsAsync<SecurityException>(
			() => factory.UpdateAsync<ScopedTask>("team-a", TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 目标对象没有 <c>BusinessContext</c> 时仍应使用环境来源发现宿主要求；由于无法判定，
	/// 必须抛出 <see cref="InvalidOperationException"/> 而不是静默放行。
	/// </summary>
	[Fact]
	public async Task Factory_Boundary_Should_Use_Ambient_Source_When_Target_Has_No_BusinessContext()
	{
		using var scope = CreateScope(codes: [], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		// 手工构造的对象的 BusinessContext 为 null；宿主注册的来源仍必须生效，
		// 否则工厂边界会静默放行本应被拒绝的更新。
		var target = new AmbientTask();
		target.MarkAsChanged();

		// 宿主来源能查到要求后，缺少 BusinessContext 属于配置错误，必须 fail-closed 而非放行。
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => factory.SaveAsync(target, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 派生类型用 <c>new</c> 遮蔽基类更新方法时，保存仍按基类方法上的权限要求判定；
	/// 未授予相应权限时抛出 <see cref="SecurityException"/>。
	/// </summary>
	[Fact]
	public async Task SaveAsync_OnDerivedType_ShouldEnforceHiddenBaseMethodRequirement()
	{
		using var scope = CreateScope(codes: [], out var provider);

		// DerivedScopedEntity 用 new 遮蔽了 BaseScopedEntity 的 UpdateAsync；
		// 继承自 EditableObject<BaseScopedEntity> 的 SaveAsync 会以基类为静态类型调用工厂，
		// 因此必须按基类方法上的 [Permission] 判定，而不能按运行时派生类型放行。
		var target = new DerivedScopedEntity
		{
			BusinessContext = new BusinessContext(provider.GetRequiredService<BusinessContextAccessor>())
		};
		target.MarkAsChanged();

		var factory = provider.GetRequiredService<IObjectFactory>();
		await Assert.ThrowsAsync<SecurityException>(
			() => factory.SaveAsync<BaseScopedEntity>(target, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 授予宿主声明的 <c>host:update</c> 后，更新应通过工厂边界，并返回团队标识为 <c>team-a</c> 的对象。
	/// </summary>
	[Fact]
	public async Task Factory_Boundary_Should_Allow_When_Host_Declared_Requirement_Is_Granted()
	{
		using var scope = CreateScope(codes: ["host:update"], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();

		var updated = await factory.UpdateAsync<ScopedTask>("team-a", TestContext.Current.CancellationToken);

		Assert.Equal("team-a", updated.TeamId);

		BusinessContextAccessor.Clear();
	}

	#region 装配

	private static IServiceScope CreateScope(string[] codes, out IServiceProvider provider)
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(ScopedTask).Assembly);
		services.AddPermission(p => { p.Scan(typeof(ScopedTask).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });

		// 宿主补充的规则：与 Osba 自己的口径并存，注册期与运行期都必须看到它
		services.AddPermission(p => { p.Scan(typeof(ScopedTask).Assembly); p.Source(new HostUpdateRequirementSource()); });

		services.AddSingleton<IScopeSubjectResolver>(new HostRuleResolver(codes));
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();

		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;

		return scope;
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[new Claim(UserClaimTypes.Subject, userId)],
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 用于验证「目标没有 BusinessContext 时仍使用环境来源」的简单可编辑对象。
/// </summary>
public class AmbientTask : EditableObject<AmbientTask>
{
	/// <summary>
	/// 更新工厂方法：本测试对象不落库，直接完成。
	/// </summary>
	[FactoryUpdate]
	protected new Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// 宿主补充的要求来源：只给权限码（要求由 <see cref="IPermissionCodeSource"/> 的默认实现折算），
/// 对 <see cref="ScopedTask"/> / <see cref="AmbientTask"/> 的更新操作额外要求 <c>host:update</c>。
/// </summary>
internal sealed class HostUpdateRequirementSource : IPermissionCodeSource
{
	private const string Code = "host:update";

	/// <summary>
	/// 获取该来源覆盖的业务操作集合，这里仅包含更新操作。
	/// </summary>
	public IReadOnlyList<string> AllOperations => [BusinessOperation.Update];

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return (type == typeof(ScopedTask) || type == typeof(AmbientTask)) && operation == BusinessOperation.Update ? [Code] : [];
	}
}

/// <summary>授予指定权限码与部门范围的解析器（数据范围满足 <see cref="ScopedTaskModel"/> 的部门策略）。</summary>
internal sealed class HostRuleResolver(string[] codes) : IScopeSubjectResolver
{
	/// <inheritdoc />
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder()
		                                            .AddCodes(codes)
		                                            .Add(ScopeDimensions.Dept, "team-a")
		                                            .Build());
	}
}
