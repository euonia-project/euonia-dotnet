namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 验证结果。
/// </summary>
[Serializable]
public class ValidationResult
{
    /// <summary>
    /// 初始化 <see cref="ValidationResult"/> 的新实例。
    /// </summary>
    public ValidationResult()
    {
    }

    /// <summary>
    /// 使用指定的属性名称与错误消息初始化 <see cref="ValidationResult"/> 的新实例。
    /// </summary>
    /// <param name="propertyName">验证失败的属性名称。</param>
    /// <param name="errorMessage">验证错误消息。</param>
    public ValidationResult(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 获取或设置验证失败的属性名称。
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置验证错误消息。
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
