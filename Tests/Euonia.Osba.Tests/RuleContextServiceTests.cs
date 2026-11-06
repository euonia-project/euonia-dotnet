using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证规则上下文可以从目标对象的当前作用域解析服务。
/// </summary>
public class RuleContextServiceTests
{
	/// <summary>
	/// 规则可通过上下文解析到与目标对象相同作用域的 Scoped 服务。
	/// </summary>
	[Fact]
	public async Task Rule_ShouldResolveScopedServiceFromContext()
	{
		using var scope = RuleTestHarness.CreateScope(
			out var provider,
			services => services.AddScoped<RuleProbeService>());

		var target = RuleTestHarness.Create<RuleContextProbe>(provider);

		await target.ValidateAsync(cascade: true, TestContext.Current.CancellationToken);

		Assert.True(target.Resolved);
		Assert.Same(provider.GetRequiredService<RuleProbeService>(), target.ResolvedService);
	}
}

/// <summary>
/// 供规则解析的 Scoped 服务。
/// </summary>
public sealed class RuleProbeService
{
	/// <summary>
	/// 服务实例的唯一标识，用于区分不同的解析结果。
	/// </summary>
	public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// 通过 <see cref="IRuleContext.GetService{T}"/> 解析服务的规则。
/// </summary>
public sealed class ServiceResolvingRule : ObjectRuleBase
{
	/// <summary>
	/// 从规则上下文解析 <see cref="RuleProbeService"/> 并写回目标对象的探针规则。
	/// </summary>
	/// <param name="context">当前规则上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示操作完成的任务。</returns>
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		if (context.Target is RuleContextProbe target)
		{
			target.ResolvedService = context.GetService<RuleProbeService>();
			target.Resolved = target.ResolvedService != null;
		}

		return Task.CompletedTask;
	}
}

/// <summary>
/// 在对象级规则中解析服务的测试对象。
/// </summary>
public sealed class RuleContextProbe : EditableObject<RuleContextProbe>
{
	/// <summary>
	/// 是否成功从上下文解析到服务。
	/// </summary>
	public bool Resolved { get; set; }

	/// <summary>
	/// 从上下文解析到的服务实例。
	/// </summary>
	public RuleProbeService? ResolvedService { get; set; }

	/// <summary>
	/// 注册用于解析服务的对象级规则。
	/// </summary>
	protected override void AddRules()
	{
		Rules.AddRule<ServiceResolvingRule>();
	}
}
