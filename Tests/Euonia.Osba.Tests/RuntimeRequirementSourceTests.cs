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
	[FactoryUpdate]
	protected Task UpdateAsync(CancellationToken cancellationToken = default)
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

	public IReadOnlyList<string> AllOperations => [BusinessOperation.Update];

	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return (type == typeof(ScopedTask) || type == typeof(AmbientTask)) && operation == BusinessOperation.Update ? [Code] : [];
	}
}

/// <summary>授予指定权限码与部门范围的解析器（数据范围满足 <see cref="ScopedTaskModel"/> 的部门策略）。</summary>
internal sealed class HostRuleResolver(string[] codes) : IScopeSubjectResolver
{
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder()
		                                            .AddCodes(codes)
		                                            .Add(ScopeDimensions.Dept, "team-a")
		                                            .Build());
	}
}
