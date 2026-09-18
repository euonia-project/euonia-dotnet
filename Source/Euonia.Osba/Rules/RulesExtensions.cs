using System.Diagnostics.CodeAnalysis;

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
		public void AddRule<T>(IPropertyInfo property, [NotNull] Func<T, Task<bool>> handler, string message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, async (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					// 在 handler 完成前保持绕过规则检查，防止异步规则提前恢复检查
					return await handler(target);
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
		public void AddRule<T>(IPropertyInfo property, [NotNull] Func<T, Task<bool>> handler, Func<string> message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, async (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					// 在 handler 完成前保持绕过规则检查，防止异步规则提前恢复检查
					return await handler(target);
				}
			}, message);
			//var methodName = handler.Method.ToString();
			//rule.AddQueryParameter("s", Convert.ToBase64String(Encoding.Unicode.GetBytes(methodName)));

			rules.AddRule(rule);
		}

		/// <summary>
		/// 向业务对象添加同步 lambda 表达式规则。
		/// </summary>
		/// <typeparam name="T">业务对象的类型。</typeparam>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="handler">同步验证处理函数。</param>
		/// <param name="message">规则的错误消息。</param>
		public void AddRule<T>(IPropertyInfo property, [NotNull] Func<T, bool> handler, string message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					return Task.FromResult(handler(target));
				}
			}, message);

			rules.AddRule(rule);
		}

		/// <summary>
		/// 向业务对象添加同步 lambda 表达式规则。
		/// </summary>
		/// <typeparam name="T">业务对象的类型。</typeparam>
		/// <param name="property">受规则影响的属性。</param>
		/// <param name="handler">同步验证处理函数。</param>
		/// <param name="message">生成规则消息的委托。</param>
		public void AddRule<T>(IPropertyInfo property, [NotNull] Func<T, bool> handler, Func<string> message)
			where T : BusinessObject
		{
			var rule = new CommonRule.Lambda(property, (_, context) =>
			{
				var target = (T)context.Target;
				using (target.BypassRuleChecks)
				{
					return Task.FromResult(handler(target));
				}
			}, message);

			rules.AddRule(rule);
		}
	}
}