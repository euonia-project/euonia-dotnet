using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// The implement of <see cref="ICacheService"/> with <see cref="System.Runtime.Caching"/>.
/// </summary>
public class RuntimeCacheService : BaseCacheService, ICacheService
{
	private readonly RuntimeCacheManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="RuntimeCacheService"/> class.
	/// </summary>
	/// <param name="options"></param>
	public RuntimeCacheService(IOptions<RuntimeCacheOptions> options)
	{
		_manager = new RuntimeCacheManager(options.Value);
		KeyPrefix = options.Value.KeyPrefix;
	}

	/// <inheritdoc />
	public TValue Get<TValue>(string key)
	{
		key = RewriteKey(key);
		return GetCacheManager<TValue>().Get(key);
	}

	/// <inheritdoc />
	public bool TryGet<TValue>(string key, out TValue value)
	{
		key = RewriteKey(key);
		var result = GetCacheManager<TValue>().TryGetOrAdd(key, _ => null, out var cache);
		value = result ? cache.Value : default;
		return result;
	}

	/// <inheritdoc />
	public TValue GetOrAdd<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null)
	{
		key = RewriteKey(key);
		var result = GetCacheManager<TValue>().GetOrAdd(key, _ =>
		{
			var value = factory();
			return GetCacheItem(key, value, timeout);
		});
		return result.Value;
	}

	/// <inheritdoc />
	public TValue GetOrAdd<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		var timespan = timeout - (isUtcTime ? DateTime.UtcNow : DateTime.Now);

		return GetOrAdd(key, factory, timespan);
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null)
	{
		var value = factory();
		return AddOrUpdate(key, value, timeout);
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		var timespan = timeout - (isUtcTime ? DateTime.UtcNow : DateTime.Now);
		return AddOrUpdate(key, factory, timespan);
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, TValue value, TimeSpan? timeout = null)
	{
		key = RewriteKey(key);
		var cacheItem = GetCacheItem(key, value, timeout);
		return GetCacheManager<TValue>().AddOrUpdate(cacheItem, _ => value);
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, TValue value, DateTime timeout, bool isUtcTime = true)
	{
		var timespan = timeout - (isUtcTime ? DateTime.UtcNow : DateTime.Now);

		return AddOrUpdate(key, value, timespan);
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(CacheItem<TValue> item)
	{
		RewriteKey(item.Key);

		return GetCacheManager<TValue>().AddOrUpdate(item, _ => item.Value);
	}

	/// <inheritdoc />
	public bool Remove<TValue>(string key)
	{
		key = RewriteKey(key);
		return GetCacheManager<TValue>().Remove(key);
	}

	/// <inheritdoc />
	public async Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default)
	{
		return await Task.Run(() =>
		{
			key = RewriteKey(key);
			var result = GetCacheManager<TValue>().TryGetOrAdd(key, _ => null, out var cache);
			var value = result ? cache.Value : default;
			return Tuple.Create(result, value);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		key = RewriteKey(key);
		if (TryGet<TValue>(key, out var value))
		{
			return value;
		}

		value = await factory();
		var result = GetCacheManager<TValue>().GetOrAdd(key, _ => GetCacheItem(key, value, timeout));
		return result.Value;
	}

	/// <inheritdoc />
	public async Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
	{
		var timespan = timeout - (isUtcTime ? DateTime.UtcNow : DateTime.Now);
		return await GetOrAddAsync(key, factory, timespan, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		key = RewriteKey(key);
		var value = await factory();
		return AddOrUpdate(key, value, timeout);
	}

	/// <inheritdoc />
	public async Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
	{
		var timespan = timeout - (isUtcTime ? DateTime.UtcNow : DateTime.Now);
		return await AddOrUpdateAsync(key, factory, timespan, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<TValue> AddOrUpdateAsync<TValue>(Func<Task<CacheItem<TValue>>> factory, CancellationToken cancellationToken = default)
	{
		var item = await factory();
		return AddOrUpdate(item);
	}

	/// <inheritdoc />
	protected override ICacheManager<TValue> GetCacheManager<TValue>()
	{
		return _manager.Instance<TValue>();
	}
}