using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证执行器按操作指定规则的能力：追加、排除、跳过，及其作用域边界。
/// </summary>
/// <remarks>
/// <para>
/// 这里大量复用 <see cref="RuleCleanEditable"/>（无类型级规则）与 <see cref="RuleFailEditable"/>：
/// 实例级/操作级规则<b>不进入</b>按类型共享的规则管理器，因此不会跨场景污染——
/// 测试本身也依赖这条性质来复用类型。
/// </para>
/// </remarks>
public class ActuatorRuleTests
{
	#region 追加规则

	/// <summary>
	/// 以实例形式附加失败的规则时，更新保存应抛出 <see cref="ValidationException"/> 并包含规则错误信息。
	/// </summary>
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

	/// <summary>
	/// 以泛型形式附加失败的规则时，更新保存应抛出 <see cref="ValidationException"/>。
	/// </summary>
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

	/// <summary>
	/// 从服务提供程序解析规则时，应注入其依赖并返回依赖相关的失败信息。
	/// </summary>
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

	/// <summary>
	/// 同时附加多个规则时，所有规则都应被执行并汇总各自的错误。
	/// </summary>
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

	/// <summary>
	/// 规则应在 Handle 之后执行，并能看到 Handle 填充后的属性值。
	/// </summary>
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

	/// <summary>
	/// 实例级规则不应泄漏到同类型的后续操作，后续操作应正常保存。
	/// </summary>
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

	/// <summary>
	/// 命令附加失败规则时，命令主体应被阻止执行并抛出 <see cref="ValidationException"/>。
	/// </summary>
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

	/// <summary>
	/// 按类型排除规则后，原本被类型级规则拦截的保存应放行。
	/// </summary>
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

	/// <summary>
	/// 按名称排除规则后，原本被类型级规则拦截的保存应放行。
	/// </summary>
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

	/// <summary>
	/// 按类型排除规则时仅匹配精确类型，不应连带排除派生规则。
	/// </summary>
	[Fact]
	public async Task Update_BypassRule_ExactTypeMatch_ShouldNotExcludeDerivedRules()
	{
		// 按精确类型匹配：排除基类不得连带排除派生规则（否则一次笔误会关掉所有数据校验规则）
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

	/// <summary>
	/// 跳过规则检查后，即使存在会失败的规则保存也应成功完成。
	/// </summary>
	[Fact]
	public async Task Update_WithoutRuleChecks_ShouldSaveSuccessfully_EvenWithFailingRule()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleFailEditable>()
		                           .Update("id")
		                           .Handle(editable => editable.Name = "changed")
		                           .BypassRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		// 保存确实发生了，说明跳过检查后终结步骤照常执行
		Assert.Equal(1, result.UpdateCount);

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 跳过规则检查仅对当前操作生效，后续操作仍应执行规则检查并失败。
	/// </summary>
	[Fact]
	public async Task Update_WithoutRuleChecks_ShouldNotAffectNextOperation()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		await actuator.For<RuleFailEditable>()
		             .Update("id")
		             .Handle(editable => editable.Name = "changed")
		             .BypassRuleChecks()
		             .ExecuteAsync(TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<ValidationException>(async () =>
			await actuator.For<RuleFailEditable>()
			             .Update("id")
			             .Handle(editable => editable.Name = "changed")
			             .ExecuteAsync(TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 跳过规则检查不应覆盖调用方自行挂起的检查状态。
	/// </summary>
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
		                           .BypassRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.NotSame(editable, target);

		// 调用方自己的挂起仍然有效：清空违规集合后 IsValid 不会因检查被改写
		editable.GetBrokenRules().Clear();
		Assert.True(await editable.ValidateAsync(cancellationToken: TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	/// <summary>
	/// 跳过规则检查后，命令主体应照常执行。
	/// </summary>
	[Fact]
	public async Task Execute_WithoutRuleChecks_ShouldRunCommandBody()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var actuator = provider.GetRequiredService<IActuator>();

		var result = await actuator.For<RuleFailCommand>()
		                           .Execute()
		                           .BypassRuleChecks()
		                           .ExecuteAsync(TestContext.Current.CancellationToken);

		Assert.True(result.Executed);

		BusinessContextAccessor.Clear();
	}

	#endregion

	#region 删除时的规则开关

	/// <summary>
	/// 删除操作默认不执行对象级规则，应直接完成删除。
	/// </summary>
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

	/// <summary>
	/// 启用删除时的规则检查后，失败的规则应阻止删除操作。
	/// </summary>
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

	/// <summary>
	/// 并发操作附加的规则彼此隔离，不应污染按类型共享的规则管理器。
	/// </summary>
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
