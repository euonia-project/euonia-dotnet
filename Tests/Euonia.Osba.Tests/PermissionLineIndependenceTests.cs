using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证数据权限<b>不再经规则通道</b>：权限线（工厂边界）与验证线（规则）彼此独立。
/// </summary>
/// <remarks>
/// <para>
/// 早先版本的框架会对已声明权限模型的类型<b>自动注入</b>数据范围规则，把越权表达成
/// <see cref="Nerosoft.Euonia.Validation.ValidationException"/>。拆分后该注入与
/// 「规则按类型进程级缓存 vs 模型按容器注册」的耦合一并移除——数据权限只由工厂边界裁决。
/// </para>
/// <para>
/// 这里保留「先在未声明模型的容器里触达类型」的场景：它曾经会触发按类型缓存的规则初始化，
/// 正是那条耦合的根因。现在无论类型是否先被别的容器触达，越权保存都统一被权限线拦下。
/// </para>
/// </remarks>
public class PermissionLineIndependenceTests
{
	/// <summary>
	/// 验证即使类型先在未声明权限模型的容器中被触达，越权保存仍会由工厂边界的权限线以
	/// <see cref="SecurityException"/> 拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_OutOfScope_ShouldFailWithSecurityException_EvenIfTypeWasFirstTouchedWithoutModelDeclarations()
	{
		// 先在未声明任何权限模型的容器里触达该类型（旧实现里这一步会完成按类型缓存的规则初始化）
		using (RuleTestHarness.CreateScope(out var warmupProvider))
		{
			_ = RuleTestHarness.Create<ScopeOrder>(warmupProvider);
		}

		BusinessContextAccessor.Clear();

		using var scope = CreateDeclaringScope(out var provider);

		var order = RuleTestHarness.Create<ScopeOrder>(provider);
		order.TeamId = "TeamX";          // 解析器未授予任何 team 值 ⇒ 越权
		order.MarkAsNew();

		// 不存在「规则阶段先命中」这一说了：越权统一由工厂边界的权限线拦下
		var exception = await Assert.ThrowsAsync<SecurityException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("Data scope denied", exception.Message);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 验证重复为对象接线业务上下文不会产生注入副作用，越权保存依旧被拒绝。
	/// </summary>
	[Fact]
	public async Task SaveAsync_OutOfScope_ShouldStillBeDenied_WhenContextIsAssignedRepeatedly()
	{
		using var scope = CreateDeclaringScope(out var provider);

		var order = RuleTestHarness.Create<ScopeOrder>(provider);
		order.TeamId = "TeamX";

		// 重复接线不再有任何注入副作用（旧实现有按实例幂等注入的规则）
		order.BusinessContext = provider.GetRequiredService<BusinessContext>();
		order.BusinessContext = provider.GetRequiredService<BusinessContext>();
		order.MarkAsNew();

		var exception = await Assert.ThrowsAsync<SecurityException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("Data scope denied", exception.Message);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 验证同一容器中未声明权限模型的类型不受数据权限约束，保存能够正常完成。
	/// </summary>
	[Fact]
	public async Task SaveAsync_UnmodeledType_ShouldNotBeDenied()
	{
		// 同一容器里，未声明模型的类型不受数据权限约束，也不涉及任何规则注入
		using var scope = CreateDeclaringScope(out var provider);

		var editable = RuleTestHarness.Create<RuleCleanEditable>(provider);
		editable.Name = "anything";
		editable.MarkAsNew();

		var result = await editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.NotNull(result);

		BusinessContextAccessor.Clear();
	}

	private static IServiceScope CreateDeclaringScope(out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		// 扫描测试程序集 ⇒ 注册表包含 ScopeOrderModel，ScopeOrder 因此受数据权限约束
		services.AddBusinessObject(typeof(PermissionLineIndependenceTests).Assembly);
	services.AddPermission(p => { p.Scan(typeof(PermissionLineIndependenceTests).Assembly); p.Source(ObjectPermissionCodeSource.Instance); });
		services.AddSingleton<IScopeSubjectResolver, NoGrantResolver>();
		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		provider = scope.ServiceProvider;
		BusinessContextAccessor.SetCurrent(provider);
		return scope;
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
}

/// <summary>
/// 越权判定用的资源类型（独立类型，避免与既有测试互相污染）。
/// </summary>
public class ScopeOrder : EditableObject<ScopeOrder>
{
	/// <summary>
	/// 团队标识属性的元数据。
	/// </summary>
	public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);

	/// <summary>
	/// 获取或设置用于数据范围判定的团队标识。
	/// </summary>
	public string TeamId
	{
		get => GetProperty(TeamIdProperty);
		set => SetProperty(TeamIdProperty, value);
	}

	/// <summary>
	/// 插入操作的工厂实现，测试中不执行实际操作。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示插入完成的任务。</returns>
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="ScopeOrder"/> 的权限模型：按 team 维度授权。
/// </summary>
public sealed class ScopeOrderModel : ScopeModel<ScopeOrder>
{
	/// <summary>
	/// 定义按团队维度映射的数据范围模型。
	/// </summary>
	/// <param name="builder">数据范围模型构建器。</param>
	public override void Define(ScopeModelBuilder<ScopeOrder> builder)
	{
		builder.Map("team", order => order.TeamId);
	}

	/// <summary>
	/// 获取仅授权 <c>team</c> 维度的数据范围策略。
	/// </summary>
	public override ScopePolicy<ScopeOrder> Policy => ScopePolicy<ScopeOrder>.Grant("team");
}

/// <summary>
/// 不授予任何维度值的解析器：任何 <see cref="ScopeOrder"/> 都是越权的。
/// </summary>
public sealed class NoGrantResolver : IScopeSubjectResolver
{
	/// <summary>
	/// 返回一个不授予任何数据范围维度值的主体集合，使所有 <see cref="ScopeOrder"/> 均被视为越权。
	/// </summary>
	/// <param name="user">当前用户主体。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>仅包含写入权限码、不含任何范围维度值的主体集合。</returns>
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder().AddCodes(["order:write"]).Build());
	}
}