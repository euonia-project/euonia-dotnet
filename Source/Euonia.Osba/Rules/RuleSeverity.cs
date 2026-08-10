namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 验证规则严重级别的值。
/// </summary>
public enum RuleSeverity
{
    /// <summary>
    /// 表示严重的业务规则违规，应导致对象被视为无效。
    /// </summary>
    Error,

    /// <summary>
    /// 表示应显示给用户的业务规则违规，但不应对对象判定为无效。
    /// </summary>
    Warning,

    /// <summary>
    /// 表示应显示给用户的业务规则结果，但严重级别低于警告。
    /// </summary>
    Information,

    /// <summary>
    /// 表示不应显示给用户的业务规则结果，且规则已成功。
    /// </summary>
    Success
}