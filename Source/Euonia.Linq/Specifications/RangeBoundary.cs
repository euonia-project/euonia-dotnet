namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 规约参数的边界指示。
/// </summary>
public enum RangeBoundary
{
    /// <summary>
    /// 仅包含左侧边界。
    /// </summary>
    Left,

    /// <summary>
    /// 仅包含右侧边界。
    /// </summary>
    Right,

    /// <summary>
    /// 包含左右两侧边界。
    /// </summary>
    Both,

    /// <summary>
    /// 左右两侧边界均不包含。
    /// </summary>
    Neither
}