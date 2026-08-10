namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示实现此接口的类具有规则检查功能。
/// </summary>
public interface IHasRuleCheck
{
    /// <summary>
    /// 获取一个值，指示对象当前是否有效。
    /// </summary>
    /// <returns>如果对象当前有效，则为 <c>True</c>；否则为 <c>False</c>。</returns>
    bool IsValid { get; }

    /// <summary>
    /// 指示某个规则已完成。
    /// </summary>
    /// <param name="property">规则所针对的属性信息。</param>
    void RuleCheckComplete(IPropertyInfo property);

    /// <summary>
    /// 指示某个规则已完成。
    /// </summary>
    /// <param name="property">规则所针对的属性名称。</param>
    void RuleCheckComplete(string property);

    /// <summary>
    /// 指示所有规则均已完成。
    /// </summary>
    void AllRulesComplete();

	/// <summary>
	/// 挂起规则检查。
	/// </summary>
    void SuspendRuleChecking();

	/// <summary>
	/// 恢复规则检查。
	/// </summary>
    void ResumeRuleChecking();

    /// <summary>
    /// 获取此对象的违规规则集合。
    /// </summary>
    /// <returns>违规规则集合。</returns>
    BrokenRuleCollection GetBrokenRules();
}