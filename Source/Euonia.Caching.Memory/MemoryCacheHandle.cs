using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// Implementation of a cache handle using <see cref="Microsoft.Extensions.Caching.Memory"/>.
/// </summary>
/// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
public class MemoryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
{
	private volatile MemoryCache _cache;

	/// <summary>
	/// Initializes a new instance of the <see cref="MemoryCacheHandle{TCacheValue}"/> class.
	/// </summary>
	/// <param name="managerConfiguration">The manager configuration.</param>
	/// <param name="configuration">The cache handle configuration.</param>
	/// <param name="options">The vendor specific options.</param>
	///
	public MemoryCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, MemoryCacheOptions options = null)
		: base(managerConfiguration, configuration)
	{
		Check.EnsureNotNull(configuration, nameof(configuration));

		Options = options ?? new MemoryCacheOptions();
		_cache = MemoryCacheStoreRegistry.GetOrCreate(Options);
	}

	/// <inheritdoc/>
	public override int Count => _cache.Count;

	internal MemoryCacheOptions Options { get; }

	/// <inheritdoc/>
	protected override void Dispose(bool disposeManaged)
	{
		// 存储按配置共享，不能在此释放：其他 TCacheValue 的句柄仍在使用同一实例。
		// 共享存储随 MemoryCacheOptions 实例一同被回收（见 _sharedStores 的说明）。
		base.Dispose(disposeManaged);
	}

	/// <inheritdoc/>
	public override void Clear()
	{
		// 存储为多句柄共享，因此只能清空内容，不能替换实例（替换并释放会让其他句柄失去存储）。
		// 这也修正了语义：文档承诺「清空本缓存及其所有区域」，而此前只清空了当前 TCacheValue 的那一份。
		_cache.Clear();
	}

	/// <inheritdoc/>
	public override void ClearRegion(string region)
	{
		_cache.RemoveChildren(region);
		_cache.Remove(region);
	}

	/// <inheritdoc />
	public override bool Exists(string key)
	{
		return _cache.Contains(GetItemKey(key));
	}

	/// <inheritdoc />
	public override bool Exists(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		return _cache.Contains(GetItemKey(key, region));
	}

	/// <inheritdoc/>
	protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
	{
		return GetCacheItemInternal(key, null);
	}

	/// <inheritdoc/>
	protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
	{
		var fullKey = GetItemKey(key, region);

		if (_cache.Get(fullKey) is not CacheItem<TCacheValue> item)
		{
			return null;
		}

		if (item.IsExpired)
		{
			RemoveInternal(item.Key, item.Region);
			TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);
			return null;
		}

		if (item.ExpirationMode == CacheExpirationMode.Sliding)
		{
			// item = this.GetItemExpiration(item);
			_cache.Set(fullKey, item, GetOptions(item));
		}

		return item;
	}

	/// <inheritdoc/>
	protected override bool RemoveInternal(string key)
	{
		return RemoveInternal(key, null);
	}

	/// <inheritdoc/>
	protected override bool RemoveInternal(string key, string region)
	{
		var fullKey = GetItemKey(key, region);
		var result = _cache.Contains(fullKey);
		if (result)
		{
			_cache.Remove(fullKey);
		}

		return result;
	}

	/// <inheritdoc/>
	protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
	{
		var key = GetItemKey(item);

		if (_cache.Contains(key))
		{
			return false;
		}

		var options = GetOptions(item);
		_cache.Set(key, item, options);

		if (item.Region != null)
		{
			_cache.RegisterChild(item.Region, key);
		}

		return true;
	}

	/// <inheritdoc/>
	protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
	{
		var key = GetItemKey(item);

		var options = GetOptions(item);
		_cache.Set(key, item, options);

		if (item.Region != null)
		{
			_cache.RegisterChild(item.Region, key);
		}
	}

	private string GetItemKey(CacheItem<TCacheValue> item) => GetItemKey(item?.Key, item?.Region);

	private string GetItemKey(string key, string region = null)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		if (string.IsNullOrWhiteSpace(region))
		{
			return key;
		}

		return region + ":" + key;
	}

	private MemoryCacheEntryOptions GetOptions(CacheItem<TCacheValue> item)
	{
		if (item.Region != null)
		{
			if (!_cache.Contains(item.Region))
			{
				CreateRegionToken(item.Region);
			}
		}

		var options = new MemoryCacheEntryOptions()
			.SetPriority(CacheItemPriority.Normal);

		if (item.ExpirationMode == CacheExpirationMode.Absolute)
		{
			options.SetAbsoluteExpiration(item.ExpirationTimeout);
			options.RegisterPostEvictionCallback(ItemRemoved, Tuple.Create(item.Key, item.Region));
		}

		if (item.ExpirationMode == CacheExpirationMode.Sliding)
		{
			options.SetSlidingExpiration(item.ExpirationTimeout);
			options.RegisterPostEvictionCallback(ItemRemoved, Tuple.Create(item.Key, item.Region));
		}

		item.LastAccessedUtc = DateTime.UtcNow;

		return options;
	}

	private void CreateRegionToken(string region)
	{
		var options = new MemoryCacheEntryOptions
		{
			Priority = CacheItemPriority.Normal,
			AbsoluteExpiration = DateTimeOffset.MaxValue,
			SlidingExpiration = TimeSpan.MaxValue,
		};

		_cache.Set(region, new ConcurrentDictionary<object, bool>(), options);
	}

	private void ItemRemoved(object key, object value, EvictionReason reason, object state)
	{
		var strKey = key as string;
		if (string.IsNullOrWhiteSpace(strKey))
		{
			return;
		}

		// don't trigger stuff on manual remove
		if (reason == EvictionReason.Removed)
		{
			return;
		}

		if (state is Tuple<string, string> tuple)
		{
			if (tuple.Item2 != null)
			{
				Stats.OnRemove(tuple.Item2);
			}
			else
			{
				Stats.OnRemove();
			}

			object originalValue = null;
			if (value is CacheItem<TCacheValue> item)
			{
				originalValue = item.Value;
			}

			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (reason)
			{
				case EvictionReason.Capacity:
					TriggerCacheSpecificRemove(tuple.Item1, tuple.Item2, CacheItemRemovedReason.Evicted, originalValue);
					break;
				case EvictionReason.Expired:
					TriggerCacheSpecificRemove(tuple.Item1, tuple.Item2, CacheItemRemovedReason.Expired, originalValue);
					break;
			}
		}
		else
		{
			Stats.OnRemove();
		}
	}
}