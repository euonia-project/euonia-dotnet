using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 默认验证器。
/// </summary>
public class DefaultValidator : IValidator
{
	private readonly IServiceProvider _provider;

	/// <summary>
	/// 初始化 <see cref="DefaultValidator"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析 FluentValidation 验证器的服务提供程序。</param>
	public DefaultValidator(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// 验证给定的项。
	/// </summary>
	/// <typeparam name="T">要验证的对象的类型。</typeparam>
	/// <param name="item">要验证的实例。</param>
	/// <exception cref="ValidationException">找到对应的 FluentValidation 验证器且验证失败时抛出，包含各字段的错误信息。</exception>
	public void Validate<T>(T item) where T : class
	{
		var validator = _provider.GetService<IValidator<T>>();
		var result = validator?.Validate(item);
		if (result == null)
		{
			return;
		}

		if (result.IsValid)
		{
			return;
		}

		var errors = result.Errors.Select(error => new ValidationResult(error.PropertyName, error.ErrorMessage));
		throw new ValidationException(string.Empty, errors);
	}

	/// <summary>
	/// 异步验证给定的项。
	/// </summary>
	/// <typeparam name="T">要验证的对象的类型。</typeparam>
	/// <param name="item">要验证的实例。</param>
	/// <returns>表示异步验证过程的任务。</returns>
	/// <exception cref="ValidationException">找到对应的 FluentValidation 验证器且验证失败时抛出，包含各字段的错误信息。</exception>
	public Task ValidateAsync<T>(T item) where T : class
	{
		var validator = _provider.GetService<IValidator<T>>();

		if (validator == null)
		{
			return Task.CompletedTask;
		}

		return validator.ValidateAsync(item)
		                .ContinueWith(task =>
		                {
			                if (task.Result.IsValid)
			                {
				                return;
			                }

			                var errors = task.Result.Errors.Select(error => new ValidationResult(error.PropertyName, error.ErrorMessage));
			                throw new ValidationException(string.Empty, errors);
		                });
	}
}