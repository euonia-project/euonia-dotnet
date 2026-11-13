using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 对象级规则轮次的清理口径：<b>必须与 <c>BrokenRuleCollection.ClearRules(string)</c> 的序数相等保持一致</b>。
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
	/// <summary>
	/// 验证仅大小写不同的两个属性在连续三轮对象级规则检查后仍各保留一条错误，不会跨轮累积。
	/// </summary>
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

	/// <summary>
	/// 每次都添加错误结果的属性级规则，用于验证重复检查不会累加错误数。
	/// </summary>
	private sealed class AlwaysFailPropertyRule(IPropertyInfo property)
		: PropertyRuleBase(property)
	{
		public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
		{
			context.AddErrorResult("恒失败的属性级规则");
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// 注册仅大小写不同的两个属性名的业务对象，用于验证规则管理区分大小写。
	/// </summary>
	private sealed class CaseOnlyDuplicateSubject : BusinessObject<CaseOnlyDuplicateSubject>
	{
		internal static readonly PropertyInfo<string> UpperProperty = RegisterProperty<string>("Name");
		internal static readonly PropertyInfo<string> LowerProperty = RegisterProperty<string>("name");

		public Nerosoft.Euonia.Osba.Rules PublicRules => Rules;
	}
}
