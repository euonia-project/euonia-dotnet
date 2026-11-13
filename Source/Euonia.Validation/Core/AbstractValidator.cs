namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 抽象验证器类，实现 <see cref="IObjectValidator{TValue}" />
/// </summary>
/// <typeparam name="TValue">值类型。</typeparam>
/// <seealso cref="IObjectValidator{TValue}" />
public abstract class AbstractValidator<TValue> : IObjectValidator<TValue>
{
    /// <summary>
    /// 获取或设置错误消息。
    /// </summary>
    /// <value>错误消息。</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="AbstractValidator{TValue}"/> 类的新实例。
    /// </summary>
    protected AbstractValidator()
    {
    }

    /// <summary>
    /// 初始化 <see cref="AbstractValidator{TValue}"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息。</param>
    protected AbstractValidator(string message)
        : this()
    {
        Message = message;
    }

    /// <summary>
    /// 验证指定的值。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <returns>如果有效则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public abstract bool Validate(TValue value);
}