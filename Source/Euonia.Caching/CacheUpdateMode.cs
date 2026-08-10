namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义缓存管理器可能的更新模式。
/// <para>
/// 更新模式作用于 Get 操作。如果缓存管理器在一个缓存句柄中找到缓存项，而其他缓存句柄没有该项，
/// 则根据模式决定是否将该项添加到其他缓存句柄中。
/// </para>
/// </summary>
public enum CacheUpdateMode
{
    /// <summary>
    /// 指示缓存管理器不要将缓存项与其他缓存句柄同步（例如在 <see cref="ICache{TCacheValue}.Get(string)"/> 中）。
    /// </summary>
    None,

    /// <summary>
    /// 指示缓存管理器将缓存项与缓存句柄列表中位于其上方的其他缓存句柄同步。
    /// 缓存句柄的顺序由配置定义。
    /// </summary>
    /// <remarks>
    /// 这是默认行为，仅在确实需要时才应关闭。
    /// </remarks>
    Up
}