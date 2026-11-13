using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供缓存管理器的默认实现。
/// 缓存管理器在缓存持有者之上提供了一层抽象，使其可以轻松替换，并可在组件上下文中隔离。
/// 实现 <see cref="ICacheManager" />
/// </summary>
/// <seealso cref="ICacheManager" />
public class DefaultCacheManager : ICacheManager
{
    /// <summary>
    /// 缓存所属的组件类型，用作缓存分区的键。
    /// </summary>
    private readonly Type _component;

    /// <summary>
    /// 缓存持有者
    /// </summary>
    private readonly ICacheHolder _holder;

    /// <summary>
    /// 为给定组件类型和特定的缓存持有者实现构造新的缓存管理器。
    /// </summary>
    /// <param name="component">缓存所适用的组件（上下文）。</param>
    /// <param name="holder">包含已缓存实体的缓存持有者。</param>
    public DefaultCacheManager(Type component, ICacheHolder holder)
    {
        _component = component;
        _holder = holder;
    }

    /// <summary>
    /// 从缓存持有者获取缓存条目。
    /// </summary>
    /// <typeparam name="TKey">用于获取缓存条目的键类型。</typeparam>
    /// <typeparam name="TResult">从缓存中获取的条目类型。</typeparam>
    /// <returns>来自缓存的条目。</returns>
    private CacheItem<TKey, TResult> GetCache<TKey, TResult>() where TKey : notnull
    {
        return _holder.GetCache<TKey, TResult>(_component);
    }

    /// <inheritdoc />
    public TResult Get<TKey, TResult>(TKey key) where TKey : notnull
    {
        return TryGet<TKey, TResult>(key, out var result) ? result : default!;
    }

    /// <summary>
    /// 获取指定的键。
    /// </summary>
    /// <typeparam name="TKey">键的类型。</typeparam>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    /// <param name="key">键。</param>
    /// <param name="acquire">获取方法。</param>
    /// <returns>TResult。</returns>
    public TResult GetOrAdd<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull
    {
        return GetCache<TKey, TResult>().GetOrAdd(key, acquire);
    }

    /// <inheritdoc />
    public TResult AddOrUpdate<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull
    {
        return GetCache<TKey, TResult>().AddOrUpdate(key, acquire);
    }

    /// <summary>
    /// 尝试获取指定的键。
    /// </summary>
    /// <typeparam name="TKey">键的类型。</typeparam>
    /// <typeparam name="TResult">结果的类型。</typeparam>
    /// <param name="key">键。</param>
    /// <param name="result">键对应的缓存值。</param>
    /// <returns>如果缓存中存在该键则为 <c>true</c>；否则为 <c>false</c>，此时 <paramref name="result"/> 为默认值。</returns>
    public bool TryGet<TKey, TResult>(TKey key, [MaybeNullWhen(false)] out TResult result) where TKey : notnull
    {
        return GetCache<TKey, TResult>().TryGet(key, out result);
    }
}

/// <summary>
/// 提供缓存管理器的默认实现。
/// 缓存管理器在缓存持有者之上提供了一层抽象，使其可以轻松替换，并可在组件上下文中隔离。
/// 实现 <see cref="DefaultCacheManager" />
/// </summary>
/// <typeparam name="TComponent">缓存所适用的组件（上下文）类型。</typeparam>
/// <seealso cref="DefaultCacheManager" />
/// <seealso cref="DefaultCacheManager" />
public class DefaultCacheManager<TComponent> : DefaultCacheManager
{
    /// <summary>
    /// 为给定组件类型和特定的缓存持有者实现构造新的缓存管理器。
    /// </summary>
    /// <param name="holder">包含已缓存实体的缓存持有者。</param>
    public DefaultCacheManager(ICacheHolder holder)
        : base(typeof(TComponent), holder)
    {
    }
}