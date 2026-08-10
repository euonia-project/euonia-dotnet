using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Caching.Redis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// The implement of <see cref="ICacheService"/> with Redis.
/// </summary>
public class RedisCacheService : BaseCacheService, ICacheService
{
	private readonly RedisCacheManager _manager;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="options"></param>
	public RedisCacheService(IOptions<RedisCacheOptions> options)
	{
		_manager = new RedisCacheManager(options.Value);
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
		var item = GetCacheManager<TValue>().GetCacheItem(key);
		if (item != null)
		{
			value = item.Value;
			return true;
		}

		value = default;
		return false;
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
		// 应用键前缀（CacheItem 不可变，通过 WithKey 生成带前缀的新项）
		item = item.WithKey(RewriteKey(item.Key));
		return GetCacheManager<TValue>().AddOrUpdate(item, _ => item.Value);
	}

	/// <inheritdoc />
	public bool Remove<TValue>(string key)
	{
		key = RewriteKey(key);
		return GetCacheManager<TValue>().Remove(key);
	}

	/// <inheritdoc />
	public Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		key = RewriteKey(key);
		var item = GetCacheManager<TValue>().GetCacheItem(key);
		var value = item != null ? item.Value : default;

		return Task.FromResult(Tuple.Create(item != null, value));
	}

	/// <inheritdoc />
	public async Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		key = RewriteKey(key);
		var manager = GetCacheManager<TValue>();

		var existing = manager.GetCacheItem(key);
		if (existing != null)
		{
			return existing.Value;
		}

		var value = await factory();
		var result = manager.GetOrAdd(key, _ => GetCacheItem(key, value, timeout));
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
		cancellationToken.ThrowIfCancellationRequested();

		// 不要在此处重写键：AddOrUpdate 内部会重写，避免双重前缀
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
		cancellationToken.ThrowIfCancellationRequested();

		var item = await factory();
		return AddOrUpdate(item);
	}

	/// <inheritdoc />
	protected override ICacheManager<TValue> GetCacheManager<TValue>()
	{
		return _manager.Instance<TValue>();
	}
}