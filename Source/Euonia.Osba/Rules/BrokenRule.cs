namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <para>Stores details about a specific broken rule.</para>
/// <para>保存中断性规则详情</para>
/// </summary>
public class BrokenRule
{
    /// <summary>
    /// <para>Gets or sets the property affected by the broken rule.</para>
    /// <para>获取或设置受规则影响的属性</para>
    /// </summary>
    public string Property { get; internal set; }

    /// <summary>
    /// <para>Gets or sets the description of <see cref="BrokenRule"/>.</para>
    /// <para>获取或设置<see cref="BrokenRule"/>的描述信息.</para>
    /// </summary>
    public string Description { get; internal set; }

    /// <summary>
    /// Gets the severity.
    /// </summary>
    public RuleSeverity Severity { get; internal set; }
}