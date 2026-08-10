using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 此句柄仅供内部使用和测试。它不实现任何过期功能。
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public class DictionaryCacheHandle<TValue> : BaseCacheHandle<TValue>
{
	/// <summary>
	/// 过期项扫描的间隔毫秒数。
	/// </summary>
	private const int SCAN_INTERVAL = 5000;

	/// <summary>
	/// 用于生成定时器初始延迟的随机数生成器。
	/// </summary>
	private static readonly Random _random = new();

	/// <summary>
	/// 存储缓存项（以完整键为键）的并发字典。
	/// </summary>
	private readonly ConcurrentDictionary<string, CacheItem<TValue>> _cache;

	/// <summary>
	/// 用于周期扫描过期项的定时器。
	/// </summary>
	private readonly Timer _timer;

	//private long _lastScan = 0L;

	/// <summary>
	/// 指示扫描是否正在运行的标志（0 未运行，1 运行中）。
	/// </summary>
	private int _scanRunning;

	//private object _startScanLock = new object();

	/// <summary>
	/// 初始化 <see cref="DictionaryCacheHandle{TCacheValue}"/> 类的新实例。
	/// </summary>
	/// <param name="managerConfiguration">管理器配置。</param>
	/// <param name="configuration">缓存句柄配置。</param>
	/// 
	public DictionaryCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration)
		: base(managerConfiguration, configuration)
	{
		_cache = new ConcurrentDictionary<string, CacheItem<TValue>>();
		_timer = new Timer(TimerLoop, null, _random.Next(1000, SCAN_INTERVAL), SCAN_INTERVAL);
	}

	/// <summary>
	/// 获取缓存项的数量。
	/// </summary>
	/// <value>数量。</value>
	public override int Count => _cache.Count;

	/// <summary>
	/// 清空此缓存，移除基础缓存及所有区域中的所有项。
	/// </summary>
	public override void Clear() => _cache.Clear();

	/// <summary>
	/// 清空缓存区域，仅移除指定 <paramref name="region"/> 中的所有项。
	/// </summary>
	/// <param name="region">缓存区域。</param>
	/// <exception cref="ArgumentNullException">当 <c>region</c> 为 <c>null</c> 时抛出。</exception>
	public override void ClearRegion(string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		var key = string.Concat(region, ":");
		foreach (var item in _cache.Where(p => p.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase)))
		{
			_cache.TryRemove(item.Key, out _);
		}
	}

	/// <inheritdoc />
	public override bool Exists(string key)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		return _cache.ContainsKey(key);
	}

	/// <inheritdoc />
	public override bool Exists(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
		var fullKey = GetKey(key, region);
		return _cache.ContainsKey(fullKey);
	}

	/// <summary>
	/// 添加一个值到缓存。
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	/// <returns>
	/// <c>true</c> 如果键尚未添加到缓存，<c>false</c> 否则。
	/// </returns>
	/// <exception cref="ArgumentNullException">如果 <c>item</c> 为 <c>null</c>。</exception>
	protected override bool AddInternalPrepared(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		var key = GetKey(item.Key, item.Region);

		return _cache.TryAdd(key, item);
	}

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中识别项的键。</param>
	/// <returns><c>CacheItem</c>。</returns>
	protected override CacheItem<TValue> GetCacheItemInternal(string key) =>
		GetCacheItemInternal(key, null);

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于在缓存中识别项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns><c>CacheItem</c>。</returns>
	protected override CacheItem<TValue> GetCacheItemInternal(string key, string region)
	{
		var fullKey = GetKey(key, region);

		if (_cache.TryGetValue(fullKey, out CacheItem<TValue> result))
		{
			if (result.ExpirationMode != CacheExpirationMode.None && IsExpired(result, DateTime.UtcNow))
			{
				_cache.TryRemove(fullKey, out _);
				TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, result.Value);
				return null;
			}
		}

		return result;
	}

	/// <summary>
	/// 将 <paramref name="item"/> 放入缓存。如果项已存在，它将被更新为新的值。如果项不存在，项将被添加到缓存。
	/// </summary>
	/// <param name="item">要添加到缓存的 <c>CacheItem</c>。</param>
	/// <exception cref="ArgumentNullException">如果 <c>item</c> 为 <c>null</c>。</exception>
	protected override void PutInternalPrepared(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		_cache[GetKey(item.Key, item.Region)] = item;
	}

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中识别项的键。</param>
	/// <returns>
	/// <c>true</c> 如果键被找到并从缓存中移除，<c>false</c> 否则。
	/// </returns>
	protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于在缓存中识别项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// <c>true</c> 如果键被找到并从缓存中移除，<c>false</c> 否则。
	/// </returns>
	protected override bool RemoveInternal(string key, string region)
	{
		var fullKey = GetKey(key, region);
		return _cache.TryRemove(fullKey, out _);
	}

	/// <summary>
	/// 获取键。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="region">区域。</param>
	/// <returns>完整键。若区域为空，则返回键本身；否则返回拼接了区域前缀的完整键。</returns>
	/// <exception cref="ArgumentException">如果 <c>key</c> 是空的。</exception>
	private static string GetKey(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		return string.IsNullOrWhiteSpace(region) ? key : string.Concat(region, ":", key);
	}

	/// <summary>
	/// 判断指定的缓存项是否已过期。
	/// </summary>
	/// <param name="item">缓存项。</param>
	/// <param name="now">当前时间。</param>
	/// <returns>如果已过期，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	private static bool IsExpired(CacheItem<TValue> item, DateTime now)
	{
		switch (item.ExpirationMode)
		{
			case CacheExpirationMode.Absolute
				when item.CreatedUtc.Add(item.ExpirationTimeout) < now:
			case CacheExpirationMode.Sliding
				when item.LastAccessedUtc.Add(item.ExpirationTimeout) < now:
				return true;
			case CacheExpirationMode.Default:
			case CacheExpirationMode.None:
			default:
				return false;
		}
	}

	/// <summary>
	/// 定时器回调，用于周期扫描并移除过期项。通过 <see cref="Interlocked"/> 防止并发扫描。
	/// </summary>
	/// <param name="state">定时器状态对象（未使用）。</param>
	private void TimerLoop(object state)
	{
		if (_scanRunning > 0)
		{
			return;
		}

		if (Interlocked.CompareExchange(ref _scanRunning, 1, 0) != 0)
		{
			return;
		}

		try
		{
			var _ = ScanForExpiredItems();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
		finally
		{
			Interlocked.Exchange(ref _scanRunning, 0);
		}
	}

	/// <summary>
	/// 扫描缓存中的所有项，移除已过期的项并更新统计信息。
	/// </summary>
	/// <returns>被移除的过期项数量。</returns>
	private int ScanForExpiredItems()
	{
		var removed = 0;
		var now = DateTime.UtcNow;
		foreach (var item in _cache.Values)
		{
			if (!IsExpired(item, now))
			{
				continue;
			}

			RemoveInternal(item.Key, item.Region);

			// 触发全局逐出事件
			TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);

			// 修正统计信息
			Stats.OnRemove(item.Region);
			removed++;
		}

		return removed;
	}

	/// <summary>
	/// 释放非托管资源，并可选择性地释放托管资源。
	/// </summary>
	~DictionaryCacheHandle()
	{
		_timer.Dispose();
	}
}