using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Osba;

public partial class CommonRule
{
	/// <summary>
	/// 使用正则表达式提供属性验证。
	/// </summary>
	public class Regular : CommonRuleBase
	{
		private readonly Regex _regex;

		/// <summary>
		/// 获取用于匹配属性值的正则表达式模式。
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// 初始化 <see cref="Regular"/> 的新实例。
		/// </summary>
		/// <param name="property">要检查的属性。</param>
		/// <param name="expression">要匹配的正则表达式模式。</param>
		/// <param name="message">规则的错误消息。</param>
		/// <remarks>
		/// 正则只能匹配字符串：绑定到非 <see cref="string"/> 属性时在<b>构造期</b>抛出
		/// <see cref="NotSupportedException"/>，而不是等到每次规则检查才抛
		/// （那会被 <c>Rules.RunAsync</c> 折算成校验错误，接线错误被伪装成业务提示）。
		/// </remarks>
		public Regular(IPropertyInfo property, string expression, string message)
				: base(property, message)
		{
			EnsureStringProperty(property);
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// 初始化 <see cref="Regular"/> 的新实例。
		/// </summary>
		/// <param name="property">要检查的属性。</param>
		/// <param name="expression">要匹配的正则表达式模式。</param>
		/// <param name="messageFactory">生成规则消息的委托。</param>
		/// <remarks>类型校验口径同 <see cref="Regular(IPropertyInfo,string,string)"/>。</remarks>
		public Regular(IPropertyInfo property, string expression, Func<string> messageFactory)
				: base(property, messageFactory)
		{
			EnsureStringProperty(property);
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// 校验目标属性是字符串类型；正则匹配只对字符串有意义。
		/// </summary>
		private static void EnsureStringProperty(IPropertyInfo property)
		{
			if (property?.Type != null && property.Type != typeof(string))
			{
				throw new NotSupportedException($"The regular expression can not use on property '{property.FriendlyName}'.");
			}
		}

		/// <summary>
		/// 获取或设置一个值，指示是否应忽略 <c>null</c> 值。
		/// </summary>
		public bool IgnoreNullValue { get; set; } = true;

		/// <inheritdoc />
		public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
		{
			if (context.Target is IBusinessObject target)
			{
				var value = target.ReadProperty(Property);

				var message = value switch
				{
					string @string => _regex.IsMatch(@string) ? string.Empty : string.Format(MessageFactory(), Property.FriendlyName),
					null => IgnoreNullValue ? string.Empty : string.Format(MessageFactory(), Property.FriendlyName),
					_ => throw new NotSupportedException($"The regular expression can not use on property '{Property.FriendlyName}'.")
				};
				if (!string.IsNullOrWhiteSpace(message))
				{
					context.AddErrorResult(message);
				}
			}

			return Task.CompletedTask;
		}
	}
}