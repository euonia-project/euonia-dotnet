namespace Nerosoft.Euonia.Osba;

public partial class CommonRule
{
	/// <summary>
	/// The common rule base.
	/// </summary>
	public abstract class CommonRuleBase : RuleBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CommonRuleBase"/> class.
		/// </summary>
		/// <param name="property"></param>
		protected CommonRuleBase(IPropertyInfo property)
			: base(property)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CommonRuleBase"/> class.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="message"></param>
		protected CommonRuleBase(IPropertyInfo property, string message)
			: this(property, () => message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CommonRuleBase"/> class.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="messageFactory"></param>
		protected CommonRuleBase(IPropertyInfo property, Func<string> messageFactory)
			: this(property)
		{
			MessageFactory = messageFactory;
		}

		/// <summary>
		/// Gets the message generate delegate.
		/// </summary>
		protected virtual Func<string> MessageFactory { get; }
	}
}