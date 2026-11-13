using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证宿主/插件可以在 <c>AddRules()</c> 之外向规则管理器注册规则。
/// </summary>
public class RuleRegistrationTests
{
	/// <summary>
	/// 通过注册表取得的规则管理器可追加规则，并影响之后创建的对象。
	/// </summary>
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
public sealed class ExternalAlwaysFailRule : ObjectRuleBase
{
	/// <summary>
	/// 向上下文添加固定的错误结果，使对象校验必然失败。
	/// </summary>
	/// <param name="context">当前规则上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>表示操作完成的任务。</returns>
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
	/// <summary>
	/// Name 属性的元数据，由旧的重载推断属性名。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty(x => x.Name);

	/// <summary>
	/// 未在 <c>AddRules</c> 中声明规则、由外部注册规则约束的名称属性。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}
}
