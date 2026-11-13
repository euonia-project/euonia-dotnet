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
	/// <summary>
	/// 直接实现 <see cref="FluentValidation.IValidator{T}"/> 的验证器应被识别并暴露为对应的校验器接口类型。
	/// </summary>
	[Fact]
	public void GetExposedServiceTypes_WithDirectlyImplementedValidator_ShouldReturnValidatorInterface()
	{
		// 回归测试：直接实现 IValidator<T>（不继承 AbstractValidator<T>）时，
		// 沿 BaseType 查找泛型参数会递归到 null 并抛 NullReferenceException。
		var registrar = new ValidationRegistrar();
		var method = typeof(ValidationRegistrar).GetMethod("GetExposedServiceTypes", BindingFlags.Instance | BindingFlags.NonPublic);

		var services = (List<Type>)method!.Invoke(registrar, [typeof(DirectValidator)])!;

		Assert.Contains(typeof(IValidator<DirectItem>), services);
	}

	/// <summary>
	/// 被校验的简单类型，用于验证注册器能从验证器的泛型参数推导出暴露的服务类型。
	/// </summary>
	private sealed class DirectItem
	{
	}

	/// <summary>
	/// 直接实现 <see cref="IValidator{T}"/>（不继承 <see cref="FluentValidation.AbstractValidator{T}"/>）的验证器，用于覆盖注册扫描的边界情形。
	/// </summary>
	private sealed class DirectValidator : IValidator<DirectItem>
	{
		public bool CanValidateInstancesOfType(Type type) => type == typeof(DirectItem);

		public FluentValidationResult Validate(DirectItem instance) => new();

		public Task<FluentValidationResult> ValidateAsync(DirectItem instance, CancellationToken cancellation = default) => Task.FromResult(new FluentValidationResult());

		public FluentValidationResult Validate(IValidationContext context) => new();

		public Task<FluentValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) => Task.FromResult(new FluentValidationResult());

		public IValidatorDescriptor CreateDescriptor() => null!;
	}
}
