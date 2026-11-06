using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 针对对象级 / 属性级规则契约的回归测试。
/// </summary>
public class RuleBaseContractTests
{
	[Fact]
	public void Interfaces_SeparateObjectAndPropertyContracts()
	{
		Assert.Null(typeof(IRuleBase).GetProperty(nameof(IPropertyRule.Property)));
		Assert.Null(typeof(IObjectRule).GetProperty(nameof(IPropertyRule.Property)));
		Assert.NotNull(typeof(IPropertyRule).GetProperty(nameof(IPropertyRule.Property)));
	}

	[Fact]
	public void ObjectRuleBase_ImplementsObjectContract_WithoutProperty()
	{
		var rule = new PlainObjectRule();

		Assert.IsAssignableFrom<IObjectRule>(rule);
		Assert.DoesNotContain(typeof(IPropertyRule), rule.GetType().GetInterfaces());
	}

	[Fact]
	public void PropertyRuleBase_ImplementsPropertyContract_WithProperty()
	{
		var property = new PropertyInfo<string>("Name");
		var rule = new PlainPropertyRule(property);

		Assert.IsAssignableFrom<IPropertyRule>(rule);
		Assert.Same(property, rule.Property);
	}

	[Fact]
	public void PropertyRuleBase_ThrowsForNullProperty()
	{
		Assert.Throws<ArgumentNullException>(() => new PlainPropertyRule(null!));
	}

	[Fact]
	public void RuleManager_ClassifiesRulesByContract()
	{
		var manager = RuleManager.GetRules<RuleContractTarget>();
		var property = new PropertyInfo<string>("Name");
		var objectRule = new PlainObjectRule();
		var propertyRule = new PlainPropertyRule(property);

		manager.AddRule(objectRule);
		manager.AddRule(propertyRule);

		try
		{
			Assert.Contains(objectRule, manager.ObjectRules);
			Assert.DoesNotContain(propertyRule, manager.ObjectRules);
			Assert.Contains(propertyRule, manager.RulesFor(property));
		}
		finally
		{
			RuleManager.CleanRules(typeof(RuleContractTarget));
		}
	}

	private sealed class PlainObjectRule : ObjectRuleBase
	{
	}

	private sealed class PlainPropertyRule(IPropertyInfo property) : PropertyRuleBase(property)
	{
	}

	private sealed class RuleContractTarget
	{
	}
}
