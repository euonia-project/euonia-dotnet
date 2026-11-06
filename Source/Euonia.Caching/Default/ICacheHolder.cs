namespace Nerosoft.Euonia.Caching;

/// <summary>
/// ICacheHolder
/// </summary>
public interface ICacheHolder
{
    /// <summary>
    /// Gets a Cache entry from the cache. If none is found, an empty one is created and returned.
    /// </summary>
    /// <typeparam name="TKey">The type of the key within the component.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="component">The component context.</param>
    /// <returns>An entry from the cache, or a new, empty one, if none is found.</returns>
    internal CacheItem<TKey, TResult> GetCache<TKey, TResult>(Type component) where TKey : notnull;
}