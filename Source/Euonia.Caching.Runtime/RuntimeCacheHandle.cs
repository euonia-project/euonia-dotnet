using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// Simple implementation for the <see cref="MemoryCache"/>.
/// </summary>
/// <typeparam name="TValue">The type of the cache value.</typeparam>
/// <remarks>
/// Although the MemoryCache doesn't support regions nor a RemoveAll/Clear method, we will
/// implement it via cache dependencies.
/// </remarks>
public class RuntimeCacheHandle<TValue> : BaseCacheHandle<TValue>
{
	private const string DEFAULT_NAME = "default";

	private volatile MemoryCache _cache;
	private string _instanceKey;
	private int _instanceKeyLength;

	/// <summary>
	/// Initializes a new instance of the <see cref="RuntimeCacheHandle{TCacheValue}"/> class.
	/// </summary>
	/// <param name="managerConfiguration">The manager configuration.</param>
	/// <param name="configuration">The cache handle configuration.</param>
	/// <param name="options">The vendor specific options.</param>
	/// 
	public RuntimeCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, RuntimeCacheOptions options = null)
		: base(managerConfiguration, configuration)
	{
		Check.EnsureNotNull(configuration, nameof(configuration));

		//if (_cacheName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName.ToUpper(CultureInfo.InvariantCulture)))
		if (string.Equals(DEFAULT_NAME, configuration.Name, StringComparison.InvariantCultureIgnoreCase))
		{
			//we can't change default cache configuration by code, can we?
			Check.Ensure(options == null, "MemoryCache Default instance can only be configured through app/web.config.");

			_cache = MemoryCache.Default;
		}
		else
		{
			var config = options == null ? new NameValueCollection(0) : options.AsNameValueCollection();
			_cache = new MemoryCache(configuration.Name, config);
		}

		_instanceKey = Guid.NewGuid().ToString();
		_instanceKeyLength = _instanceKey.Length;
		CreateInstanceToken();
	}

	/// <summary>
	/// Gets the cache settings.
	/// </summary>
	/// <value>The cache settings.</value>
	public NameValueCollection CacheSettings => GetSettings(_cache);

	/// <summary>
	/// Gets the number of items the cache handle currently maintains.
	/// </summary>
	/// <value>The count.</value>
	public override int Count => (int)_cache.GetCount();

	/// <summary>
	/// Clears this cache, removing all items in the base cache and all regions.
	/// </summary>
	public override void Clear()
	{
		_cache.Remove(_instanceKey);
		CreateInstanceToken();
	}

	/// <summary>
	/// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
	/// </summary>
	/// <param name="region">The cache region.</param>
	public override void ClearRegion(string region) =>
		_cache.Remove(GetRegionTokenKey(region));

	/// <inheritdoc />
	public override bool Exists(string key)
	{
		return _cache.Contains(GetItemKey(key));
	}

	/// <inheritdoc />
	public override bool Exists(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
		var fullKey = GetItemKey(key, region);
		return _cache.Contains(fullKey);
	}

	/// <summary>
	/// Adds a value to the cache.
	/// </summary>
	/// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
	/// <returns>
	/// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
	/// </returns>
	protected override bool AddInternalPrepared(CacheItem<TValue> item)
	{
		var key = GetItemKey(item);

		if (_cache.Contains(key))
		{
			return false;
		}

		var policy = GetPolicy(item);
		return _cache.Add(key, item, policy);
	}

	/// <summary>
	/// Gets a <c>CacheItem</c> for the specified key.
	/// </summary>
	/// <param name="key">The key being used to identify the item within the cache.</param>
	/// <returns>The <c>CacheItem</c>.</returns>
	protected override CacheItem<TValue> GetCacheItemInternal(string key) => GetCacheItemInternal(key, null);

	/// <summary>
	/// Gets a <c>CacheItem</c> for the specified key.
	/// </summary>
	/// <param name="key">The key being used to identify the item within the cache.</param>
	/// <param name="region">The cache region.</param>
	/// <returns>The <c>CacheItem</c>.</returns>
	protected override CacheItem<TValue> GetCacheItemInternal(string key, string region)
	{
		var fullKey = GetItemKey(key, region);

		if (_cache.Get(fullKey) is not CacheItem<TValue> item)
		{
			return null;
		}

		// maybe the item is already expired because MemoryCache implements a default interval
		// of 20 seconds! to check for expired items on each store, we do it on access to also
		// reflect smaller time frames especially for sliding expiration...
		// cache.Get eventually triggers eviction callback, but just in case...
		if (item.IsExpired)
		{
			RemoveInternal(item.Key, item.Region);
			TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);
			return null;
		}

		if (item.ExpirationMode == CacheExpirationMode.Sliding)
		{
			// because we don't use UpdateCallback because of some multithreading issues lets
			// try to simply reset the item by setting it again.
			// item = this.GetItemExpiration(item); // done via base cache handle
			_cache.Set(fullKey, item, GetPolicy(item));
		}

		return item;
	}

	/// <summary>
	/// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
	/// with the new value. If the item doesn't exist, the item will be added to the cache.
	/// </summary>
	/// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
	protected override void PutInternalPrepared(CacheItem<TValue> item)
	{
		var key = GetItemKey(item);
		var policy = GetPolicy(item);
		_cache.Set(key, item, policy);
	}

	/// <summary>
	/// Removes a value from the cache for the specified key.
	/// </summary>
	/// <param name="key">The key being used to identify the item within the cache.</param>
	/// <returns>
	/// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
	/// </returns>
	protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

	/// <summary>
	/// Removes a value from the cache for the specified key.
	/// </summary>
	/// <param name="key">The key being used to identify the item within the cache.</param>
	/// <param name="region">The cache region.</param>
	/// <returns>
	/// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
	/// </returns>
	protected override bool RemoveInternal(string key, string region)
	{
		var fullKey = GetItemKey(key, region);
		if (!_cache.Contains(fullKey))
		{
			return false;
		}

		_cache.Remove(fullKey);
		return true;
	}

	private static NameValueCollection GetSettings(MemoryCache instance)
	{
		var cacheCfg = new NameValueCollection
		{
			{ "CacheMemoryLimitMegabytes", (instance.CacheMemoryLimit / 1024 / 1024).ToString(CultureInfo.InvariantCulture) },
			{ "PhysicalMemoryLimitPercentage", instance.PhysicalMemoryLimit.ToString(CultureInfo.InvariantCulture) },
			{ "PollingInterval", instance.PollingInterval.ToString() }
		};

		return cacheCfg;
	}

	private void CreateInstanceToken()
	{
		// don't add a new key while we are disposing our instance
		if (!Disposing)
		{
			var instanceItem = new CacheItem<string>(_instanceKey, _instanceKey);
			var policy = new CacheItemPolicy()
			{
				Priority = CacheItemPriority.NotRemovable,
				RemovedCallback = InstanceTokenRemoved,
				AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
				SlidingExpiration = ObjectCache.NoSlidingExpiration,
			};

			_cache.Add(instanceItem.Key, instanceItem, policy);
		}
	}

	private void CreateRegionToken(string region)
	{
		var key = GetRegionTokenKey(region);

		// add region token with dependency on our instance token, so that all regions get
		// removed whenever the instance gets cleared.
		var policy = new CacheItemPolicy()
		{
			Priority = CacheItemPriority.NotRemovable,
			AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
			SlidingExpiration = ObjectCache.NoSlidingExpiration,
			ChangeMonitors = { _cache.CreateCacheEntryChangeMonitor(new[] { _instanceKey }) },
		};
		_cache.Add(key, region, policy);
	}

	private CacheItemPolicy GetPolicy(CacheItem<TValue> item)
	{
		var monitorKeys = new[] { _instanceKey };

		if (!string.IsNullOrWhiteSpace(item.Region))
		{
			// this should be the only place to create the region token if it doesn't exist it
			// might got removed by clearRegion but next time put or add gets called, the region
			// should be re added...
			var regionToken = GetRegionTokenKey(item.Region);
			if (!_cache.Contains(regionToken))
			{
				CreateRegionToken(item.Region);
			}

			monitorKeys = new[] { _instanceKey, regionToken };
		}

		var policy = new CacheItemPolicy()
		{
			Priority = CacheItemPriority.Default,
			ChangeMonitors = { _cache.CreateCacheEntryChangeMonitor(monitorKeys) },
			AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
			SlidingExpiration = ObjectCache.NoSlidingExpiration,
		};

		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (item.ExpirationMode)
		{
			case CacheExpirationMode.Absolute:
				policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
				policy.RemovedCallback = ItemRemoved;
				break;
			case CacheExpirationMode.Sliding:
				policy.SlidingExpiration = item.ExpirationTimeout;
				policy.RemovedCallback = ItemRemoved;

				//// for some reason, we'll get issues with multithreading if we set this...
				//// see http://stackoverflow.com/questions/21680429/why-does-memorycache-throw-nullreferenceexception
				////policy.UpdateCallback = new CacheEntryUpdateCallback(ItemUpdated); // must be set, otherwise sliding doesn't work at all.
				break;
		}

		item.LastAccessedUtc = DateTime.UtcNow;

		return policy;
	}

	private string GetItemKey(CacheItem<TValue> item) => GetItemKey(item?.Key, item?.Region);

	private string GetItemKey(string key, string region = null)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		if (string.IsNullOrWhiteSpace(region))
		{
			return _instanceKey + ":" + key;
		}

		// key without region
		// <instance>:key
		// key with region
		// <instance>@<regionlen><regionstring>:<keystring>
		// <instance>@6region:key
		return string.Concat(_instanceKey, "@", region.Length, "@", region, ":", key);
	}

	private string GetRegionTokenKey(string region)
	{
		var key = string.Concat(_instanceKey, "_", region);
		return key;
	}

	private void InstanceTokenRemoved(CacheEntryRemovedArguments arguments)
	{
		_instanceKey = Guid.NewGuid().ToString();
		_instanceKeyLength = _instanceKey.Length;
	}

	private void ItemRemoved(CacheEntryRemovedArguments arguments)
	{
		var fullKey = arguments.CacheItem.Key;
		if (string.IsNullOrWhiteSpace(fullKey))
		{
			return;
		}

		// ignore manual removes, stats will be updated already
		if (arguments.RemovedReason == CacheEntryRemovedReason.Removed)
		{
			return;
		}

		ParseKeyParts(_instanceKeyLength, fullKey, out bool isToken, out bool hasRegion, out string region, out string key);

		if (!isToken)
		{
			if (hasRegion)
			{
				Stats.OnRemove(region);
			}
			else
			{
				Stats.OnRemove();
			}

			object originalValue = null;
			if (arguments.CacheItem.Value is CacheItem<TValue> item)
			{
				originalValue = item.Value;
			}

			// trigger cachemanager's remove on evicted and expired items
			if (arguments.RemovedReason == CacheEntryRemovedReason.Evicted || arguments.RemovedReason == CacheEntryRemovedReason.CacheSpecificEviction)
			{
				TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Evicted, originalValue);
			}
			else if (arguments.RemovedReason == CacheEntryRemovedReason.Expired)
			{
				TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, originalValue);
			}
		}
	}

	private static void ParseKeyParts(int instanceKeyLength, string fullKey, out bool isToken, out bool hasRegion, out string region, out string key)
	{
		var relevantKey = fullKey[instanceKeyLength..];
		isToken = relevantKey[0] == '_';
		hasRegion = false;
		region = null;
		key = null;

		if (!isToken)
		{
			hasRegion = relevantKey[0] == '@';
			var regionLenEnd = hasRegion ? relevantKey.IndexOf('@', 1) : -1;

			int regionLen;
			regionLen = hasRegion && regionLenEnd > 0 ? int.TryParse(relevantKey[1..regionLenEnd], out regionLen) ? regionLen : 0 : 0;
			hasRegion = hasRegion && regionLen > 0;

			var restKey = hasRegion ? relevantKey[(regionLenEnd + 1)..] : relevantKey;
			region = hasRegion ? restKey[..regionLen] : null;
			key = restKey[(regionLen + 1)..];
		}
	}
}