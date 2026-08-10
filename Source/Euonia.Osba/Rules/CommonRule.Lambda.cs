namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 提供通用规则集以验证属性。
/// </summary>
public partial class CommonRule
{
	/// <summary>
	/// 使用 lambda 表达式提供属性验证。
	/// </summary>
	public class Lambda : CommonRuleBase
	{
		/// <inheritdoc />
		public Lambda(IPropertyInfo property, Func<object, IRuleContext, Task<bool>> handler, string message)
			: base(property, message)
		{
			Handler = handler;
		}

		/// <inheritdoc />
		public Lambda(IPropertyInfo property, Func<object, IRuleContext, Task<bool>> handler, Func<string> messageFactory)
			: base(property, messageFactory)
		{
			Handler = handler;
		}

		/// <summary>
		/// 获取处理函数。
		/// </summary>
		private Func<object, IRuleContext, Task<bool>> Handler { get; }

		/// <inheritdoc />
		public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
		{
			if (context.Target is IBusinessObject target)
			{
				var value = target.ReadProperty(Property);

				var result = await Handler(value, context);

				if (!result)
				{
					context.AddErrorResult(string.Format(MessageFactory(), Property.FriendlyName));
				}
			}
			else
			{
				await Task.CompletedTask;
			}
		}
	}

	/// <summary>
	/// 使用强类型 lambda 表达式提供属性验证。
	/// </summary>
	/// <typeparam name="T">属性值的类型。</typeparam>
	public class Lambda<T> : CommonRuleBase
	{
		/// <inheritdoc />
		public Lambda(PropertyInfo<T> property, Func<T, IRuleContext, bool> handler, string message)
			: base(property, message)
		{
			Handler = handler;
		}

		/// <inheritdoc />
		public Lambda(PropertyInfo<T> property, Func<T, IRuleContext, bool> handler, Func<string> messageFactory)
			: base(property, messageFactory)
		{
			Handler = handler;
		}

		/// <summary>
		/// 获取处理函数。
		/// </summary>
		private Func<T, IRuleContext, bool> Handler { get; }

		/// <inheritdoc />
		public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
		{
			if (context.Target is IBusinessObject target)
			{
				var value = (T)target.ReadProperty(Property);

				var result = Handler(value, context);

				if (!result)
				{
					context.AddErrorResult(string.Format(MessageFactory(), Property.FriendlyName));
				}
			}

			await Task.CompletedTask;
		}
	}
}