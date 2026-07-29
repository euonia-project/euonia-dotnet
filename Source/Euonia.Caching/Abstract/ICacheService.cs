// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// The base service contract of Caching.
/// </summary>
public interface ICacheService
{
	/// <summary>
	/// Get cached value with specified key.
	/// </summary>
	/// <param name="key">Cache item key.</param>
	/// <typeparam name="TValue">Type of value.</typeparam>
	/// <returns></returns>
	TValue Get<TValue>(string key);

	/// <summary>
	/// Try get cached value with specified key.
	/// </summary>
	/// <param name="key">The cache item key to retrieve.</param>
	/// <param name="value">The cached value if found; otherwise, the default value.</param>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <returns><c>true</c> if the key was found in the cache; otherwise, <c>false</c>.</returns>
	bool TryGet<TValue>(string key, out TValue value);

	/// <summary>
	/// Get exist value or add new while the specified key not exists.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The factory function to create the value if the key does not exist.</param>
	/// <param name="timeout">The optional timeout duration for the cache entry.</param>
	/// <returns>The cached value or the newly created value from the factory.</returns>
	TValue GetOrAdd<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null);

	/// <summary>
	/// Get exist value or add new while the specified key not exists with absolute expiration time.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The factory function to create the value if the key does not exist.</param>
	/// <param name="timeout">The absolute expiration time for the cache entry.</param>
	/// <param name="isUtcTime">Indicates whether the timeout is in UTC time; default is <c>true</c>.</param>
	/// <returns>The cached value or the newly created value from the factory.</returns>
	TValue GetOrAdd<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// Add or update cached value of specified key using a factory function.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The factory function to create or update the value.</param>
	/// <param name="timeout">The optional timeout duration for the cache entry.</param>
	/// <returns>The newly created or updated cached value.</returns>
	TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null);

	/// <summary>
	/// Add or update cached value of specified key using a factory function with absolute expiration time.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The factory function to create or update the value.</param>
	/// <param name="timeout">The absolute expiration time for the cache entry.</param>
	/// <param name="isUtcTime">Indicates whether the timeout is in UTC time; default is <c>true</c>.</param>
	/// <returns>The newly created or updated cached value.</returns>
	TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// Add or update cached value of specified key with a direct value.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="value">The value to cache.</param>
	/// <param name="timeout">The optional timeout duration for the cache entry.</param>
	/// <returns>The cached value.</returns>
	TValue AddOrUpdate<TValue>(string key, TValue value, TimeSpan? timeout = null);

	/// <summary>
	/// Add or update cached value of specified key with a direct value and absolute expiration time.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="value">The value to cache.</param>
	/// <param name="timeout">The absolute expiration time for the cache entry.</param>
	/// <param name="isUtcTime">Indicates whether the timeout is in UTC time; default is <c>true</c>.</param>
	/// <returns>The cached value.</returns>
	TValue AddOrUpdate<TValue>(string key, TValue value, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// Add or update cached value using a <see cref="CacheItem{TValue}"/> instance.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="item">The cache item containing the key, value, and expiration settings.</param>
	/// <returns>The cached value from the item.</returns>
	TValue AddOrUpdate<TValue>(CacheItem<TValue> item);

	/// <summary>
	/// Remove cached value of specified key.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key to remove.</param>
	/// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
	bool Remove<TValue>(string key);

	/// <summary>
	/// Try get cached value with specified key asynchronously.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key to retrieve.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>A tuple containing a boolean indicating success and the cached value if found.</returns>
	Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get exist value or add new while the specified key not exists asynchronously.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The asynchronous factory function to create the value if the key does not exist.</param>
	/// <param name="timeout">The optional timeout duration for the cache entry.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The cached value or the newly created value from the factory.</returns>
	Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get exist value or add new while the specified key not exists asynchronously with absolute expiration time.
	/// </summary>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The asynchronous factory function to create the value if the key does not exist.</param>
	/// <param name="timeout">The absolute expiration time for the cache entry.</param>
	/// <param name="isUtcTime">Indicates whether the timeout is in UTC time; default is <c>true</c>.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>The cached value or the newly created value from the factory.</returns>
	Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Add or update cached value of specified key asynchronously.
	/// </summary>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The asynchronous factory function to create or update the value.</param>
	/// <param name="timeout">The optional timeout duration for the cache entry.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <returns>The newly created or updated cached value.</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Add or update cached value of specified key asynchronously.
	/// </summary>
	/// <param name="key">The cache item key.</param>
	/// <param name="factory">The asynchronous factory function to create or update the value.</param>
	/// <param name="timeout">The absolute expiration time for the cache entry.</param>
	/// <param name="isUtcTime">Indicates whether the timeout is in UTC time; default is <c>true</c>.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <returns>The newly created or updated cached value.</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// Add or update cached value of specified key asynchronously.
	/// </summary>
	/// <param name="factory">The asynchronous factory function that returns a <see cref="CacheItem{TValue}"/> to cache.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <typeparam name="TValue">The type of the cached value.</typeparam>
	/// <returns>The newly created or updated cached value.</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(Func<Task<CacheItem<TValue>>> factory, CancellationToken cancellationToken = default);
}