using System.Linq.Expressions;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// The generic validator for specified type.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public class GenericValidator<TValue> : AbstractValidator<TValue>
{
	private readonly Func<TValue, string> _function;

	/// <summary>
	/// Initializes a new instance of the <see cref="GenericValidator{TValue}"/> class.
	/// </summary>
	/// <param name="expression"></param>
	public GenericValidator(Expression<Func<TValue, string>> expression)
	{
		_function = expression.Compile();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GenericValidator{TValue}"/> class.
	/// </summary>
	/// <param name="expression"></param>
	/// <param name="message"></param>
	public GenericValidator(Expression<Func<TValue, bool>> expression, string message)
	{
		var function = expression.Compile();
		_function = value => function(value) ? string.Empty : message;
	}

	/// <inheritdoc/>
	public override bool Validate(TValue value)
	{
		Message = _function.Invoke(value) ?? string.Empty;
		return string.IsNullOrEmpty(Message);
	}
}