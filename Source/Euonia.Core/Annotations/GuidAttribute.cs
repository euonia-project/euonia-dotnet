namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// 验证某个值是否表示有效的 GUID 字符串。
/// 可应用于属性、字段或参数。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class GuidAttribute : ValidationAttribute
{
	/// <summary>
	/// 确定指定的值是否为有效的 GUID。
	/// 验证规则：
	/// <para>- null 被视为有效（使用 [Required] 来禁止 null）。</para>
	/// <para>- 可被 <see cref="Guid.TryParse(string, out Guid)"/> 解析的字符串有效。</para>
	/// <para>- 其他所有值均无效。</para>
	/// </summary>
	/// <param name="value">正在验证的成员的值。</param>
	/// <param name="validationContext">验证操作的上下文信息。</param>
	/// <returns>
	/// 当值有效时返回 <see cref="ValidationResult.Success"/>；否则返回包含
	/// 配置的 <see cref="ValidationAttribute.ErrorMessage"/>（或默认消息）和成员名称的 <see cref="ValidationResult"/>。
	/// </returns>
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		return value switch
		{
			null => ValidationResult.Success,
			string str when Guid.TryParse(str, out _) => ValidationResult.Success,
			_ => new ValidationResult(
				ErrorMessage ?? $"{validationContext.MemberName} must be a valid GUID.",
				[validationContext.MemberName])
		};
	}
}