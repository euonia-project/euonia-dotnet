using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Provides the default implementation for a cache manager.
/// The cache manager provides an abstraction over the cache holder allowing it to be easily swaped and isolating it within a component context.
/// Implements the <see cref="ICacheManager" />
/// </summary>
/// <seealso cref="ICacheManager" />
public class DefaultCacheManager : ICacheManager
{
    /// <summary>
    /// The component
    /// </summary>
    private readonly Type _component;

    /// <summary>
    /// The cache holder
    /// </summary>
    private readonly ICacheHolder _holder;

    /// <summary>
    /// Constructs a new cache manager for a given component type and with a specific cache holder implementation.
    /// </summary>
    /// <param name="component">The component to which the cache applies (context).</param>
    /// <param name="holder">The cache holder that contains the entities cached.</param>
    public DefaultCacheManager(Type component, ICacheHolder holder)
    {
        _component = component;
        _holder = holder;
    }

    /// <summary>
    /// Gets a cache entry from the cache holder.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to be used to fetch the cache entry.</typeparam>
    /// <typeparam name="TResult">The type of the entry to be obtained from the cache.</typeparam>
    /// <returns>The entry from the cache.</returns>
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
    /// Gets the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="acquire">The acquire.</param>
    /// <returns>TResult.</returns>
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
    /// Try gets the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="result">The cached value of key.</param>
    /// <returns></returns>
    public bool TryGet<TKey, TResult>(TKey key, [MaybeNullWhen(false)] out TResult result) where TKey : notnull
    {
        return GetCache<TKey, TResult>().TryGet(key, out result);
    }
}

/// <summary>
/// Provides the default implementation for a cache manager.
/// The cache manager provides an abstraction over the cache holder allowing it to be easily swaped and isolating it within a component context.
/// Implements the <see cref="DefaultCacheManager" />
/// </summary>
/// <typeparam name="TComponent">The type of the component to which the cache applies (context)..</typeparam>
/// <seealso cref="DefaultCacheManager" />
/// <seealso cref="DefaultCacheManager" />
public class DefaultCacheManager<TComponent> : DefaultCacheManager
{
    /// <summary>
    /// Constructs a new cache manager for a given component type and with a specific cache holder implementation.
    /// </summary>
    /// <param name="holder">The cache holder that contains the entities cached.</param>
    public DefaultCacheManager(ICacheHolder holder)
        : base(typeof(TComponent), holder)
    {
    }
}