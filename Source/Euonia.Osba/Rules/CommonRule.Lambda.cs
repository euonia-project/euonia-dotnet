namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Provides common rule set to validate a property.
/// </summary>
public partial class CommonRule
{
	/// <summary>
	/// Provides property validation using a lambda expression.
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
		/// Gets the handler function.
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
	/// Provides property validation using a lambda expression.
	/// </summary>
	/// <typeparam name="T"></typeparam>
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
		/// Gets the handler function.
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