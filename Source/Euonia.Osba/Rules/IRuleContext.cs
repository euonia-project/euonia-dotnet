namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务规则被调用时提供的上下文信息。
/// </summary>
public interface IRuleContext
{
    /// <summary>
    /// 获取规则对象。
    /// </summary>
    IRuleBase Rule { get; }

    /// <summary>
    /// 获取目标业务对象的引用。
    /// </summary>
    object Target { get; }

    /// <summary>
    /// 获取规则检查结果。
    /// </summary>
    IReadOnlyList<RuleResult> Results { get; }

    /// <summary>
    /// 向规则上下文添加一个错误结果。
    /// </summary>
    /// <param name="description">错误描述。</param>
    void AddErrorResult(string description);

    /// <summary>
    /// 向规则上下文添加一个警告结果。
    /// </summary>
    /// <param name="description">警告描述。</param>
    void AddWarningResult(string description);

    /// <summary>
    /// 向规则上下文添加一个信息结果。
    /// </summary>
    /// <param name="description">信息描述。</param>
    void AddInformationResult(string description);

    /// <summary>
    /// 向规则上下文添加一个成功结果。
    /// </summary>
    void AddSuccessResult();

    /// <summary>
    /// 完成规则上下文。
    /// </summary>
    void Complete();
}