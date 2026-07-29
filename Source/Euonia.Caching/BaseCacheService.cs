namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Provides a base implementation for cache service classes that manage cache operations with key prefix support.
/// </summary>
/// <remarks>
/// This abstract class serves as a foundation for cache service implementations, providing key rewriting functionality
/// to ensure consistent key naming conventions across cache operations. Derived classes must implement the
/// <see cref="GetCacheManager{T}"/> method to provide specific cache manager instances.
/// </remarks>
public abstract class BaseCacheService
{
	/// <summary>
	/// Gets or sets the prefix that will be prepended to all cache keys.
	/// </summary>
	/// <value>
	/// A string representing the key prefix. If <see langword="null"/> or empty, no prefix is applied to cache keys.
	/// </value>
	public virtual string KeyPrefix { get; protected set; }

	/// <summary>
	/// Gets the cache manager instance for the specified type.
	/// </summary>
	/// <typeparam name="TValue">The type of data to be cached.</typeparam>
	/// <returns>An <see cref="ICacheManager{T}"/> instance for managing cached items of type <typeparamref name="TValue"/>.</returns>
	protected abstract ICacheManager<TValue> GetCacheManager<TValue>();

	/// <summary>
	/// Rewrites the specified cache key by prepending the configured key prefix.
	/// </summary>
	/// <param name="key">The original cache key to be rewritten.</param>
	/// <returns>
	/// The rewritten cache key with the format "{KeyPrefix}.Cache.{key}" if <see cref="KeyPrefix"/> is configured
	/// and the key doesn't already start with this pattern; otherwise, returns the original key unchanged.
	/// </returns>
	/// <remarks>
	/// This method ensures that all cache keys follow a consistent naming convention. If the key already contains
	/// the expected prefix pattern, it is returned as-is to prevent duplicate prefixing. The comparison is
	/// case-insensitive.
	/// </remarks>
	protected virtual string RewriteKey(string key)
	{
		if (string.IsNullOrEmpty(KeyPrefix))
		{
			return key;
		}

		return key.StartsWith($"{KeyPrefix}.Cache.", StringComparison.OrdinalIgnoreCase) ? key : $"{KeyPrefix}.Cache.{key}";
	}

	/// <summary>
	/// Creates a <see cref="CacheItem{TValue}"/> instance with the specified key, value, and optional timeout.
	/// </summary>
	/// <typeparam name="TValue">The type of value to be cached.</typeparam>
	/// <param name="key">The cache key identifier for the item.</param>
	/// <param name="value">The value to be stored in the cache.</param>
	/// <param name="timeout">
	/// An optional <see cref="TimeSpan"/> specifying the absolute expiration time for the cache item.
	/// If <see langword="null"/> or less than or equal to <see cref="TimeSpan.Zero"/>, the item is created without expiration.
	/// </param>
	/// <returns>
	/// A <see cref="CacheItem{TValue}"/> configured with absolute expiration if <paramref name="timeout"/> is greater than
	/// <see cref="TimeSpan.Zero"/>; otherwise, a cache item without expiration settings.
	/// </returns>
	/// <remarks>
	/// This helper method standardizes the creation of cache items with consistent expiration behavior.
	/// When a positive timeout is provided, the cache item uses <see cref="CacheExpirationMode.Absolute"/> expiration mode.
	/// </remarks>
	protected virtual CacheItem<TValue> GetCacheItem<TValue>(string key, TValue value, TimeSpan? timeout)
	{
		CacheItem<TValue> item;
		if (timeout > TimeSpan.Zero)
		{
			item = new CacheItem<TValue>(key, value, CacheExpirationMode.Absolute, timeout.Value);
		}
		else
		{
			item = new CacheItem<TValue>(key, value);
		}

		return item;
	}
}
