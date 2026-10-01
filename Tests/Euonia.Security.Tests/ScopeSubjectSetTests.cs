using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 授权数据集合的查找语义：维度名大小写不敏感、值大小写敏感、按码覆盖默认键。
/// <para>
/// 这些规则以前散落在「字典套字典」的查找代码里；集合形状改为「策略键 → 主体（维度 + 值）集合」后，
/// 大小写不敏感由主体的相等性保证，因此值得单独钉住——规则换了实现，承诺不能跟着变。
/// </para>
/// </summary>
public class ScopeSubjectSetTests
{
	[Fact]
	public void ValuesOf_Should_Match_Dimension_Case_Insensitively_And_Value_Case_Sensitively()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
		                              .Add("Dept", "TeamA")
		                              .Build();

		// 维度名大小写不敏感：换任意大小写都能查到同一批值
		Assert.Equal(["TeamA"], subjects.ValuesOf(ScopeKeys.Default, "dept"));
		Assert.Equal(["TeamA"], subjects.ValuesOf(ScopeKeys.Default, "DEPT"));

		// 值大小写敏感：授予的是 TeamA，不是 teama
		Assert.Contains("TeamA", subjects.ValuesOf(ScopeKeys.Default, "dept"));
		Assert.DoesNotContain("teama", subjects.ValuesOf(ScopeKeys.Default, "dept"));
	}

	[Fact]
	public void Contains_Should_Follow_The_Same_Rules()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
		                              .Add("Dept", "TeamA")
		                              .Build();

		Assert.True(subjects.Contains(ScopeKeys.Default, "dept", "TeamA"));
		Assert.True(subjects.Contains(ScopeKeys.Default, "DEPT", "TeamA"));
		Assert.False(subjects.Contains(ScopeKeys.Default, "dept", "teama"));
		Assert.False(subjects.Contains(ScopeKeys.Default, "region", "TeamA"));
	}

	[Fact]
	public void Code_Grant_Should_Override_The_Default_Key_Not_Union()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
		                              .Add("dept", "from-default")
		                              .AddGrant("order:edit", "dept", "from-code")
		                              .Build();

		// 该码上有该维度的授予 ⇒ 用它，而不是「默认 ∪ 码级」
		Assert.Equal(["from-code"], subjects.ValuesOf("order:edit", "dept"));

		// 该码上没有授予的维度照旧回落到默认键
		Assert.Equal(["from-default"], subjects.ValuesOf("order:other", "dept"));
	}

	[Fact]
	public void ValuesOf_Should_Be_Empty_When_Nothing_Is_Granted()
	{
		var subjects = ScopeSubjectSet.CreateBuilder().Add("dept", "team-a").Build();

		Assert.Empty(subjects.ValuesOf(ScopeKeys.Default, "region"));
		Assert.False(subjects.Contains(ScopeKeys.Default, "dept", null));

		// 权限码为 null 视作默认键（不是「无授予」）
		Assert.Equal(["team-a"], subjects.ValuesOf(null, "dept"));
	}

	[Fact]
	public void KeysWithGrants_Should_List_Only_Keys_That_Have_Values()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
		                              .AddCode("order:read")
		                              .Add("dept", "team-a")
		                              .AddGrant("order:edit", "dept", "team-a")
		                              .Build();

		// 只持有权限码、没有任何维度授予的码不出现在这里
		Assert.Contains(ScopeKeys.Default, subjects.KeysWithGrants);
		Assert.Contains("order:edit", subjects.KeysWithGrants);
		Assert.DoesNotContain("order:read", subjects.KeysWithGrants);
	}

	[Fact]
	public void HoldsPermission_Should_Support_Prefix_Wildcard_But_Not_Feed_Dimension_Lookup()
	{
		var subjects = ScopeSubjectSet.CreateBuilder()
		                              .AddCode("repo:*")
		                              .AddGrant("repo:*", "repository", "r1")
		                              .Build();

		Assert.True(subjects.HoldsPermission("repo:push"));
		Assert.False(subjects.HoldsPermission("order:read"));

		// 通配不参与维度查找：repo:push 不会顺带拿到 (repo:*, repository) 的授予
		Assert.Empty(subjects.ValuesOf("repo:push", "repository"));
		Assert.Equal(["r1"], subjects.ValuesOf("repo:*", "repository"));
	}
}
