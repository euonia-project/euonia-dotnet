using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 验证执行器按操作指定规则的能力：追加、排除、跳过，及其作用域边界。
/// </summary>
/// <remarks>
/// <para>
/// 这里大量复用 <see cref="RuleCleanEditable"/>（无类型级规则）与 <see cref="RuleFailEditable"/>：
/// 实例级/操作级规则<b>不进入</b>按类型共享的规则管理器，因此不会跨场景污染——
/// 这正是本次改造要保证的性质，测试本身也依赖它。
/// </para>
/// </remarks>
public class ActuatorRuleTests
{
	#region 追加规则

	[Fact]
	public async Task Update_WithRuleInstance_ShouldFailSave()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRule(new AlwaysFailObjectRule())
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		Assert.Contains("对象级规则失败", exception.Errors.Select(error => error.ErrorMessage));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithRuleGeneric_ShouldFailSave()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRule<AlwaysFailObjectRule>()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithRuleFromProvider_ShouldResolveDependency()
	{
		using var scope = RuleTestHarness.CreateScope(
			out var provider,
			services => services.AddSingleton<RuleTestDependency>());
		var actuator = provider.GetRequiredService<IActuator>();

		var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRule<DependentFailRule>(provider)
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		Assert.Contains("dependency=from-di", exception.Errors.Select(error => error.ErrorMessage));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithRules_ShouldApplyAll()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRules([new AlwaysFailObjectRule(), new DependentFailRule(new RuleTestDependency())])
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		var messages = exception.Errors.Select(error => error.ErrorMessage).ToList();
		Assert.Contains("对象级规则失败", messages);
		Assert.Contains("dependency=from-di", messages);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithRule_ShouldRunAfterHandle_AndSeePopulatedValues()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();
		var rule = new NameSnapshotFailRule();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "from-handle")
			             .WithRule(rule)
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		Assert.Equal("from-handle", rule.Observed);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithRule_ShouldNotLeakToOtherInstancesOfSameType()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleCleanEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .WithRule<AlwaysFailObjectRule>()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		// 同类型的下一次操作不带该规则，必须照常通过
		var result = await actuator.For<RuleCleanEditable>()
		                           .Update("id")
		                           .Handle(editable => editable.Name = "second")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, result.UpdateCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Execute_WithRule_ShouldBlockCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var command = await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleWarnCommand>()
			             .Execute()
			             .WithRule<AlwaysFailObjectRule>()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		Assert.Contains("对象级规则失败", command.Errors.Select(error => error.ErrorMessage));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 排除规则

	[Fact]
	public async Task Update_BypassRuleByType_ShouldExcludeTypeLevelRule()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		// 对照：不排除时保存被类型级规则拦下
		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleFailEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		// 排除后放行
		var result = await actuator.For<RuleFailEditable>()
		                           .Update("id")
		                           .Handle(editable => editable.Name = "changed")
		                           .BypassRule<AlwaysFailObjectRule>()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, result.UpdateCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_BypassRuleByName_ShouldExcludeTypeLevelRule()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();
		var ruleName = new AlwaysFailObjectRule().Name;

		var result = await actuator.For<RuleFailEditable>()
		                           .Update("id")
		                           .Handle(editable => editable.Name = "changed")
		                           .BypassRule(ruleName)
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.Equal(1, result.UpdateCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_BypassRule_ExactTypeMatch_ShouldNotExcludeDerivedRules()
	{
		// 按精确类型匹配：排除基类不得连带排除派生规则（否则一次笔误会关掉框架自动注入的规则）
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleFailEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .BypassRule<RuleBase>()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 跳过检查

	[Fact]
	public async Task Update_WithoutRuleChecks_ShouldSaveSuccessfully_EvenWithFailingRule()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleFailEditable>()
		                           .Update("id")
		                           .Handle(editable => editable.Name = "changed")
		                           .WithoutRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		// 保存确实发生了，说明跳过检查后终结步骤照常执行
		Assert.Equal(1, result.UpdateCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithoutRuleChecks_ShouldNotAffectNextOperation()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await actuator.For<RuleFailEditable>()
		             .Update("id")
		             .Handle(editable => editable.Name = "changed")
		             .WithoutRuleChecks()
		             .ExecuteAsync(TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleFailEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Update_WithoutRuleChecks_ShouldNotClobberCallerOwnSuspension()
	{
		// 执行器的跳过声明与调用方自己的挂起状态分开记录，
		// 否则一次 WithoutRuleChecks 会「恢复」掉调用方挂起的检查
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var editable = RuleTestHarness.Create<RuleCleanEditable>(provider);
		editable.SuspendRuleChecking();

		var target = await actuator.For<RuleCleanEditable>()
		                           .Update("id")
		                           .Handle(item => item.Name = "changed")
		                           .WithoutRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.NotSame(editable, target);

		// 调用方自己的挂起仍然有效：清空违规集合后 IsValid 不会因检查被改写
		editable.GetBrokenRules().Clear();
		Assert.True(await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Execute_WithoutRuleChecks_ShouldRunCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleFailCommand>()
		                           .Execute()
		                           .WithoutRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Executed);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 删除时的规则开关

	[Fact]
	public async Task Delete_WithRule_ShouldNotRunRulesByDefault()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleDeleteEditable>()
		                           .Delete("id")
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		// 删除默认不检查对象级规则（与 MarkAsDeleted() 的默认值一致）
		Assert.Equal(1, result.DeleteCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Delete_WithRuleChecksOnDelete_ShouldRunRules()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleDeleteEditable>()
			             .Delete("id")
			             .WithRuleChecksOnDelete()
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 并发隔离

	[Fact]
	public async Task Update_ConcurrentOperations_ShouldNotPolluteSharedRules()
	{
		// 附加规则只挂在目标对象上，绝不写入按类型共享的规则管理器；
		// 否则并发操作会互相看到对方的规则，一半的请求会「莫名」失败。
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		const int rounds = 12;

		var tasks = Enumerable.Range(0, rounds).Select(async index =>
		{
			var fails = index % 2 == 0;

			var operation = actuator.For<RuleCleanEditable>()
			                        .Update("id")
			                        .Handle(editable => editable.Name = $"name-{index}");

			if (fails)
			{
				operation = (UpdateActuator<RuleCleanEditable>)operation.WithRule<AlwaysFailObjectRule>();
			}

			try
			{
				await operation.ExecuteAsync(TestContext.Current.CancellationToken);
				return fails ? "unexpected-success" : "ok";
			}
			catch (ValidationException)
			{
				return fails ? "expected-failure" : "unexpected-failure";
			}
		});

		var outcomes = await Task.WhenAll(tasks);

		Assert.Equal(rounds / 2, outcomes.Count(outcome => outcome == "expected-failure"));
		Assert.Equal(rounds / 2, outcomes.Count(outcome => outcome == "ok"));
		Assert.DoesNotContain("unexpected-failure", outcomes);
		Assert.DoesNotContain("unexpected-success", outcomes);

		BusinessContextAccessor.Clear();
	}

	#endregion
}
