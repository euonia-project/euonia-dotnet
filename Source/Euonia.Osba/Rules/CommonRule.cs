namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 提供通用规则创建的静态辅助类（分部类）。
/// </summary>
public partial class CommonRule
{
	/// <summary>
	/// 通用规则基类。
	/// </summary>
	public abstract class CommonRuleBase : RuleBase
	{
		/// <summary>
		/// 初始化 <see cref="CommonRuleBase"/> 类的新实例。
		/// </summary>
		/// <param name="property">受规则影响的属性。</param>
		protected CommonRuleBase(IPropertyInfo property)
			: base(property)
		{
		}

		/// <summary>
		/// 初始化 <see cref="CommonRuleBase"/> 类的新实例。
		/// </summary>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="message">规则的错误消息。</param>
		protected CommonRuleBase(IPropertyInfo property, string message)
			: this(property, () => message)
		{
		}

		/// <summary>
		/// 初始化 <see cref="CommonRuleBase"/> 类的新实例。
		/// </summary>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="messageFactory">生成规则消息的委托。</param>
		protected CommonRuleBase(IPropertyInfo property, Func<string> messageFactory)
			: this(property)
		{
			MessageFactory = messageFactory;
		}

		/// <summary>
		/// 获取消息生成委托。
		/// </summary>
		protected virtual Func<string> MessageFactory { get; }
	}
}