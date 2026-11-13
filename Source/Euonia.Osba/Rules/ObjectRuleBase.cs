namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 对象级规则基类：不绑定具体属性，作用于整个对象。
/// </summary>
public abstract class ObjectRuleBase : RuleBase, IObjectRule
{
	/// <summary>
	/// 初始化 <see cref="ObjectRuleBase"/> 类的新实例。
	/// </summary>
	protected ObjectRuleBase()
	{
	}
}
