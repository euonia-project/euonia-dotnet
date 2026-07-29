using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// The default validator.
/// </summary>
public class DefaultValidator : IValidator
{
	private readonly IServiceProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultValidator"/> class.
	/// </summary>
	/// <param name="provider"></param>
	public DefaultValidator(IServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// Validate the given item.
	/// </summary>
	/// <param name="item"></param>
	/// <typeparam name="T"></typeparam>
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
	/// Validate the given item asynchronously.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item"></param>
	/// <returns></returns>
	/// <exception cref="ValidationException"></exception>
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