namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="Rules"/> 的扩展方法。
/// </summary>
public static class RulesExtensions
{
	/// <param name="rules">要扩展的规则实例。</param>
	extension(Rules rules)
	{
		/// <summary>
		/// 向业务对象添加 lambda 表达式规则。
		/// </summary>
		/// <typeparam name="T">业务对象的类型。</typeparam>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="handler">验证处理函数。</param>
		/// <param name="message">规则的错误消息。</param>
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
		/// 向业务对象添加 lambda 表达式规则。
		/// </summary>
		/// <typeparam name="T">业务对象的类型。</typeparam>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="handler">验证处理函数。</param>
		/// <param name="message">生成规则消息的委托。</param>
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