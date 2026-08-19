namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表达式条件运算类型。
/// </summary>
public enum PredicateOperator
{
    /// <summary>
    /// 表示条件 AND 运算：仅当第一个操作数计算结果为 true 时才计算第二个操作数。
    /// </summary>
    AndAlso,

    /// <summary>
    /// 表示条件 OR 运算：仅当第一个操作数计算结果为 false 时才计算第二个操作数。
    /// </summary>
    OrElse
}
