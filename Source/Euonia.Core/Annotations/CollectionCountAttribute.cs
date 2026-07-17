using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// 用于验证集合属性中元素数量的特性。
/// 确保集合至少包含 <see cref="MinimumCount"/> 个元素，
/// 并可选择性地限制不超过 <see cref="MaximumCount"/> 个元素。
/// 当 <see cref="AllowNull"/> 为 true 时，null 值将被视为有效。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class CollectionCountAttribute : ValidationAttribute
{
	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小元素数量。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	public CollectionCountAttribute(int minimumCount)
	{
		MinimumCount = minimumCount;
	}

	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小和最大元素数量。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	/// <param name="maximumCount">集合中允许的最大元素数量。</param>
	public CollectionCountAttribute(int minimumCount, int maximumCount)
	{
		MinimumCount = minimumCount;
		MaximumCount = maximumCount;
	}

	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小元素数量，并使用错误消息访问器。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	/// <param name="errorMessageAccessor">返回错误消息的函数。</param>
	public CollectionCountAttribute(int minimumCount, Func<string> errorMessageAccessor)
		: base(errorMessageAccessor)
	{
		MinimumCount = minimumCount;
	}

	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小和最大元素数量，并使用错误消息访问器。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	/// <param name="maximumCount">集合中允许的最大元素数量。</param>
	/// <param name="errorMessageAccessor">返回错误消息的函数。</param>
	public CollectionCountAttribute(int minimumCount, int maximumCount, Func<string> errorMessageAccessor)
		: base(errorMessageAccessor)
	{
		MinimumCount = minimumCount;
		MaximumCount = maximumCount;
	}

	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小元素数量，并使用静态错误消息。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	/// <param name="errorMessage">验证失败时使用的错误消息。</param>
	public CollectionCountAttribute(int minimumCount, string errorMessage)
		: base(errorMessage)
	{
		MinimumCount = minimumCount;
	}

	/// <summary>
	/// 初始化 <see cref="CollectionCountAttribute"/> 类的新实例，
	/// 强制要求最小和最大元素数量，并使用静态错误消息。
	/// </summary>
	/// <param name="minimumCount">集合中所需的最小元素数量。</param>
	/// <param name="maximumCount">集合中允许的最大元素数量。</param>
	/// <param name="errorMessage">验证失败时使用的错误消息。</param>
	public CollectionCountAttribute(int minimumCount, int maximumCount, string errorMessage)
		: base(errorMessage)
	{
		MinimumCount = minimumCount;
		MaximumCount = maximumCount;
	}

	/// <summary>
	/// 获取集合中所需的最小元素数量。
	/// 默认值为 0。
	/// </summary>
	public int MinimumCount { get; }

	/// <summary>
	/// 获取或设置集合中允许的可选最大元素数量。
	/// 为 null 时，不强制上限。
	/// </summary>
	public int? MaximumCount { get; }

	/// <summary>
	/// 获取或设置一个值，指示 null 集合是否被视为有效。
	/// 默认值为 true。
	/// </summary>
	public bool AllowNull { get; set; } = true;

	/// <summary>
	/// 根据配置的最小/最大数量验证指定的值。
	/// 当值有效时返回 <see cref="ValidationResult.Success"/>；
	/// 否则返回包含格式化错误消息的 <see cref="ValidationResult"/>。
	/// 行为说明：
	/// - 如果值为 null 且 <see cref="AllowNull"/> 为 true，则验证成功。
	/// - 如果值为 null 且 <see cref="AllowNull"/> 为 false，则返回验证错误。
	/// - 如果值实现了 <c>ICollection</c> 且其 <c>Count</c> 小于
	///   <see cref="MinimumCount"/>，则返回验证错误。
	/// - 如果值实现了 <c>ICollection</c> 且 <see cref="MaximumCount"/> 有值
	///   并且 <c>Count</c> 大于 <see cref="MaximumCount"/>，则返回验证错误。
	/// </summary>
	/// <param name="value">要验证的属性值（预期为集合）。</param>
	/// <param name="validationContext">验证操作的上下文信息。</param>
	/// <returns>指示成功或失败的 <see cref="ValidationResult"/>。</returns>
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		return value switch
		{
			null when AllowNull => ValidationResult.Success,
			null => new ValidationResult(ErrorMessage ?? $"The collection must not be null."),
			ICollection collection when collection.Count < MinimumCount =>
				new ValidationResult(FormatErrorMessage(ErrorMessage ?? $"The {0} must contain at least {MinimumCount} items.", validationContext.DisplayName), [validationContext.MemberName]),
			ICollection collection when MaximumCount.HasValue && collection.Count > MaximumCount.Value =>
				new ValidationResult(FormatErrorMessage(ErrorMessage ?? $"The {0} must contain at most {MaximumCount.Value} items.", validationContext.DisplayName), [validationContext.MemberName]),
			_ => ValidationResult.Success
		};
	}

	/// <summary>
	/// 使用当前区域性和提供的显示名称格式化错误消息。
	/// 消息应包含用于显示名称的单个格式占位符 ({0})。
	/// </summary>
	/// <param name="message">要格式化的消息模板。</param>
	/// <param name="displayName">已验证成员的显示名称。</param>
	/// <returns>格式化后的错误消息。</returns>
	private static string FormatErrorMessage(string message, string displayName)
	{
		return string.Format(CultureInfo.CurrentCulture, message, displayName);
	}
}