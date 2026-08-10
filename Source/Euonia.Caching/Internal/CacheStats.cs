using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// <para>存储 <see cref="BaseCacheHandle{TCacheValue}"/> 的统计信息。</para>
/// <para>
/// 统计计数器是全局存储的，用于 <see cref="BaseCacheHandle{TCacheValue}"/>
/// 和每个缓存区域！
/// </para>
/// <para>
/// 要检索某个区域的计数器，只需指定 GetStatistics 的可选区域属性。
/// </para>
/// </summary>
/// <remarks>
/// 该类主要用于内部使用。只有 GetStatistics 是可见的。因此该类是密封的。
/// </remarks>
/// <typeparam name="TValue">拥有缓存句柄的继承对象类型。</typeparam>
public sealed class CacheStats<TValue> : IDisposable
{
	/// <summary>
	/// 用于表示"无区域"计数器的键。
	/// </summary>
	private static readonly string _nullRegionKey = Guid.NewGuid().ToString();

	/// <summary>
	/// 存储各区域统计计数器的并发字典。
	/// </summary>
	private readonly ConcurrentDictionary<string, CacheStatsCounter> _counters;

	/// <summary>
	/// 指示是否启用统计功能。
	/// </summary>
	private readonly bool _isStatsEnabled;

	/// <summary>
	/// 初始化 <see cref="CacheStats{TCacheValue}"/> 类的新实例。
	/// </summary>
	/// <param name="cacheName">缓存的名称。</param>
	/// <param name="handleName">句柄的名称。</param>
	/// <param name="isStatsEnabled">指示是否启用统计功能。</param>
	/// <exception cref="ArgumentNullException">
	/// 当 <c>cacheName</c> 或 <c>handleName</c> 为 <c>null</c> 时抛出。
	/// </exception>
	public CacheStats(string cacheName, string handleName, bool isStatsEnabled)
	{
		Check.EnsureNotNullOrWhiteSpace(cacheName, nameof(cacheName));
		Check.EnsureNotNullOrWhiteSpace(handleName, nameof(handleName));

		_counters = new ConcurrentDictionary<string, CacheStatsCounter>();
		_isStatsEnabled = isStatsEnabled;
	}

	/// <summary>
	/// 终结 <see cref="CacheStats{TCacheValue}"/> 类的实例。
	/// </summary>
	~CacheStats()
	{
		Dispose(false);
	}

	/// <summary>
	/// 执行与释放、重置非托管资源相关的应用程序定义任务。
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// <para>
	/// 返回对应于 <see cref="CacheStatsCounterType"/> 类型的统计信息。
	/// </para>
	/// <para>
	/// 如果缓存句柄配置为禁用统计功能，该方法将始终返回零。
	/// </para>
	/// </summary>
	/// <remarks>
	/// 在多线程环境中，计数器可能在读取时被更改。不要依赖这些计数器，因为它们可能不准确。
	/// </remarks>
	/// <example>
	/// <code>
	/// <![CDATA[
	/// var cache = CacheFactory.FromConfiguration("myCache");
	///
	/// foreach (var handle in cache.CacheHandles)
	/// {
	///    var stats = handle.Stats;
	///    var region = "myRegion";
	///    Console.WriteLine(string.Format(
	///            "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
	///                stats.GetStatistic(CacheStatsCounterType.Items, region),
	///                stats.GetStatistic(CacheStatsCounterType.Hits, region),
	///                stats.GetStatistic(CacheStatsCounterType.Misses, region),
	///                stats.GetStatistic(CacheStatsCounterType.RemoveCalls, region),
	///                stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls, region),
	///                stats.GetStatistic(CacheStatsCounterType.ClearCalls, region),
	///                stats.GetStatistic(CacheStatsCounterType.AddCalls, region),
	///                stats.GetStatistic(CacheStatsCounterType.PutCalls, region),
	///                stats.GetStatistic(CacheStatsCounterType.GetCalls, region)
	///            ));
	/// }
	/// ]]>
	/// </code>
	/// </example>
	/// <param name="type">要检索的统计类型。</param>
	/// <param name="region">
	/// 区域。返回值将仅表示该区域的计数器。
	/// </param>
	/// <returns>
	/// 表示指定 <see cref="CacheStatsCounterType"/> 和区域计数的数字。
	/// </returns>
	public long GetStatistic(CacheStatsCounterType type, string region)
	{
		if (!_isStatsEnabled)
		{
			return 0L;
		}

		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		var counter = GetCounter(region);
		return counter.Get(type);
	}

	/// <summary>
	/// <para>
	/// 返回与 <see cref="CacheStatsCounterType"/> 类型对应的统计信息（无区域重载）。
	/// </para>
	/// <para>
	/// 如果缓存句柄配置为禁用统计功能，该方法将始终返回零。
	/// </para>
	/// </summary>
	/// <remarks>
	/// 在多线程环境中，计数器可能在读取时被更改。不要依赖这些计数器，因为它们可能不准确。
	/// </remarks>
	/// <example>
	/// <code>
	/// <![CDATA[
	/// var cache = CacheFactory.FromConfiguration("myCache");
	///
	/// foreach (var handle in cache.CacheHandles)
	/// {
	///    var stats = handle.Stats;
	///    Console.WriteLine(string.Format(
	///            "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
	///                stats.GetStatistic(CacheStatsCounterType.Items),
	///                stats.GetStatistic(CacheStatsCounterType.Hits),
	///                stats.GetStatistic(CacheStatsCounterType.Misses),
	///                stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
	///                stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
	///                stats.GetStatistic(CacheStatsCounterType.ClearCalls),
	///                stats.GetStatistic(CacheStatsCounterType.AddCalls),
	///                stats.GetStatistic(CacheStatsCounterType.PutCalls),
	///                stats.GetStatistic(CacheStatsCounterType.GetCalls)
	///            ));
	/// }
	/// ]]>
	/// </code>
	/// </example>
	/// <param name="type">要检索的统计类型。</param>
	/// <returns>表示指定 <see cref="CacheStatsCounterType"/> 计数的数字。</returns>
	public long GetStatistic(CacheStatsCounterType type) => GetStatistic(type, _nullRegionKey);

	/// <summary>
	/// 当缓存项被添加到缓存时调用。
	/// </summary>
	/// <param name="item">缓存项。</param>
	/// <exception cref="ArgumentNullException">当 <c>item</c> 为 <c>null</c> 时抛出。</exception>
	public void OnAdd(CacheItem<TValue> item)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		Check.EnsureNotNull(item, nameof(item));

		foreach (var counter in GetWorkingCounters(item.Region))
		{
			counter.Increment(CacheStatsCounterType.AddCalls);
			counter.Increment(CacheStatsCounterType.Items);
		}
	}

	/// <summary>
	/// 当缓存被清空时调用。
	/// </summary>
	public void OnClear()
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		// 清空需要锁，否则可能会破坏整体计数
		foreach (var key in _counters.Keys)
		{
			if (_counters.TryGetValue(key, out var counter))
			{
				counter.Set(CacheStatsCounterType.Items, 0L);
				counter.Increment(CacheStatsCounterType.ClearCalls);
			}
		}
	}

	/// <summary>
	/// 当某个缓存区域被清空时调用。
	/// </summary>
	/// <param name="region">区域。</param>
	public void OnClearRegion(string region)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		// 清空需要锁，否则可能会破坏整体计数
		// lock (this.lockObject)
		{
			var regionCounter = GetCounter(region);
			var itemCount = regionCounter.Get(CacheStatsCounterType.Items);
			regionCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
			regionCounter.Set(CacheStatsCounterType.Items, 0L);

			var defaultCounter = GetCounter(_nullRegionKey);
			defaultCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
			defaultCounter.Add(CacheStatsCounterType.Items, itemCount * -1);
		}
	}

	/// <summary>
	/// 当调用缓存 Get 时调用。
	/// </summary>
	/// <param name="region">区域。</param>
	public void OnGet(string region = null)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		foreach (var counter in GetWorkingCounters(region))
		{
			counter.Increment(CacheStatsCounterType.GetCalls);
		}
	}

	/// <summary>
	/// 当 Get 成功时调用。
	/// </summary>
	/// <param name="region">区域。</param>
	public void OnHit(string region = null)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		foreach (var counter in GetWorkingCounters(region))
		{
			counter.Increment(CacheStatsCounterType.Hits);
		}
	}

	/// <summary>
	/// 当 Get 未成功时调用。
	/// </summary>
	/// <param name="region">区域。</param>
	public void OnMiss(string region = null)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		foreach (var counter in GetWorkingCounters(region))
		{
			counter.Increment(CacheStatsCounterType.Misses);
		}
	}

	/// <summary>
	/// 当缓存项被更新时调用。
	/// </summary>
	/// <param name="item">缓存项。</param>
	/// <param name="itemAdded">如果为 <c>true</c>，表示该项原先不存在并已被添加。</param>
	/// <exception cref="ArgumentNullException">当 <c>item</c> 为 <c>null</c> 时抛出。</exception>
	public void OnPut(CacheItem<TValue> item, bool itemAdded)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		Check.EnsureNotNull(item, nameof(item));

		foreach (var counter in GetWorkingCounters(item.Region))
		{
			counter.Increment(CacheStatsCounterType.PutCalls);

			if (itemAdded)
			{
				counter.Increment(CacheStatsCounterType.Items);
			}
		}
	}

	/// <summary>
	/// 当缓存项从缓存中被移除时调用。
	/// </summary>
	/// <param name="region">区域。</param>
	public void OnRemove(string region = null)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		foreach (var counter in GetWorkingCounters(region))
		{
			counter.Increment(CacheStatsCounterType.RemoveCalls);
			counter.Decrement(CacheStatsCounterType.Items);
		}
	}

	/// <summary>
	/// 当缓存项已被更新时调用。
	/// </summary>
	/// <param name="key">键。</param>
	/// <param name="region">区域。</param>
	/// <param name="result">更新结果。</param>
	/// <exception cref="ArgumentNullException">当 <c>key</c> 或 <c>result</c> 为 <c>null</c> 时抛出。</exception>
	public void OnUpdate(string key, string region, CacheItemUpdateResult<TValue> result)
	{
		if (!_isStatsEnabled)
		{
			return;
		}

		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		Check.EnsureNotNull(result, nameof(result));

		foreach (var counter in GetWorkingCounters(region))
		{
			counter.Add(CacheStatsCounterType.GetCalls, result.NumberOfTriesNeeded);
			counter.Add(CacheStatsCounterType.Hits, result.NumberOfTriesNeeded);
			counter.Increment(CacheStatsCounterType.PutCalls);
		}
	}

	private void Dispose(bool disposeManaged)
	{
		if (disposeManaged)
		{
			_counters.Clear();
		}
	}

	/// <summary>
	/// 获取指定键对应的统计计数器；若不存在则创建并添加。
	/// </summary>
	/// <param name="key">计数器键（区域名称或无区域键）。</param>
	/// <returns>对应的 <see cref="CacheStatsCounter"/> 实例。</returns>
	private CacheStatsCounter GetCounter(string key)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		if (!_counters.TryGetValue(key, out var counter))
		{
			counter = new CacheStatsCounter();
			if (_counters.TryAdd(key, counter))
			{
				return counter;
			}

			return GetCounter(key);
		}

		return counter;
	}

	/// <summary>
	/// 获取指定区域的工作计数器集合，包含全局（无区域）计数器。
	/// </summary>
	/// <param name="region">区域名称；可为 <c>null</c>。</param>
	/// <returns>工作计数器序列。</returns>
	private IEnumerable<CacheStatsCounter> GetWorkingCounters(string region)
	{
		yield return GetCounter(_nullRegionKey);

		if (!string.IsNullOrWhiteSpace(region))
		{
			var counter = GetCounter(region);
			if (counter != null)
			{
				yield return counter;
			}
		}
	}
}
