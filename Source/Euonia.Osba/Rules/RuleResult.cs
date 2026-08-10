namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 包含规则结果的信息。
/// </summary>
public class RuleResult
{
    /// <summary>
    /// 初始化 <see cref="RuleResult"/> 类的新实例。
    /// </summary>
    /// <param name="ruleName">规则名称。</param>
    public RuleResult(string ruleName)
    {
        RuleName = ruleName;
        Success = true;
    }

    /// <summary>
    /// 初始化 <see cref="RuleResult"/> 类的新实例。
    /// </summary>
    /// <param name="ruleName">规则名称。</param>
    /// <param name="description">规则失败的描述。</param>
    /// <param name="severity">规则的严重级别。</param>
    public RuleResult(string ruleName, string description, RuleSeverity severity)
    {
        RuleName = ruleName;
        Success = string.IsNullOrEmpty(description);
        Description = description;
        Severity = severity;
    }

    /// <summary>
    /// 获取一个值，指示规则是否成功。
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// 获取规则失败原因的人类可读描述。
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// 获取或设置失败规则的严重级别。
    /// </summary>
    public RuleSeverity Severity { get; set; }

    /// <summary>
    /// 获取规则的唯一名称。
    /// </summary>
    public string RuleName { get; private set; }

    /// <summary>
    /// 获取或设置受规则影响的属性列表。
    /// </summary>
    public IList<IPropertyInfo> Properties { get; set; }
}