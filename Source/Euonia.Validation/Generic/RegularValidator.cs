using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// RegularValidator 类。
/// 实现 <see cref="AbstractValidator{String}" />
/// </summary>
/// <seealso cref="AbstractValidator{String}" />
public class RegularValidator : AbstractValidator<string>
{
    /// <summary>
    /// 初始化 <see cref="RegularValidator"/> 类的新实例。
    /// </summary>
    /// <param name="pattern">模式。</param>
    /// <param name="message">错误消息。</param>
    public RegularValidator(string pattern, string message)
        : base(message)
    {
        Pattern = pattern;
    }

    /// <summary>
    /// 获取模式。
    /// </summary>
    /// <value>模式。</value>
    public string Pattern { get; }

    /// <summary>
    /// 验证指定的值。
    /// </summary>
    /// <param name="value">要验证的值。</param>
    /// <returns>如果值有效则为 <c>true</c>，否则为 <c>false</c>。</returns>
    public override bool Validate(string value)
    {
        return Regex.IsMatch(value, Pattern);
    }
}
