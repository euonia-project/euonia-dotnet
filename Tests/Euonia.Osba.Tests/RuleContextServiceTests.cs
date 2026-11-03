using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证规则上下文可以从目标对象的当前作用域解析服务。
/// </summary>
public class RuleContextServiceTests
{
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
	public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// 通过 <see cref="IRuleContext.GetService{T}"/> 解析服务的规则。
/// </summary>
public sealed class ServiceResolvingRule : RuleBase
{
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
	public bool Resolved { get; set; }

	public RuleProbeService ResolvedService { get; set; }

	protected override void AddRules()
	{
		Rules.AddRule<ServiceResolvingRule>();
	}
}
