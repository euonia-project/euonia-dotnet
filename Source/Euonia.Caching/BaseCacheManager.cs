using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="BaseCacheManager{TCacheValue}"/> 实现了 <see cref="ICacheManager{TCacheValue}"/>，是本库的主要类。
/// 缓存管理器将所有缓存操作委托给已添加的 <see cref="BaseCacheHandle{T}"/> 列表，
/// 并根据规则和配置使各缓存句柄保持同步。
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public partial class BaseCacheManager<TValue> : BaseCache<TValue>, ICacheManager<TValue>
{
	/// <summary>
	/// 按顺序排列的缓存句柄数组，所有缓存操作都会委托给这些句柄。
	/// </summary>
	private readonly BaseCacheHandle<TValue>[] _cacheHandles;

	/// <summary>
	/// <see cref="CacheHandles"/> 的只读视图，构造时创建一次，避免每次访问都重新分配集合。
	/// </summary>
	private readonly ReadOnlyCollection<BaseCacheHandle<TValue>> _cacheHandlesCollection;

	/// <summary>
	/// 配置的缓存背板，用于跨进程同步缓存操作；未配置时为 <c>null</c>。
	/// </summary>
	private readonly CacheBackplane _cacheBackplane;

	/// <summary>
	/// 使用指定的 <paramref name="configuration"/> 初始化 <see cref="BaseCacheManager{TCacheValue}"/> 类的新实例。
	/// 如果 <paramref name="configuration"/> 的名称已定义，缓存管理器将使用该名称；否则将生成一个随机字符串。
	/// </summary>
	/// <param name="configuration">
	/// 用于定义缓存管理器结构与复杂度的配置。
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="configuration"/> 为 <c>null</c> 时抛出。
	/// </exception>
	/// <see cref="CacheFactory"/>
	/// <see cref="ConfigurationBuilder"/>
	/// <see cref="BaseCacheHandle{TCacheValue}"/>
	public BaseCacheManager(CacheManagerConfiguration configuration)
		: this(configuration?.Name ?? Guid.NewGuid().ToString(), configuration)
	{
	}

	/// <summary>
	/// 使用指定的 <paramref name="name"/> 和 <paramref name="configuration"/> 初始化 <see cref="BaseCacheManager{TCacheValue}"/> 类的新实例。
	/// </summary>
	/// <param name="name">缓存名称。</param>
	/// <param name="configuration">
	/// 用于定义缓存管理器结构与复杂度的配置。
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="name"/> 或 <paramref name="configuration"/> 为 <c>null</c> 时抛出。
	/// </exception>
	/// <see cref="CacheFactory"/>
	/// <see cref="ConfigurationBuilder"/>
	/// <see cref="BaseCacheHandle{TCacheValue}"/>
	private BaseCacheManager(string name, CacheManagerConfiguration configuration)
	{
		Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
		Check.EnsureNotNull(configuration, nameof(configuration));

		Name = name;
		Configuration = configuration;

		try
		{
			_cacheHandles = CacheReflectionHelper.CreateCacheHandles(this).ToArray();
			_cacheHandlesCollection = Array.AsReadOnly(_cacheHandles);

			var index = 0;
			foreach (var handle in _cacheHandles)
			{
				var handleIndex = index;
				handle.OnCacheSpecificRemove += (_, args) =>
				{
					// 基础缓存句柄会自行处理此操作的日志记录

					if (Configuration.UpdateMode == CacheUpdateMode.Up)
					{
						EvictFromHandlesAbove(args.Key, args.Region, handleIndex);
					}

					// 在清理之后再向下传递，否则该项可能仍驻留在内存中
					TriggerOnRemoveByHandle(args.Key, args.Region, args.Reason, handleIndex + 1, args.Value);
				};

				index++;
			}

			_cacheBackplane = CacheReflectionHelper.CreateBackplane(configuration);
			if (_cacheBackplane != null)
			{
				RegisterCacheBackplane(_cacheBackplane);
			}
		}
		catch (Exception ex)
		{
			// 保留原始异常堆栈，而非 throw ex.InnerException ?? ex 丢失堆栈信息
			ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
		}
	}

	/// <inheritdoc />
	public event EventHandler<CacheActionEventArgs> OnAdd;

	/// <inheritdoc />
	public event EventHandler<CacheClearEventArgs> OnClear;

	/// <inheritdoc />
	public event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

	/// <inheritdoc />
	public event EventHandler<CacheActionEventArgs> OnGet;

	/// <inheritdoc />
	public event EventHandler<CacheActionEventArgs> OnPut;

	/// <inheritdoc />
	public event EventHandler<CacheActionEventArgs> OnRemove;

	/// <inheritdoc />
	public event EventHandler<CacheItemRemovedEventArgs> OnRemoveByHandle;

	/// <inheritdoc />
	public event EventHandler<CacheActionEventArgs> OnUpdate;

	/// <inheritdoc />
	public CacheManagerConfiguration Configuration { get; }

	/// <inheritdoc />
	public IEnumerable<BaseCacheHandle<TValue>> CacheHandles
		=> _cacheHandlesCollection;

	/// <summary>
	/// 获取配置的缓存背板。
	/// </summary>
	/// <value>背板实例。</value>
	public CacheBackplane Backplane => _cacheBackplane;

	/// <summary>
	/// 获取缓存名称。
	/// </summary>
	/// <value>缓存的名称。</value>
	public string Name { get; }

	/// <inheritdoc />
	public override void Clear()
	{
		CheckDisposed();

		foreach (var handle in _cacheHandles)
		{
			handle.Clear();
			handle.Stats.OnClear();
		}

		_cacheBackplane?.NotifyClear();

		TriggerOnClear();
	}

	/// <inheritdoc />
	public override void ClearRegion(string region)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		CheckDisposed();

		foreach (var handle in _cacheHandles)
		{
			handle.ClearRegion(region);
			handle.Stats.OnClearRegion(region);
		}

		_cacheBackplane?.NotifyClearRegion(region);

		TriggerOnClearRegion(region);
	}

	/// <inheritdoc />
	public override bool Exists(string key)
	{
		return _cacheHandles.Any(handle => handle.Exists(key));
	}

	/// <inheritdoc />
	public override bool Exists(string key, string region)
	{
		return _cacheHandles.Any(handle => handle.Exists(key, region));
	}

	/// <summary>
	/// 返回当前实例的字符串表示形式。
	/// </summary>
	/// <returns>
	/// 表示此实例的 <see cref="string"/>。
	/// </returns>
	public override string ToString() =>
		string.Format(CultureInfo.InvariantCulture, "Name: {0}, Handles: [{1}]", Name, string.Join(",", _cacheHandles.Select(p => p.GetType().Name)));

	/// <inheritdoc />
	protected override bool AddInternal(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		CheckDisposed();

		var handleIndex = _cacheHandles.Length - 1;

		var result = AddItemToHandle(item, _cacheHandles[handleIndex]);

		// 无论何种情况都从其他句柄中逐出，因为如果该项存在，可能是不同的版本；
		// 如果不存在，这只是用于使上层其他版本失效的一次健全性检查。
		EvictFromOtherHandles(item.Key, item.Region, handleIndex);

		if (result)
		{
			// 更新背板
			if (_cacheBackplane != null)
			{
				if (string.IsNullOrWhiteSpace(item.Region))
				{
					_cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Add);
				}
				else
				{
					_cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Add);
				}
			}

			// 仅触发一次而非每个句柄触发一次，且仅在项被添加时触发！
			TriggerOnAdd(item.Key, item.Region);
		}

		return result;
	}

	/// <inheritdoc />
	protected override void PutInternal(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		CheckDisposed();

		foreach (var handle in _cacheHandles)
		{
			if (handle.Configuration.EnableStatistics)
			{
				// 检查是否确实为新项，否则条目计数会失真，因为我们每次都计数，
				// 但只使用当前句柄来检索项，否则会触发获取操作并可能在另一个句柄中找到它
				var oldItem = string.IsNullOrWhiteSpace(item.Region) ? handle.GetCacheItem(item.Key) : handle.GetCacheItem(item.Key, item.Region);

				handle.Stats.OnPut(item, oldItem == null);
			}

			handle.Put(item);
		}

		// 更新背板
		if (_cacheBackplane != null)
		{
			if (string.IsNullOrWhiteSpace(item.Region))
			{
				_cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Put);
			}
			else
			{
				_cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Put);
			}
		}

		TriggerOnPut(item.Key, item.Region);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposeManaged)
	{
		if (disposeManaged)
		{
			foreach (var handle in _cacheHandles)
			{
				handle.Dispose();
			}

			_cacheBackplane?.Dispose();
		}

		base.Dispose(disposeManaged);
	}

	/// <inheritdoc />
	protected override CacheItem<TValue> GetCacheItemInternal(string key) =>
		GetCacheItemInternal(key, null);

	/// <inheritdoc />
	protected override CacheItem<TValue> GetCacheItemInternal(string key, string region)
	{
		CheckDisposed();

		CacheItem<TValue> cacheItem = null;

		for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
		{
			var handle = _cacheHandles[handleIndex];
			cacheItem = string.IsNullOrWhiteSpace(region) ? handle.GetCacheItem(key) : handle.GetCacheItem(key, region);

			handle.Stats.OnGet(region);

			if (cacheItem != null)
			{
				cacheItem.LastAccessedUtc = DateTime.UtcNow;

				// 如有需要则更新其他句柄
				AddToHandles(cacheItem, handleIndex);
				handle.Stats.OnHit(region);
				TriggerOnGet(key, region);
				break;
			}
			else
			{
				handle.Stats.OnMiss(region);
			}
		}

		return cacheItem;
	}

	/// <inheritdoc />
	protected override bool RemoveInternal(string key) =>
		RemoveInternal(key, null);

	/// <inheritdoc />
	protected override bool RemoveInternal(string key, string region)
	{
		CheckDisposed();

		var result = false;

		foreach (var handle in _cacheHandles)
		{
			var handleResult = !string.IsNullOrWhiteSpace(region) ? handle.Remove(key, region) : handle.Remove(key);

			if (!handleResult)
			{
				continue;
			}

			result = true;
			handle.Stats.OnRemove(region);
		}

		if (result)
		{
			// 更新背板
			if (_cacheBackplane != null)
			{
				if (string.IsNullOrWhiteSpace(region))
				{
					_cacheBackplane.NotifyRemove(key);
				}
				else
				{
					_cacheBackplane.NotifyRemove(key, region);
				}
			}

			// 仅触发一次而非每个句柄触发一次
			TriggerOnRemove(key, region);
		}

		return result;
	}

	/// <summary>
	/// 尝试将缓存项添加到指定的缓存句柄，并在成功时更新其统计信息。
	/// </summary>
	/// <param name="item">要添加的缓存项。</param>
	/// <param name="handle">目标缓存句柄。</param>
	/// <returns>如果添加成功，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	private static bool AddItemToHandle(CacheItem<TValue> item, BaseCacheHandle<TValue> handle)
	{
		if (handle.Add(item))
		{
			handle.Stats.OnAdd(item);
			return true;
		}

		return false;
	}

	/// <summary>
	/// 清空指定的缓存句柄集合。
	/// </summary>
	/// <param name="handles">要清空的缓存句柄集合。</param>
	private static void ClearHandles(IEnumerable<BaseCacheHandle<TValue>> handles)
	{
		foreach (var handle in handles)
		{
			handle.Clear();
			handle.Stats.OnClear();
		}

		////this.TriggerOnClear();
	}

	/// <summary>
	/// 清空指定缓存句柄集合中对应区域的数据。
	/// </summary>
	/// <param name="region">要清空的区域名称。</param>
	/// <param name="handles">要清空的缓存句柄集合。</param>
	private static void ClearRegionHandles(string region, IEnumerable<BaseCacheHandle<TValue>> handles)
	{
		foreach (var handle in handles)
		{
			handle.ClearRegion(region);
			handle.Stats.OnClearRegion(region);
		}

		////this.TriggerOnClearRegion(region);
	}

	/// <summary>
	/// 从指定的缓存句柄集合中逐出指定键的数据。
	/// </summary>
	/// <param name="key">要逐出的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="handles">要逐出的缓存句柄集合。</param>
	private static void EvictFromHandles(string key, string region, IEnumerable<BaseCacheHandle<TValue>> handles)
	{
		foreach (var handle in handles)
		{
			EvictFromHandle(key, region, handle);
		}
	}

	/// <summary>
	/// 从指定的缓存句柄中逐出指定键的数据。
	/// </summary>
	/// <param name="key">要逐出的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="handle">要逐出的缓存句柄。</param>
	private static void EvictFromHandle(string key, string region, BaseCacheHandle<TValue> handle)
	{
		var result = string.IsNullOrWhiteSpace(region) ? handle.Remove(key) : handle.Remove(key, region);

		if (result)
		{
			handle.Stats.OnRemove(region);
		}
	}

	/// <summary>
	/// 将缓存项添加到找到该项的句柄之前（顺序更靠前）的所有句柄中。
	/// </summary>
	/// <param name="item">要添加的缓存项。</param>
	/// <param name="foundIndex">找到该项的句柄索引。</param>
	private void AddToHandles(CacheItem<TValue> item, int foundIndex)
	{
		if (foundIndex == 0)
		{
			return;
		}

		// 更新列表中顺序更靠前的所有缓存句柄
		for (var handleIndex = 0; handleIndex < foundIndex; handleIndex++)
		{
			_cacheHandles[handleIndex].Add(item);
		}
	}

	/// <summary>
	/// 将缓存项添加到找到该项的句柄之后（顺序更靠后）的所有句柄中。
	/// </summary>
	/// <param name="item">要添加的缓存项。</param>
	/// <param name="foundIndex">找到该项的句柄索引。</param>
	private void AddToHandlesBelow(CacheItem<TValue> item, int foundIndex)
	{
		if (item == null)
		{
			return;
		}

		for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
		{
			if (handleIndex <= foundIndex)
			{
				continue;
			}

			if (_cacheHandles[handleIndex].Add(item))
			{
				_cacheHandles[handleIndex].Stats.OnAdd(item);
			}
		}
	}

	/// <summary>
	/// 从除指定索引外的其他所有缓存句柄中逐出指定键的数据。
	/// </summary>
	/// <param name="key">要逐出的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="excludeIndex">要排除的句柄索引。</param>
	/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="excludeIndex"/> 超出句柄数组范围时抛出。</exception>
	private void EvictFromOtherHandles(string key, string region, int excludeIndex)
	{
		if (excludeIndex < 0 || excludeIndex >= _cacheHandles.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(excludeIndex));
		}

		for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
		{
			if (handleIndex != excludeIndex)
			{
				EvictFromHandle(key, region, _cacheHandles[handleIndex]);
			}
		}
	}

	/// <summary>
	/// 从指定索引之前（顺序更靠前）的所有缓存句柄中逐出指定键的数据。
	/// </summary>
	/// <param name="key">要逐出的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="excludeIndex">作为边界的句柄索引，仅逐出其之前的句柄。</param>
	/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="excludeIndex"/> 超出句柄数组范围时抛出。</exception>
	private void EvictFromHandlesAbove(string key, string region, int excludeIndex)
	{
		if (excludeIndex < 0 || excludeIndex >= _cacheHandles.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(excludeIndex));
		}

		for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
		{
			if (handleIndex < excludeIndex)
			{
				EvictFromHandle(key, region, _cacheHandles[handleIndex]);
			}
		}
	}

	/// <summary>
	/// 注册缓存背板，并为其订阅 Changed、Removed、Cleared 与 ClearedRegion 事件，
	/// 以将远程发生的缓存变更同步到本地句柄。
	/// </summary>
	/// <param name="backplane">要注册的缓存背板。</param>
	private void RegisterCacheBackplane(CacheBackplane backplane)
	{
		Check.EnsureNotNull(backplane, nameof(backplane));

		// 此检查本应在激活时已完成，此处仅为完全确保。
		if (_cacheHandles.Any(p => p.Configuration.IsBackplaneSource))
		{
			// 增加 includeSource 参数以获取需要同步的句柄。
			// 当背板源为非分布式（内存中）缓存时，仅远程触发的删除与清空操作也应在本地触发同步。
			// 对于分布式缓存，我们期望分布式缓存已是同步的源——它本身就是触发事件的那一层。
			// 在这种情况下，仅位于分布式缓存之上的其他内存句柄会被同步。
			var handles = new Func<bool, BaseCacheHandle<TValue>[]>(includeSource =>
			{
				var handleList = new List<BaseCacheHandle<TValue>>();
				foreach (var handle in _cacheHandles)
				{
					if (!handle.Configuration.IsBackplaneSource ||
					    (includeSource && handle.Configuration.IsBackplaneSource && !handle.IsDistributedCache))
					{
						handleList.Add(handle);
					}
				}

				return handleList.ToArray();
			});

			backplane.Changed += (_, args) =>
			{
				EvictFromHandles(args.Key, args.Region, handles(false));
				switch (args.Action)
				{
					case CacheItemChangedEventAction.Add:
						TriggerOnAdd(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
						break;

					case CacheItemChangedEventAction.Put:
						TriggerOnPut(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
						break;

					case CacheItemChangedEventAction.Update:
						TriggerOnUpdate(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
						break;
				}
			};

			backplane.Removed += (_, args) =>
			{
				EvictFromHandles(args.Key, args.Region, handles(true));
				TriggerOnRemove(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
			};

			backplane.Cleared += (_, _) =>
			{
				ClearHandles(handles(true));
				TriggerOnClear(CacheActionEventArgOrigin.Remote);
			};

			backplane.ClearedRegion += (_, args) =>
			{
				ClearRegionHandles(args.Region, handles(true));
				TriggerOnClearRegion(args.Region, CacheActionEventArgOrigin.Remote);
			};
		}
	}

	/// <summary>
	/// 触发 <see cref="OnAdd"/> 事件。
	/// </summary>
	/// <param name="key">被添加的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnAdd(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnAdd?.Invoke(this, new CacheActionEventArgs(key, region, origin));
	}

	/// <summary>
	/// 触发 <see cref="OnClear"/> 事件。
	/// </summary>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnClear(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnClear?.Invoke(this, new CacheClearEventArgs(origin));
	}

	/// <summary>
	/// 触发 <see cref="OnClearRegion"/> 事件。
	/// </summary>
	/// <param name="region">被清空的区域名称。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnClearRegion(string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnClearRegion?.Invoke(this, new CacheClearRegionEventArgs(region, origin));
	}

	/// <summary>
	/// 触发 <see cref="OnGet"/> 事件。
	/// </summary>
	/// <param name="key">被获取的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnGet(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnGet?.Invoke(this, new CacheActionEventArgs(key, region, origin));
	}

	/// <summary>
	/// 触发 <see cref="OnPut"/> 事件。
	/// </summary>
	/// <param name="key">被更新的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnPut(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnPut?.Invoke(this, new CacheActionEventArgs(key, region, origin));
	}

	/// <summary>
	/// 触发 <see cref="OnRemove"/> 事件。
	/// </summary>
	/// <param name="key">被移除的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnRemove(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		OnRemove?.Invoke(this, new CacheActionEventArgs(key, region, origin));
	}

	/// <summary>
	/// 触发 <see cref="OnRemoveByHandle"/> 事件。
	/// </summary>
	/// <param name="key">被移除的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="reason">缓存项被移除的原因。</param>
	/// <param name="level">移除发生时所在的句柄层级。</param>
	/// <param name="value">被移除的缓存项值。</param>
	private void TriggerOnRemoveByHandle(string key, string region, CacheItemRemovedReason reason, int level, object value)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		OnRemoveByHandle?.Invoke(this, new CacheItemRemovedEventArgs(key, region, reason, value, level));
	}

	/// <summary>
	/// 触发 <see cref="OnUpdate"/> 事件。
	/// </summary>
	/// <param name="key">被更新的缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="origin">事件来源（本地或远程）。</param>
	private void TriggerOnUpdate(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
	{
		OnUpdate?.Invoke(this, new CacheActionEventArgs(key, region, origin));
	}
}