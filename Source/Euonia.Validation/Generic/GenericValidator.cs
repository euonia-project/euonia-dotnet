using System.Linq.Expressions;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 指定类型的泛型验证器。
/// </summary>
/// <typeparam name="TValue">要验证的值的类型。</typeparam>
public class GenericValidator<TValue> : AbstractValidator<TValue>
{
	private readonly Func<TValue, string> _function;

	/// <summary>
	/// 初始化 <see cref="GenericValidator{TValue}"/> 类的新实例。
	/// </summary>
	/// <param name="expression">返回错误消息的表达式；返回空字符串表示验证通过。</param>
	public GenericValidator(Expression<Func<TValue, string>> expression)
	{
		_function = expression.Compile();
	}

	/// <summary>
	/// 初始化 <see cref="GenericValidator{TValue}"/> 类的新实例。
	/// </summary>
	/// <param name="expression">验证条件表达式；返回 <see langword="true"/> 表示验证通过。</param>
	/// <param name="message">验证失败时使用的错误消息。</param>
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