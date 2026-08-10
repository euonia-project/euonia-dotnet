namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义缓存项支持的过期模式。
/// <para>值 <c>None</c> 表示不设置过期时间。</para>
/// </summary>
public enum CacheExpirationMode
{
    /// <summary>
    /// 过期模式枚举的默认值。
    /// 缓存管理器将默认使用 <c>None</c>。枚举中的 <code>Default</code> 条目用于与其他值区分，
    /// 并允许显式将过期设置为 <c>None</c>。
    /// </summary>
    Default = 0,

    /// <summary>
    /// 定义无过期时间。
    /// </summary>
    None = 1,

    /// <summary>
    /// 定义滑动过期。每次访问都会刷新过期时间。
    /// </summary>
    Sliding = 2,

    /// <summary>
    /// 定义绝对过期。缓存项将在过期时间之后失效。
    /// </summary>
    Absolute = 3
}