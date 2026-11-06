using Nerosoft.Euonia.Caching;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 测试用内存缓存服务，仅实现 <see cref="CacheInterceptor"/> 依赖的同步成员，其余抛 <see cref="NotSupportedException"/>。
/// </summary>
internal sealed class FakeCacheService : ICacheService
{
	private readonly Dictionary<string, (object Value, DateTime? ExpiresAt)> _store = new();

	/// <summary>
	/// 获取或设置测试可控的当前 UTC 时间，条目过期判定以它为准。
	/// </summary>
	public DateTime NowUtc { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// 将当前 UTC 时间向前推进指定时长，用于在测试中制造过期。
	/// </summary>
	/// <param name="step">推进的时长。</param>
	public void Advance(TimeSpan step)
	{
		NowUtc = NowUtc.Add(step);
	}

	/// <summary>
	/// 获取指定键的缓存值；与接口默认语义不同，键不存在时直接抛异常。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型，必须与写入时一致。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <returns>缓存值。</returns>
	/// <exception cref="KeyNotFoundException">键不存在、已过期或类型不匹配时抛出。</exception>
	public TValue Get<TValue>(string key)
	{
		return TryGet(key, out TValue value)
			? value
			: throw new KeyNotFoundException(key);
	}

	/// <inheritdoc />
	/// <remarks>条目按 <see cref="NowUtc"/> 判定是否过期，且要求存储值的运行时类型与 <typeparamref name="TValue"/> 兼容。</remarks>
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

		value = default!;
		return false;
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public TValue GetOrAdd<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		return GetOrAdd(key, factory, ToTimespan(timeout, isUtcTime));
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null)
	{
		var value = factory();
		AddOrUpdate(key, value, timeout);
		return value;
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true)
	{
		return AddOrUpdate(key, factory, ToTimespan(timeout, isUtcTime));
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, TValue value, TimeSpan? timeout = null)
	{
		lock (_store)
		{
			_store[key] = (value!, timeout.HasValue ? NowUtc.Add(timeout.Value) : null);
		}

		return value;
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(string key, TValue value, DateTime timeout, bool isUtcTime = true)
	{
		return AddOrUpdate(key, value, ToTimespan(timeout, isUtcTime));
	}

	/// <inheritdoc />
	public TValue AddOrUpdate<TValue>(CacheItem<TValue> item)
	{
		return AddOrUpdate(item.Key, item.Value, item.ExpirationTimeout);
	}

	/// <inheritdoc />
	public bool Remove<TValue>(string key)
	{
		lock (_store)
		{
			return _store.Remove(key);
		}
	}

	/// <summary>
	/// 返回当前存储中的全部键，包含已过期但尚未清理的条目。
	/// </summary>
	/// <returns>键的快照。</returns>
	public IReadOnlyCollection<string> GetKeys()
	{
		lock (_store)
		{
			return _store.Keys.ToArray();
		}
	}

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
	public Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
	public Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
	public Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
	public Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
	public Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">测试替身未实现该异步成员，调用即抛出。</exception>
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