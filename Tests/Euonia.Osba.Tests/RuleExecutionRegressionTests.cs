using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 规则执行路径的回归护栏：
/// <list type="bullet">
/// <item>规则抛出的异常不能被悄悄吞掉——结果里必须留下异常类型与消息；</item>
/// <item>取消必须原样传播，不能伪装成「校验不通过」；</item>
/// <item>规则完成回调（<c>RuleCheckComplete</c> → <c>PropertyChanged</c>）不得在规则管理器的锁内执行。</item>
/// </list>
/// </summary>
public class RuleExecutionRegressionTests
{
	[Fact]
	public async Task Rule_That_Throws_Should_Keep_The_Exception_Type_In_The_Result()
	{
		var target = new RulePropertyBoundEditable();
		target.PublicRules.AddInstanceRule(new ThrowingPropertyRule(RulePropertyBoundEditable.NameProperty));

		await target.PublicRules.CheckObjectRulesAsync(false, TestContext.Current.CancellationToken);

		Assert.Contains(target.GetBrokenRules(), broken =>
			broken.Description.Contains("[InvalidOperationException]", StringComparison.Ordinal)
			&& broken.Description.Contains("boom", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Cancelling_A_Rule_Should_Propagate_Instead_Of_Becoming_A_Validation_Error()
	{
		var target = new RulePropertyBoundEditable();
		target.PublicRules.AddInstanceRule(new CancelledPropertyRule(RulePropertyBoundEditable.NameProperty));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			target.PublicRules.CheckObjectRulesAsync(false, TestContext.Current.CancellationToken));

		Assert.Empty(target.GetBrokenRules());
	}

	[Fact]
	public async Task Rule_Completion_Should_Notify_Outside_The_Rules_Lock()
	{
		var target = new RulePropertyBoundEditable();

		var rulesLock = typeof(Nerosoft.Euonia.Osba.Rules)
			.GetField("_lockObject", BindingFlags.Instance | BindingFlags.NonPublic)
			!.GetValue(target.PublicRules);

		bool? heldDuringNotification = null;
		target.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(RulePropertyBoundEditable.Name))
			{
				heldDuringNotification = Monitor.IsEntered(rulesLock);
			}
		};

		target.PublicRules.AddInstanceRule(new PropertyBoundFailRule(RulePropertyBoundEditable.NameProperty));
		await target.PublicRules.CheckObjectRulesAsync(false, TestContext.Current.CancellationToken);

		Assert.True(heldDuringNotification.HasValue, "规则完成后应对 Name 发出 PropertyChanged 通知。");
		Assert.False(heldDuringNotification.Value, "规则完成回调不得在 Rules 的锁内执行——那是把用户代码带进了临界区。");
	}

	[Fact]
	public async Task DataAnnotationRule_That_Throws_Should_Keep_The_Exception_Type_In_The_Result()
	{
		// 数据注解规则曾自行 catch(Exception) 并只留 message，是规则异常策略唯一的旁路；
		// 它必须和其余规则一样把异常类型（含内部异常链）写进错误描述。
		var target = new RulePropertyBoundEditable();
		target.PublicRules.AddInstanceRule(
			new DataAnnotationRule(RulePropertyBoundEditable.NameProperty, new ExplodingValidationAttribute()));

		await target.PublicRules.CheckObjectRulesAsync(false, TestContext.Current.CancellationToken);

		Assert.Contains(target.GetBrokenRules(), broken =>
			broken.Description.Contains("[InvalidOperationException]", StringComparison.Ordinal)
			&& broken.Description.Contains("attribute boom", StringComparison.Ordinal));
	}
}

/// <summary>
/// 抛出 <see cref="InvalidOperationException"/> 的属性级规则。
/// </summary>
internal sealed class ThrowingPropertyRule(IPropertyInfo property) : RuleBase(property)
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		throw new InvalidOperationException("boom");
	}
}

/// <summary>
/// 以 <see cref="OperationCanceledException"/> 结束的属性级规则。
/// </summary>
internal sealed class CancelledPropertyRule(IPropertyInfo property) : RuleBase(property)
{
	/// <inheritdoc />
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		throw new OperationCanceledException();
	}
}

/// <summary>
/// 在校验阶段抛出 <see cref="InvalidOperationException"/> 的数据注解特性。
/// </summary>
internal sealed class ExplodingValidationAttribute : ValidationAttribute
{
	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		throw new InvalidOperationException("attribute boom");
	}
}
