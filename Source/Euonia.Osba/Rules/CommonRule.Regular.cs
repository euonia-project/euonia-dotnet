using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Osba;

public partial class CommonRule
{
	/// <summary>
	/// Provides property validation using regular expression.
	/// </summary>
	public class Regular : CommonRuleBase
	{
		private readonly Regex _regex;

		/// <summary>
		/// Initialize a new instance of <see cref="Regular"/>.
		/// </summary>
		/// <param name="property">The property to check.</param>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <param name="message"></param>
		public Regular(IPropertyInfo property, string expression, string message)
			: base(property, message)
		{
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// Initialize a new instance of <see cref="Regular"/>.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="expression">The regular expression pattern to match.</param>
		/// <param name="messageFactory"></param>
		public Regular(IPropertyInfo property, string expression, Func<string> messageFactory)
			: base(property, messageFactory)
		{
			Expression = expression;
			_regex = new Regex(Expression);
		}

		/// <summary>
		/// Gets the regular expression pattern to match property value.
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the null value should be ignored.
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