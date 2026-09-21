using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对 <see cref="ValidatableObject{TValue}"/> 与 <see cref="Validator"/> 的回归测试。
/// </summary>
/// <remarks>
/// <see cref="ValidatableObject{TValue}.Validate"/> 曾把 <c>IsValid</c> 赋值为
/// <c>Errors.Count &gt; 0</c>——方向完全相反：合法对象（无错误）被判定为无效并抛出空的
/// <see cref="ValidationException"/>，非法对象（有错误）反而被放行，校验同时过度触发与完全失效。
/// </remarks>
public class ValidationTests
{
	[Fact]
	public void Validate_WithNoRuleViolation_IsValidAndDoesNotThrow()
	{
		var target = CreateTarget(value: 5);

		target.Validate();

		Assert.True(target.IsValid);
		Assert.Empty(target.Errors);
		Validator.Validate(target);
	}

	[Fact]
	public void Validate_WithRuleViolation_IsInvalidAndThrows()
	{
		var target = CreateTarget(value: -1);

		target.Validate();

		Assert.False(target.IsValid);
		Assert.NotEmpty(target.Errors);
		Assert.Throws<ValidationException>(() => Validator.Validate(target));
	}

	/// <summary>
	/// 没有任何规则时始终视为合法（该分支此前已正确，一并锁定以防回归）。
	/// </summary>
	[Fact]
	public void Validate_WithoutRules_IsValid()
	{
		var target = new ValidatableObject<int>();

		target.Validate();

		Assert.True(target.IsValid);
	}

	private static ValidatableObject<int> CreateTarget(int value)
	{
		var target = new ValidatableObject<int> { Value = value };
		target.UseValidator(new PositiveRule());
		return target;
	}

	/// <summary>
	/// 要求值必须为正数的校验规则。
	/// </summary>
	private sealed class PositiveRule : IObjectValidator<int>
	{
		public string Message { get; set; } = "Value must be positive.";

		public bool Validate(int value)
		{
			return value > 0;
		}
	}
}
