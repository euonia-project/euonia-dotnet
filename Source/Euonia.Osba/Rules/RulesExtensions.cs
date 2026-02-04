namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The extension methods for <see cref="Rules"/>.
/// </summary>
public static class RulesExtensions
{
	/// <param name="rules"></param>
	extension(Rules rules)
	{
		/// <summary>
		/// Add a lambda expression rule to business object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="property"></param>
		/// <param name="handler"></param>
		/// <param name="message"></param>
		public void AddRule<T>(IPropertyInfo property, Func<T, Task<bool>> handler, string message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					return handler(target);
				}
			}, message);
			//var methodName = handler.Method.ToString();
			//rule.AddQueryParameter("s", Convert.ToBase64String(Encoding.Unicode.GetBytes(methodName)));

			rules.AddRule(rule);
		}

		/// <summary>
		/// Add a lambda expression rule to business object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="property"></param>
		/// <param name="handler"></param>
		/// <param name="message"></param>
		public void AddRule<T>(IPropertyInfo property, Func<T, Task<bool>> handler, Func<string> message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					return handler(target);
				}
			}, message);
			//var methodName = handler.Method.ToString();
			//rule.AddQueryParameter("s", Convert.ToBase64String(Encoding.Unicode.GetBytes(methodName)));

			rules.AddRule(rule);
		}
	}
}