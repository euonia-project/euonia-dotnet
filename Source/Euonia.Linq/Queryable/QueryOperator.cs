namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 枚举查询运算符。
/// </summary>
public enum QueryOperator
{
    /// <summary>
    /// 表示等于（=）指定值。
    /// </summary>
    Equal,

    /// <summary>
    /// 表示以指定值开头（LIKE value%）。
    /// </summary>
    StartsWith,

    /// <summary>
    /// 表示以指定值结尾（LIKE %value）。
    /// </summary>
    EndsWith,

    /// <summary>
    /// 表示包含指定值（LIKE %value%）。
    /// </summary>
    Contains,

    /// <summary>
    /// 表示不包含指定值（NOT LIKE %value%）。
    /// </summary>
    NotContains,

    /// <summary>
    /// 表示模糊匹配指定值（LIKE value）。
    /// </summary>
    Like,

    /// <summary>
    /// 表示不模糊匹配指定值（NOT LIKE value）。
    /// </summary>
    NotLike,

    /// <summary>
    /// 表示判断是否为指定值（IS value）。
    /// </summary>
    Is,

    /// <summary>
    /// 表示不等于（!=）指定值。
    /// </summary>
    NotEqual,

    /// <summary>
    /// 表示大于（&gt;）指定值。
    /// </summary>
    GreaterThan,

    /// <summary>
    /// 表示大于或等于（&gt;=）指定值。
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// 表示小于（&lt;）指定值。
    /// </summary>
    LessThan,

    /// <summary>
    /// 表示小于或等于（&lt;=）指定值。
    /// </summary>
    LessThanOrEqual
}
