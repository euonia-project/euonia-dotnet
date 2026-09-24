using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证数据权限规则（<see cref="ScopePolicyRule"/>）的自动注入是<b>按容器</b>判定的。
/// </summary>
/// <remarks>
/// 「本类型是否声明了权限模型」是容器内的事实（<see cref="ScopeModelRegistry"/> 是容器单例），
/// 而规则注册按类型进程级缓存——两者粒度不同，因此规则的有无<b>不得取决于哪个容器先初始化了
/// 这个类型</b>。这里用「先在未声明模型的容器里触达类型、再在声明了模型的容器里保存越权数据」
/// 来固定这一点：越权必须仍以<b>字段级验证错误</b>（<see cref="Nerosoft.Euonia.Validation.ValidationException"/>）
/// 暴露，而不是只剩工厂边界的 <see cref="System.Security.SecurityException"/>。
/// </remarks>
public class ScopeRuleInjectionTests
{
	[Fact]
	public async Task ScopeRule_ShouldFire_EvenIfTypeWasFirstTouchedWithoutModelDeclarations()
	{
		// 先在未声明任何权限模型的容器里触达该类型——这一步会完成按类型缓存的规则初始化
		using (RuleTestHarness.CreateScope(out var warmupProvider))
		{
			_ = RuleTestHarness.Create<ScopeOrder>(warmupProvider);
		}

		BusinessContextAccessor.Clear();

		using var scope = CreateDeclaringScope(out var provider);

		var order = RuleTestHarness.Create<ScopeOrder>(provider);
		order.TeamId = "TeamX";          // 解析器未授予任何 team 值 ⇒ 越权
		order.MarkAsNew();

		// 规则阶段应拦下：得到带字段级错误的 ValidationException，
		// 而不是只能由工厂边界兜住的 SecurityException
		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => order.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains(
			exception.Errors,
			error => error.ErrorMessage.Contains("数据范围", StringComparison.Ordinal));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ScopeRule_ShouldNotBeInjectedTwice_WhenContextIsAssignedRepeatedly()
	{
		using var scope = CreateDeclaringScope(out var provider);

		var order = RuleTestHarness.Create<ScopeOrder>(provider);
		order.TeamId = "TeamX";

		// 重复接线不得叠加同一条规则（否则同一次检查会报出两条同样的错误）
		order.BusinessContext = provider.GetRequiredService<BusinessContext>();
		order.BusinessContext = provider.GetRequiredService<BusinessContext>();

		await order.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(1, order.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ScopeRule_ShouldNotBeInjected_ForTypeWithoutModel()
	{
		// 同一容器里，未声明模型的类型不受数据权限约束，也不该被注入规则
		using var scope = CreateDeclaringScope(out var provider);

		var editable = RuleTestHarness.Create<RuleCleanEditable>(provider);

		Assert.True(await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));
		Assert.Empty(editable.GetBrokenRules());

		BusinessContextAccessor.Clear();
	}

	private static IServiceScope CreateDeclaringScope(out IServiceProvider provider)
	{
		var services = new ServiceCollection();
		// 扫描测试程序集 ⇒ 注册表包含 ProbeOrderModel，ScopeOrder 因此受数据权限约束
		services.AddBusinessObject(typeof(ScopeRuleInjectionTests).Assembly);
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
	public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);

	public string TeamId
	{
		get => GetProperty(TeamIdProperty);
		set => SetProperty(TeamIdProperty, value);
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// <see cref="ScopeOrder"/> 的权限模型：按 team 维度授权。
/// </summary>
public sealed class ScopeOrderModel : ScopeModel<ScopeOrder>
{
	public override void Define(ScopeModelBuilder<ScopeOrder> builder)
	{
		builder.Map("team", order => order.TeamId);
	}

	public override ScopePolicy<ScopeOrder> Policy => ScopePolicy<ScopeOrder>.Grant("team");
}

/// <summary>
/// 不授予任何维度值的解析器：任何 <see cref="ScopeOrder"/> 都是越权的。
/// </summary>
public sealed class NoGrantResolver : IScopeSubjectResolver
{
	public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder().AddCodes(["order:write"]).Build());
	}
}
