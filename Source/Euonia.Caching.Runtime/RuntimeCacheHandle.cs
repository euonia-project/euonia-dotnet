using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// <see cref="MemoryCache"/> 的简单实现。
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
/// <remarks>
/// 尽管 MemoryCache 既不支持区域，也不支持 RemoveAll/Clear 方法，但我们仍会
/// 通过缓存依赖来实现它。
/// </remarks>
public class RuntimeCacheHandle<TValue> : BaseCacheHandle<TValue>
{
	private const string DEFAULT_NAME = "default";

	private volatile MemoryCache _cache;
	private string _instanceKey;
	private int _instanceKeyLength;
	private readonly bool _isDefaultCache;

	/// <summary>
	/// 初始化 <see cref="RuntimeCacheHandle{TCacheValue}"/> 类的新实例。
	/// </summary>
	/// <param name="managerConfiguration">管理器配置。</param>
	/// <param name="configuration">缓存句柄配置。</param>
	/// <param name="options">特定于供应商的选项。</param>
	/// 
	public RuntimeCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, RuntimeCacheOptions? options = null)
		: base(managerConfiguration, configuration)
	{
		Check.EnsureNotNull(configuration, nameof(configuration));

		//if (_cacheName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName.ToUpper(CultureInfo.InvariantCulture)))
		if (string.Equals(DEFAULT_NAME, configuration.Name, StringComparison.InvariantCultureIgnoreCase))
		{
			//我们无法通过代码更改默认缓存配置，对吧？
			Check.Ensure(options == null, "MemoryCache Default instance can only be configured through app/web.config.");

			_isDefaultCache = true;
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
	/// 获取缓存设置。
	/// </summary>
	/// <value>缓存设置。</value>
	public NameValueCollection CacheSettings => GetSettings(_cache);

/// <summary>
    /// 获取缓存句柄当前维护的项数。
    /// </summary>
    /// <value>项数。</value>
    public override int Count => (int)_cache.GetCount();

    /// <summary>
    /// 释放非托管资源，并可选择性地释放托管的缓存实例。
    /// <para>仅释放实例自行创建的 <see cref="MemoryCache"/>，全局共享的 <see cref="MemoryCache.Default"/> 不在此列。</para>
    /// </summary>
    /// <param name="disposeManaged">指示是否释放托管资源。</param>
    protected override void Dispose(bool disposeManaged)
    {
        if (disposeManaged && !_isDefaultCache)
        {
            _cache.Dispose();
        }

        base.Dispose(disposeManaged);
    }

	/// <summary>
	/// 清空此缓存，移除基础缓存中的所有项以及所有区域。
	/// </summary>
	public override void Clear()
	{
		_cache.Remove(_instanceKey);
		CreateInstanceToken();
	}

	/// <summary>
	/// 清空缓存区域，仅移除指定 <paramref name="region"/> 中的所有项。
	/// </summary>
	/// <param name="region">缓存区域。</param>
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
	/// 向缓存中添加一个值。
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	/// <returns>
	/// 如果键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
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
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <returns>该 <c>CacheItem</c>。</returns>
	protected override CacheItem<TValue>? GetCacheItemInternal(string key) => GetCacheItemInternal(key, null);

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>该 <c>CacheItem</c>。</returns>
	protected override CacheItem<TValue>? GetCacheItemInternal(string key, string? region)
	{
		var fullKey = GetItemKey(key, region);

		if (_cache.Get(fullKey) is not CacheItem<TValue> item)
		{
			return null;
		}

		// 该项可能已经过期，因为 MemoryCache 默认每隔 20 秒才检查一次过期项！
		// 为了在访问时也能反映更小的时间范围（尤其是滑动过期），我们在此处进行检查……
		// cache.Get 最终会触发逐出回调，但以防万一……
		if (item.IsExpired)
		{
			RemoveInternal(item.Key, item.Region);
			TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);
			return null;
		}

		if (item.ExpirationMode == CacheExpirationMode.Sliding)
		{
			// 由于多线程问题，我们不使用 UpdateCallback，改为通过重新设置该项来重置它。
			// item = this.GetItemExpiration(item); // 由基础缓存句柄完成
			_cache.Set(fullKey, item, GetPolicy(item));
		}

		return item;
	}

	/// <summary>
	/// 将 <paramref name="item"/> 放入缓存。如果该项已存在，则会用新值更新；
	/// 如果该项不存在，则会将其添加到缓存。
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	protected override void PutInternalPrepared(CacheItem<TValue> item)
	{
		var key = GetItemKey(item);
		var policy = GetPolicy(item);
		_cache.Set(key, item, policy);
	}

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <returns>
	/// 如果找到该键并将其从缓存中移除，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中标识该项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 如果找到该键并将其从缓存中移除，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	protected override bool RemoveInternal(string key, string? region)
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
		// 释放实例时不要添加新键
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

		// 添加区域令牌并使其依赖于我们的实例令牌，这样当实例被清空时，
		// 所有区域也会被移除。
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
			// 这应该是创建区域令牌的唯一位置；如果它不存在，可能是被 clearRegion 移除了，
			// 但下次调用 put 或 add 时应重新添加该区域……
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

				//// 由于某些原因，如果设置此项，我们会遇到多线程问题……
				//// 参见 http://stackoverflow.com/questions/21680429/why-does-memorycache-throw-nullreferenceexception
				////policy.UpdateCallback = new CacheEntryUpdateCallback(ItemUpdated); // 必须设置，否则滑动过期完全不起作用。
				break;
		}

		item.LastAccessedUtc = DateTime.UtcNow;

		return policy;
	}

	private string GetItemKey(CacheItem<TValue> item) => GetItemKey(item.Key, item.Region);

	private string GetItemKey(string key, string? region = null)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		if (string.IsNullOrWhiteSpace(region))
		{
			return _instanceKey + ":" + key;
		}

		// 不带区域的键
		// <instance>:key
		// 带区域的键
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

		// 忽略手动移除，统计信息已经被更新
		if (arguments.RemovedReason == CacheEntryRemovedReason.Removed)
		{
			return;
		}

		ParseKeyParts(_instanceKeyLength, fullKey, out bool isToken, out bool hasRegion, out string? region, out string key);

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

			object? originalValue = null;
			if (arguments.CacheItem.Value is CacheItem<TValue> item)
			{
				originalValue = item.Value;
			}

			// 对逐出和过期的项触发缓存管理器的移除
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

	private static void ParseKeyParts(int instanceKeyLength, string fullKey, out bool isToken, out bool hasRegion, out string? region, out string key)
	{
		var relevantKey = fullKey[instanceKeyLength..];
		isToken = relevantKey[0] == '_';
		hasRegion = false;
		region = null;
		// key 仅在非 token 的分支中被使用，且该分支一定会为其赋值。
		key = null!;

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