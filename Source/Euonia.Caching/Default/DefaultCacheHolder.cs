using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供缓存持有器的默认实现。
/// 缓存持有器负责实际存储对已缓存实体的引用。
/// 实现 <see cref="ICacheHolder" />。
/// </summary>
/// <seealso cref="ICacheHolder" />
public class DefaultCacheHolder : ICacheHolder
{
    /// <summary>
    /// 缓存上下文访问器。
    /// </summary>
    private readonly ICacheContextAccessor _accessor;

    /// <summary>
    /// 缓存集合。
    /// </summary>
    private readonly ConcurrentDictionary<CacheKey, object> _caches = new();

    /// <summary>
    /// 初始化 <see cref="DefaultCacheHolder"/> 类的新实例。
    /// </summary>
    /// <param name="accessor">缓存上下文访问器。</param>
    public DefaultCacheHolder(ICacheContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    CacheItem<TKey, TResult> ICacheHolder.GetCache<TKey, TResult>(Type component)
    {
        var key = new CacheKey(component, typeof(TKey), typeof(TResult));
        var result = _caches.GetOrAdd(key, _ => new CacheItem<TKey, TResult>(_accessor));
        return (CacheItem<TKey, TResult>)result;
    }
}