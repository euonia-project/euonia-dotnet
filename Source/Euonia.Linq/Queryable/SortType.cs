namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 表示排序查询中的排序顺序。
/// </summary>
public enum SortType
{
    /// <summary>
    /// 表示未指定排序方式。
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// 表示升序排序。
    /// </summary>
    Ascending = -1,

    /// <summary>
    /// 表示降序排序。
    /// </summary>
    Descending = 1
}
