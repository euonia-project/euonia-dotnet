using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Class CacheManagerExtensions.
/// </summary>
public static class CacheManagerExtensions
{
    /// <summary>
    /// The locks
    /// </summary>
    private static readonly ConcurrentDictionary<object, object> _locks = new();

    /// <summary>
    /// Gets cached value with the specified key.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult Get<TResult>(this ICacheManager manager, string key)
    {
        return manager.Get<string, TResult>(key);
    }

    /// <summary>
    /// Gets cached value with the specified key, or adds value to cache if not exists.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult GetOrAdd<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire)
    {
        return manager.GetOrAdd(key, acquire);
    }

    /// <summary>
    /// Adds or updates cached value with the specified key.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult AddOrUpdate<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire)
    {
        return manager.AddOrUpdate(key, acquire);
    }

    /// <summary>
    /// Gets the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    /// <param name="manager">The cache manager.</param>
    /// <param name="key">The key.</param>
    /// <param name="preventConcurrentCalls">if set to <c>true</c> [prevent concurrent calls].</param>
    /// <param name="acquire">The acquire.</param>
    /// <returns>TResult.</returns>
    public static TResult GetOrAdd<TKey, TResult>(this ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, bool preventConcurrentCalls) where TKey : notnull
    {
        if (!preventConcurrentCalls)
        {
            return manager.GetOrAdd(key, acquire);
        }

        var lockKey = _locks.GetOrAdd(key, _ => new object());
        lock (lockKey)
        {
            return manager.GetOrAdd(key, acquire);
        }
    }

    /// <summary>
    /// Adds or updates the specified key.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <param name="preventConcurrentCalls"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult AddOrUpdate<TKey, TResult>(this ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, bool preventConcurrentCalls) where TKey : notnull
    {
        if (!preventConcurrentCalls)
        {
            return manager.AddOrUpdate(key, acquire);
        }

        var lockKey = _locks.GetOrAdd(key, _ => new object());
        lock (lockKey)
        {
            return manager.AddOrUpdate(key, acquire);
        }
    }

    /// <summary>
    /// Gets cached value with the specified key, or adds value to cache if not exists.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <param name="preventConcurrentCalls"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult GetOrAdd<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire, bool preventConcurrentCalls)
    {
        return manager.GetOrAdd<string, TResult>(key, acquire, preventConcurrentCalls);
    }

    /// <summary>
    /// Adds or updates cached value with the specified key.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <param name="preventConcurrentCalls"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult AddOrUpdate<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire, bool preventConcurrentCalls)
    {
        return manager.AddOrUpdate<string, TResult>(key, acquire, preventConcurrentCalls);
    }
}