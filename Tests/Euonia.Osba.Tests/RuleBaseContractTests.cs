using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 针对对象级 / 属性级规则契约的回归测试。
/// </summary>
public class RuleBaseContractTests
{
	/// <summary>
	/// 对象级与属性级规则契约彼此分离：只有属性级契约暴露 <c>Property</c>。
	/// </summary>
	[Fact]
	public void Interfaces_SeparateObjectAndPropertyContracts()
	{
		Assert.Null(typeof(IRuleBase).GetProperty(nameof(IPropertyRule.Property)));
		Assert.Null(typeof(IObjectRule).GetProperty(nameof(IPropertyRule.Property)));
		Assert.NotNull(typeof(IPropertyRule).GetProperty(nameof(IPropertyRule.Property)));
	}

	/// <summary>
	/// 对象级规则基类只实现对象级契约，不暴露属性成员。
	/// </summary>
	[Fact]
	public void ObjectRuleBase_ImplementsObjectContract_WithoutProperty()
	{
		var rule = new PlainObjectRule();

		Assert.IsAssignableFrom<IObjectRule>(rule);
		Assert.DoesNotContain(typeof(IPropertyRule), rule.GetType().GetInterfaces());
	}

	/// <summary>
	/// 属性级规则基类实现属性级契约，并保留构造时传入的属性。
	/// </summary>
	[Fact]
	public void PropertyRuleBase_ImplementsPropertyContract_WithProperty()
	{
		var property = new PropertyInfo<string>("Name");
		var rule = new PlainPropertyRule(property);

		Assert.IsAssignableFrom<IPropertyRule>(rule);
		Assert.Same(property, rule.Property);
	}

	/// <summary>
	/// <see cref="PropertyRuleBase"/> 在属性为 <c>null</c> 时抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	[Fact]
	public void PropertyRuleBase_ThrowsForNullProperty()
	{
		Assert.Throws<ArgumentNullException>(() => new PlainPropertyRule(null!));
	}

	/// <summary>
	/// 规则管理器按契约把规则分入对象级集合与对应属性的集合。
	/// </summary>
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

	/// <summary>
	/// 空实现的对象级规则，用于验证其只实现对象级契约并注册到对象规则集合。
	/// </summary>
	private sealed class PlainObjectRule : ObjectRuleBase
	{
	}

	/// <summary>
	/// 空实现的属性级规则，用于验证其保留构造属性并注册到对应属性的规则集合。
	/// </summary>
	private sealed class PlainPropertyRule(IPropertyInfo property) : PropertyRuleBase(property)
	{
	}

	/// <summary>
	/// 规则注册测试用目标类型，规则管理器按此类型维护规则集合。
	/// </summary>
	private sealed class RuleContractTarget
	{
	}
}
