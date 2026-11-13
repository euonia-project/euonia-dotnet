namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 验证器。
/// </summary>
public class Validator
{
    /// <summary>
    /// 验证指定的对象。
    /// </summary>
    /// <typeparam name="T">要验证的对象的类型。</typeparam>
    /// <param name="item">要验证的实例，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ValidationException"><paramref name="item"/> 实现 <see cref="IValidatableObject"/> 且验证失败时抛出，包含其错误集合。</exception>
    public static void Validate<T>(T item)
        where T : class
    {
		ArgumentNullException.ThrowIfNull(item);
		
        if (item is IValidatableObject @object)
        {
            @object.Validate();
            if (!@object.IsValid)
            {
                var errors = @object.Errors.Select(error => new ValidationResult("Value", error));
                throw new ValidationException(string.Empty, errors);
            }
        }
        else
        {
            var validator = ValidatorFactory.Create();
            validator?.Validate(item);
        }
    }

	/// <summary>
	/// 异步验证指定的对象。
	/// </summary>
	/// <typeparam name="T">要验证的对象的类型。</typeparam>
	/// <param name="item">要验证的实例，不能为 <see langword="null"/>。</param>
	/// <returns>表示异步验证过程的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="item"/> 为 <see langword="null"/>。</exception>
	/// <exception cref="ValidationException"><paramref name="item"/> 实现 <see cref="IValidatableObject"/> 且验证失败时抛出，包含其错误集合。</exception>
	public static async Task ValidateAsync<T>(T item)
		where T : class
	{
		ArgumentNullException.ThrowIfNull(item);
		
		if (item is IValidatableObject @object)
		{
			@object.Validate();
			if (!@object.IsValid)
			{
				var errors = @object.Errors.Select(error => new ValidationResult("Value", error));
				throw new ValidationException(string.Empty, errors);
			}
		}
		else
		{
			var validator = ValidatorFactory.Create();
			if (validator != null)
			{
				await validator.ValidateAsync(item);
			}
		}
	}
}