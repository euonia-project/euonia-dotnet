using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 测试用内存缓存服务，仅实现 <see cref="CacheInterceptor"/> 依赖的同步成员，其余抛 <see cref="NotSupportedException"/>。
/// </summary>
internal sealed class FakeCacheService : ICacheService
{
	private readonly Dictionary<string, (object Value, DateTime? ExpiresAt)> _store = new();

	public DateTime NowUtc { get; set; } = DateTime.UtcNow;

	public void Advance(TimeSpan step)
	{
		NowUtc = NowUtc.Add(step);
	}

	public TValue Get<TValue>(string key)
	{
		return TryGet(key, out TValue value)
			? value
			: throw new KeyNotFoundException(key);
	}

	public bool TryGet<TValue>(string key, out TValue value)
	{
		if (_store.TryGetValue(key, out var entry) && !IsExpired(entry.ExpiresAt))
		{
			if (entry.Value is TValue typed)
			{
				value = typed;
				return true;
			}
		}

		value = default;
		return false;
	}

	public TValue GetOrAdd<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null)
	{
		if (TryGet(key, out TValue value))
		{
			return value;
		}

		value = factory();
		AddOrUpdate(key, value, timeout);
		return value;
	}

	public TValue GetOrAdd<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		return GetOrAdd(key, factory, ToTimespan(timeout, isUtcTime));
	}

	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null)
	{
		var value = factory();
		AddOrUpdate(key, value, timeout);
		return value;
	}

	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		return AddOrUpdate(key, factory, ToTimespan(timeout, isUtcTime));
	}

	public TValue AddOrUpdate<TValue>(string key, TValue value, TimeSpan? timeout = null)
	{
		lock (_store)
		{
			_store[key] = (value!, timeout.HasValue ? NowUtc.Add(timeout.Value) : null);
		}

		return value;
	}

	public TValue AddOrUpdate<TValue>(string key, TValue value, DateTime timeout, bool isUtcTime = true)
	{
		return AddOrUpdate(key, value, ToTimespan(timeout, isUtcTime));
	}

	public TValue AddOrUpdate<TValue>(CacheItem<TValue> item)
	{
		return AddOrUpdate(item.Key, item.Value, item.ExpirationTimeout);
	}

	public bool Remove<TValue>(string key)
	{
		lock (_store)
		{
			return _store.Remove(key);
		}
	}

	public IReadOnlyCollection<string> GetKeys()
	{
		lock (_store)
		{
			return _store.Keys.ToArray();
		}
	}

	public Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public Task<TValue> AddOrUpdateAsync<TValue>(Func<Task<CacheItem<TValue>>> factory, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	private bool IsExpired(DateTime? expiresAt)
	{
		return expiresAt.HasValue && expiresAt.Value <= NowUtc;
	}

	private TimeSpan? ToTimespan(DateTime timeout, bool isUtcTime)
	{
		var now = isUtcTime ? NowUtc : DateTime.Now;
		return timeout > now ? timeout - now : TimeSpan.Zero;
	}
}