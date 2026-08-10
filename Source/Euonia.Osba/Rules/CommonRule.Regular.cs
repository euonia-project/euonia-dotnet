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
		/// 初始化 <see cref="Regular"/> 的新实例。
		/// </summary>
		/// <param name="property">要检查的属性。</param>
		/// <param name="expression">要匹配的正则表达式模式。</param>
		/// <param name="message">规则的错误消息。</param>
		public Regular(IPropertyInfo property, string expression, string message)
			: base(property, message)
		{
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// 初始化 <see cref="Regular"/> 的新实例。
		/// </summary>
		/// <param name="property">要检查的属性。</param>
		/// <param name="expression">要匹配的正则表达式模式。</param>
		/// <param name="messageFactory">生成规则消息的委托。</param>
		public Regular(IPropertyInfo property, string expression, Func<string> messageFactory)
			: base(property, messageFactory)
		{
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// 获取用于匹配属性值的正则表达式模式。
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// 获取或设置一个值，指示是否应忽略 <c>null</c> 值。
		/// </summary>
		public bool IgnoreNullValue { get; set; } = true;

		/// <inheritdoc />
		public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
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

			await Task.CompletedTask;
		}
	}
}