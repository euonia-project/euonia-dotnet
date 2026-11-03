using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证宿主/插件可以在 <c>AddRules()</c> 之外向规则管理器注册规则。
/// </summary>
public class RuleRegistrationTests
{
	[Fact]
	public async Task AddRule_ShouldApplyToExistingRegistry()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var registry = provider.GetRequiredService<IRuleRegistry>();
		registry.GetRules(typeof(ExternallyRuledObject))
		        .AddRule(new ExternalAlwaysFailRule());

		try
		{
			var target = RuleTestHarness.Create<ExternallyRuledObject>(provider);

			Assert.False(await target.ValidateAsync(cascade: true, TestContext.Current.CancellationToken));
		}
		finally
		{
			registry.CleanRules(typeof(ExternallyRuledObject));
		}
	}
}

/// <summary>
/// 总是报告错误的测试规则。
/// </summary>
public sealed class ExternalAlwaysFailRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		context.AddErrorResult("always fails");
		return Task.CompletedTask;
	}
}

/// <summary>
/// 不在 <c>AddRules()</c> 中声明规则的测试对象。
/// </summary>
public sealed class ExternallyRuledObject : EditableObject<ExternallyRuledObject>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}
