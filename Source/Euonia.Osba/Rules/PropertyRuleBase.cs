using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 属性级规则基类：绑定具体属性，<see cref="Property"/> 保证非空。
/// </summary>
public abstract class PropertyRuleBase : RuleBase, IPropertyRule
{
	/// <summary>
	/// 初始化 <see cref="PropertyRuleBase"/> 类的新实例。
	/// </summary>
	/// <param name="property">受规则影响的属性。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 <c>null</c> 时抛出。</exception>
	protected PropertyRuleBase(IPropertyInfo property)
		: base(GetPropertyName(property))
	{
		Property = property;
	}

	/// <summary>
	/// 初始化 <see cref="PropertyRuleBase"/> 类的新实例。
	/// </summary>
	/// <param name="property">受规则影响的属性。</param>
	/// <param name="validationType">验证类型成员信息。</param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="property"/> 或 <paramref name="validationType"/> 为 <c>null</c> 时抛出。
	/// </exception>
	protected PropertyRuleBase(IPropertyInfo property, MemberInfo validationType)
		: base(GetPropertyName(property), GetValidationTypeName(validationType))
	{
		Property = property;
	}

	/// <inheritdoc />
	public IPropertyInfo Property { get; }

	private static string GetPropertyName(IPropertyInfo property)
	{
		ArgumentNullException.ThrowIfNull(property);
		return property.Name;
	}

	private static string GetValidationTypeName(MemberInfo validationType)
	{
		ArgumentNullException.ThrowIfNull(validationType);
		return validationType.Name;
	}
}
