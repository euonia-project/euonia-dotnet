namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 验证器接口。
/// </summary>
/// <typeparam name="TValue">值类型。</typeparam>
public interface IObjectValidator<in TValue>
{
    /// <summary>
    /// 获取或设置错误消息。
    /// </summary>
    /// <value>错误消息。</value>
    string Message { get; set; }

    /// <summary>
    /// 验证指定的值。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <returns>如果有效则为 <c>true</c>，否则为 <c>false</c>。</returns>
    bool Validate(TValue value);
}