namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 仅公开 <see cref="CacheItem{T}"/> 的属性（不含 T 值）的契约。
/// </summary>
public interface ICacheItemProperties
{
    /// <summary>
    /// 获取缓存项的创建日期。
    /// </summary>
    /// <value>创建日期。</value>
    DateTime CreatedUtc { get; }

    /// <summary>
    /// 获取过期模式。
    /// </summary>
    /// <value>过期模式。</value>
    CacheExpirationMode ExpirationMode { get; }

    /// <summary>
    /// 获取过期时间。
    /// </summary>
    /// <value>过期时间。</value>
    TimeSpan ExpirationTimeout { get; }

    /// <summary>
    /// 获取缓存键。
    /// </summary>
    /// <value>缓存键。</value>
    string Key { get; }

    /// <summary>
    /// 获取或设置缓存项的最后访问日期。
    /// </summary>
    /// <value>最后访问日期。</value>
    DateTime LastAccessedUtc { get; set; }

    /// <summary>
    /// 获取缓存区域。
    /// </summary>
    /// <value>缓存区域。</value>
    string Region { get; }

    /// <summary>
    /// 获取一个值，指示缓存项是否使用缓存句柄配置的过期时间。
    /// </summary>
    bool UsesExpirationDefaults { get; }

    /// <summary>
    /// 获取缓存值的类型。
    /// <para>此类型可能用于序列化和反序列化。</para>
    /// </summary>
    /// <value>缓存值的类型。</value>
    Type ValueType { get; }
}