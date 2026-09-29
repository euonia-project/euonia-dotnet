using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 对象级规则轮次的清理口径：<b>必须与 <c>BrokenRuleCollection.ClearRules(string)</b> 的序数相等保持一致</b>。
/// </summary>
/// <remarks>
/// <c>Rules.PrepareObjectRules</c> 先按 <c>DistinctBy(rule.Property.Name)</c> 挑出本轮涉及的属性，
/// 再逐个定向清理；而清理侧是 <c>rule.Property != propertyName</c>（<c>string</c> 的序数比较）。
/// 两侧口径一旦不一致，仅大小写不同的两个属性只会清理其中一个，另一侧的陈旧条目跨轮累积——
/// 正是该方法注释警告的「重复检查会逐轮累积重复条目」。
/// 属性注册是大小写敏感的（<c>PropertyComparer</c> 用 <c>InvariantCulture</c>），因此 <c>Name</c> 与 <c>name</c> 可以并存。
/// </remarks>
public class BrokenRuleCaseSensitivityTests
{
	[Fact]
	public void Repeated_Object_Check_Rounds_Should_Not_Accumulate_Entries_For_Case_Only_Different_Properties()
	{
		using var scope = RuleTestHarness.CreateScope(out var provider);
		var subject = RuleTestHarness.Create<CaseOnlyDuplicateSubject>(provider);

		subject.PublicRules.AddInstanceRule(new AlwaysFailPropertyRule(CaseOnlyDuplicateSubject.UpperProperty));
		subject.PublicRules.AddInstanceRule(new AlwaysFailPropertyRule(CaseOnlyDuplicateSubject.LowerProperty));

		subject.PublicRules.CheckObjectRules(cascade: false);
		Assert.Equal(2, subject.PublicRules.BrokenRules.ErrorCount);

		subject.PublicRules.CheckObjectRules(cascade: false);
		Assert.Equal(2, subject.PublicRules.BrokenRules.ErrorCount);

		subject.PublicRules.CheckObjectRules(cascade: false);
		Assert.Equal(2, subject.PublicRules.BrokenRules.ErrorCount);
		Assert.False(subject.PublicRules.IsValid);
	}

	private sealed class AlwaysFailPropertyRule(IPropertyInfo property)
		: RuleBase(property)
	{
		public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
		{
			context.AddErrorResult("恒失败的属性级规则");
			return Task.CompletedTask;
		}
	}

	private sealed class CaseOnlyDuplicateSubject : BusinessObject<CaseOnlyDuplicateSubject>
	{
		internal static readonly PropertyInfo<string> UpperProperty = RegisterProperty<string>("Name");
		internal static readonly PropertyInfo<string> LowerProperty = RegisterProperty<string>("name");

		public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;
	}
}
