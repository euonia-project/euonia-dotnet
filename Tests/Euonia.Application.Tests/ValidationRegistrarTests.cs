using System.Reflection;
using FluentValidation;
using Nerosoft.Euonia.Validation;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对 <see cref="ValidationRegistrar"/> 自动注册扫描的回归测试。
/// </summary>
public class ValidationRegistrarTests
{
	[Fact]
	public void GetExposedServiceTypes_WithDirectlyImplementedValidator_ShouldReturnValidatorInterface()
	{
		// 回归测试：直接实现 IValidator<T>（不继承 AbstractValidator<T>）时，
		// 沿 BaseType 查找泛型参数会递归到 null 并抛 NullReferenceException。
		var registrar = new ValidationRegistrar();
		var method = typeof(ValidationRegistrar).GetMethod("GetExposedServiceTypes", BindingFlags.Instance | BindingFlags.NonPublic);

		var services = (List<Type>)method!.Invoke(registrar, [typeof(DirectValidator)]);

		Assert.Contains(typeof(IValidator<DirectItem>), services);
	}

	private sealed class DirectItem
	{
	}

	private sealed class DirectValidator : IValidator<DirectItem>
	{
		public bool CanValidateInstancesOfType(Type type) => type == typeof(DirectItem);

		public FluentValidationResult Validate(DirectItem instance) => new();

		public Task<FluentValidationResult> ValidateAsync(DirectItem instance, CancellationToken cancellation = default) => Task.FromResult(new FluentValidationResult());

		public FluentValidationResult Validate(IValidationContext context) => new();

		public Task<FluentValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) => Task.FromResult(new FluentValidationResult());

		public IValidatorDescriptor CreateDescriptor() => null;
	}
}
