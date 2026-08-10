namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 该来源枚举指示缓存事件是本地触发的还是通过背板触发的。
/// </summary>
public enum CacheActionEventArgOrigin
{
    /// <summary>
    /// 本地触发的操作。
    /// </summary>
    Local,

    /// <summary>
    /// 通过背板远程触发的操作。
    /// </summary>
    Remote
}

/// <summary>
/// 指示缓存项被移除时原因的标志。
/// </summary>
public enum CacheItemRemovedReason
{
    /// <summary>
    /// 由于过期而移除了 <see cref="CacheItem{T}"/>。
    /// </summary>
    Expired = 0,

    /// <summary>
    /// 由于底层缓存决定移除而移除了 <see cref="CacheItem{T}"/>。
    /// 例如，当达到缓存特定的内存限制时可能发生这种情况。
    /// </summary>
    Evicted = 1,

    /// <summary>
    /// 未使用 CacheManager API（例如通过 redis-cli 使用 del）手动移除了 <see cref="CacheItem{T}"/>。
    /// </summary>
    /// <remarks>
    /// 这将最终为负责的缓存层触发 <see cref="ICacheManager{TCacheValue}.OnRemoveByHandle"/>，并在项被移除后触发
    /// <see cref="ICacheManager{TCacheValue}.OnRemove"/>。
    /// </remarks>
    ExternalDelete = 99
}

/// <summary>
/// 缓存操作的事件参数。
/// </summary>
public sealed class CacheItemRemovedEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheItemRemovedEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <param name="reason">原因。</param>
    /// <param name="value">被移除的原始缓存值。根据缓存子系统的不同，可能为 <c>null</c>。</param>
    /// <param name="level">触发事件的缓存层级。</param>
    /// <exception cref="ArgumentNullException">当 <c>key</c> 为 <c>null</c> 时抛出。</exception>
    public CacheItemRemovedEventArgs(string key, string region, CacheItemRemovedReason reason, object value, int level = 0)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

        Reason = reason;
        Key = key;
        Region = region;
        Level = level;
        Value = value;
    }

    /// <summary>
    /// 获取键。
    /// </summary>
    /// <value>键。</value>
    public string Key { get; }

    /// <summary>
    /// 获取区域。
    /// </summary>
    /// <value>区域。</value>
    public string Region { get; }

    /// <summary>
    /// 获取指示 <see cref="CacheItem{T}"/> 被移除原因详情的标志。
    /// </summary>
    public CacheItemRemovedReason Reason { get; }

    /// <summary>
    /// 获取指示触发事件的缓存层级的值。
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// 获取由此事件移除的原始缓存值。
    /// <para>
    /// 如果底层缓存系统不支持在逐出时返回值（例如 Redis），
    /// 此属性可能返回 <c>Null</c>。
    /// </para>
    /// </summary>
    public object Value { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CacheItemRemovedEventArgs {Region}:{Key} - {Reason} {Level}";
    }
}

/// <summary>
/// 缓存操作的事件参数。
/// </summary>
public sealed class CacheActionEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheActionEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <exception cref="ArgumentNullException">当 <c>key</c> 为 <c>null</c> 时抛出。</exception>
    public CacheActionEventArgs(string key, string region)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

        Key = key;
        Region = region;
    }

    /// <summary>
    /// 初始化 <see cref="CacheActionEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <param name="origin">事件发生的来源。如果为远程，则事件由背板触发，并非实际在本地执行。</param>
    /// <exception cref="ArgumentNullException">当 <c>key</c> 为 <c>null</c> 时抛出。</exception>
    public CacheActionEventArgs(string key, string region, CacheActionEventArgOrigin origin)
        : this(key, region)
    {
        Origin = origin;
    }

    /// <summary>
    /// 获取键。
    /// </summary>
    /// <value>键。</value>
    public string Key { get; }

    /// <summary>
    /// 获取区域。
    /// </summary>
    /// <value>区域。</value>
    public string Region { get; }

    /// <summary>
    /// 获取指示事件是由本地操作触发还是通过背板远程触发的事件来源。
    /// </summary>
    public CacheActionEventArgOrigin Origin { get; } = CacheActionEventArgOrigin.Local;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CacheActionEventArgs {Region}:{Key} - {Origin}";
    }
}

/// <summary>
/// 缓存清空事件的事件参数。
/// </summary>
public sealed class CacheClearEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheClearEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="origin">事件发生的来源。如果为远程，则事件由背板触发，并非实际在本地执行。</param>
    public CacheClearEventArgs(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
    {
        Origin = origin;
    }

    /// <summary>
    /// 获取指示事件是由本地操作触发还是通过背板远程触发的事件来源。
    /// </summary>
    public CacheActionEventArgOrigin Origin { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CacheClearEventArgs {Origin}";
    }
}

/// <summary>
/// 清空区域事件的事件参数。
/// </summary>
public sealed class CacheClearRegionEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheClearRegionEventArgs"/> 类的新实例。
    /// </summary>
    /// <param name="region">区域。</param>
    /// <param name="origin">事件发生的来源。如果为远程，则事件由背板触发，并非实际在本地执行。</param>
    /// <exception cref="ArgumentNullException">当 <c>region</c> 为 <c>null</c> 时抛出。</exception>
    public CacheClearRegionEventArgs(string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
    {
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

        Region = region;
        Origin = origin;
    }

    /// <summary>
    /// 获取区域。
    /// </summary>
    /// <value>区域。</value>
    public string Region { get; }

    /// <summary>
    /// 获取指示事件是由本地操作触发还是通过背板远程触发的事件来源。
    /// </summary>
    public CacheActionEventArgOrigin Origin { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CacheClearRegionEventArgs {Region} - {Origin}";
    }
}