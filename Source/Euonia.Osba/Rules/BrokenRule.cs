namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 存储关于特定违规规则的详细信息。
/// </summary>
public class BrokenRule
{
    /// <summary>
    /// 获取或设置受规则影响的属性。
    /// </summary>
    public string Property { get; internal set; }

    /// <summary>
    /// 获取或设置 <see cref="BrokenRule"/> 的描述信息。
    /// </summary>
    public string Description { get; internal set; }

    /// <summary>
    /// 获取严重级别。
    /// </summary>
    public RuleSeverity Severity { get; internal set; }
}