using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证对象级规则在各条写入口径上都真正生效，以及违规集合与挂起语义的一致性。
/// </summary>
/// <remarks>
/// 覆盖对象级规则在各条写入口径上的执行，以及三处会让规则「静默失效」的陷阱：
/// 空白描述被当作通过、挂起检查后按陈旧结论放行/拦截、外部清空违规集合后计数不归零。
/// </remarks>
public class RulePathTests
{
	#region 命令链路（核心缺陷）

	[Fact]
	public async Task ExecuteActuator_ObjectRuleFails_ShouldThrow_AndNotRunCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleFailCommand>()
			             .Execute()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		Assert.Contains("对象级规则失败", exception.Errors.Select(error => error.ErrorMessage));
		Assert.Contains("Object not valid for execute.", exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ExecuteActuator_ObjectRulePasses_ShouldRunCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RulePassCommand>()
		                           .Execute()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Executed);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ExecuteActuator_WarningOnlyRule_ShouldRunCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleWarnCommand>()
		                           .Execute()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Executed);
		Assert.Equal(1, result.GetBrokenRules().WarningCount);
		Assert.Equal(0, result.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task FactoryExecute_ObjectRuleFails_ShouldThrow_BeforeCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var factory = provider.GetRequiredService<IObjectFactory>();

		var command = RuleTestHarness.Create<RuleFailCommand>(provider);

		await Assert.ThrowsAsync<ValidationException>(
			() => factory.ExecuteAsync(command, TestContext.Current.CancellationToken));

		Assert.False(command.Executed);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_ObjectRuleFails_ShouldStillThrow_ForEditablePath()
	{
		// 可编辑路径的行为不得因本次重构而改变
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleFailEditable>(provider);
		editable.MarkAsNew();

		var exception = await Assert.ThrowsAsync<ValidationException>(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Contains("Object not valid for save.", exception.Message);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 显式校验入口

	[Fact]
	public async Task ValidateAsync_OnInvalidObject_ShouldReturnFalse_AndPopulateBrokenRules()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleFailEditable>(provider);

		// 首次检查之前 IsValid 恒为 true：违规集合还没有内容
		Assert.True(editable.IsValid);

		var valid = await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);

		Assert.False(valid);
		Assert.False(editable.IsValid);
		Assert.Equal(1, editable.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ValidateAsync_OnValidObject_ShouldReturnTrue()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleCleanEditable>(provider);

		Assert.True(await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));
		Assert.Empty(editable.GetBrokenRules());

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task EnsureValidAsync_OnInvalidObject_ShouldThrow()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleFailEditable>(provider);

		await Assert.ThrowsAsync<ValidationException>(
			() => editable.EnsureValidAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 挂起检查后的陈旧判定

	[Fact]
	public async Task SaveAsync_WhenRuleCheckingSuspended_ShouldNotThrow_OnStaleBrokenRules()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleFailEditable>(provider);
		editable.MarkAsNew();

		// 先跑一次，让违规集合里留下「上次检查」的 Error
		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.False(editable.IsValid);

		editable.SuspendRuleChecking();

		// 挂起后保存不得按上次的结论拦下
		var exception = await Record.ExceptionAsync(
			() => editable.SaveAsync(cancellationToken: TestContext.Current.CancellationToken));
		Assert.Null(exception);

		editable.ResumeRuleChecking();

		// 恢复后重新检查，违规仍在
		await Assert.ThrowsAsync<ValidationException>(
			() => editable.EnsureValidAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 违规集合的一致性

	[Fact]
	public async Task CheckObjectRules_BlankErrorResult_ShouldNotThrow_AndMarkInvalid()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleBlankMessageEditable>(provider);

		// 两条规则分别用 null 与空白描述报告失败，都不得被当成通过
		var exception = await Record.ExceptionAsync(
			() => editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));

		Assert.Null(exception);
		Assert.False(editable.IsValid);
		Assert.Equal(2, editable.GetBrokenRules().ErrorCount);
		Assert.All(
			editable.GetBrokenRules(),
			broken => Assert.False(string.IsNullOrWhiteSpace(broken.Description)));
		Assert.All(
			editable.GetBrokenRules(),
			broken => Assert.Contains("Rule message is required", broken.Description));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task CheckObjectRules_RunTwice_ShouldNotAccumulateErrors()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleFailEditable>(provider);

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(1, editable.GetBrokenRules().ErrorCount);

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(1, editable.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task BrokenRules_ClearFromOutside_ShouldResetIsValid()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleBrokenRulesEditable>(provider);

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.False(editable.IsValid);

		// 外部清空集合后计数必须归零，否则 IsValid 会永久为 false、后续每次保存都抛验证异常
		editable.GetBrokenRules().Clear();

		Assert.True(editable.IsValid);
		Assert.Equal(0, editable.GetBrokenRules().ErrorCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task BrokenRules_RemoveFromOutside_ShouldResetIsValid()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RuleBrokenRulesEditable>(provider);

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);
		Assert.False(editable.IsValid);

		editable.GetBrokenRules().RemoveAt(0);

		Assert.True(editable.IsValid);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 实例级规则

	[Fact]
	public async Task AddInstanceRule_ShouldApplyToCurrentInstanceOnly()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);

		var withRule = RuleTestHarness.Create<RuleCleanEditable>(provider);
		withRule.PublicRules.AddInstanceRule(new AlwaysFailObjectRule());

		var withoutRule = RuleTestHarness.Create<RuleCleanEditable>(provider);

		Assert.False(await withRule.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));
		Assert.True(await withoutRule.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task WithRule_PropertyBoundRule_ShouldRunInObjectPass_AndAttributeToProperty()
	{
		// 属性级规则不会随属性变更自动触发，因此绑定属性的附加规则必须被对象级检查纳入，
		// 否则 WithRule(new XxxRule(SomeProperty)) 会静默不执行。
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RulePropertyBoundEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRule(new PropertyBoundFailRule(RulePropertyBoundEditable.NameProperty))
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task WithRule_PropertyBoundRule_ShouldAttributeBrokenRuleToPropertyName()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var editable = RuleTestHarness.Create<RulePropertyBoundEditable>(provider);

		editable.PublicRules.AddInstanceRule(new PropertyBoundFailRule(RulePropertyBoundEditable.NameProperty));

		await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken);

		var broken = Assert.Single(editable.GetBrokenRules());
		Assert.Equal(RulePropertyBoundEditable.NameProperty.Name, broken.Property);

		BusinessContextAccessor.Clear();
	}

	#endregion
}
