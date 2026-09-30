using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 属性信息管理器类。
/// </summary>
public static class PropertyInfoManager
{
	/// <summary>
	/// 存储对象类型与其属性列表映射的缓存。
	/// </summary>
	/// <remarks>
	/// 用 <see cref="ConcurrentDictionary{TKey,TValue}"/> 是因为快路径（已初始化的类型）是完全无锁的读；
	/// 普通 <see cref="Dictionary{TKey,TValue}"/> 在一个线程 <c>Add</c> 时被另一个线程 <c>TryGetValue</c>
	/// 会直接损坏内部状态（原实现第 25 行的读就在锁外）。
	/// </remarks>
	private static readonly ConcurrentDictionary<Type, PropertyInfoList> _propertyCache = new();

	/// <summary>
	/// 保护「创建列表 → 静态初始化 → 发布到缓存」这一段的互斥。
	/// </summary>
	/// <remarks>
	/// 不能只靠 <see cref="ConcurrentDictionary{TKey,TValue}"/>：<see cref="FieldDataManager.ForceStaticFieldInit(Type)"/>
	/// 会触发类型的静态初始化器，而初始化器反过来会重入 <see cref="GetPropertyListCache(Type)"/> 并要求拿到
	/// <strong>同一条</strong>列表（否则属性会写进另一份副本，形成分裂）。
	/// 因此必须「先占位、后初始化、初始化完成才发布」，而重入发生在同一线程上，
	/// 用线程静态的 <see cref="_initializing"/> 让它认领占位即可。
	/// </remarks>
	private static readonly object _publishLock = new();

	/// <summary>
	/// 当前线程正在初始化、<strong>尚未发布</strong>到 <see cref="_propertyCache"/> 的列表。
	/// 线程静态 ⇒ 读它无需加锁。
	/// </summary>
	[ThreadStatic]
	private static Dictionary<Type, PropertyInfoList> _initializing;

	/// <summary>
	/// 获取对象类型的属性列表缓存；若不存在则创建并完成一次静态初始化。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <returns>属性列表缓存。</returns>
	internal static PropertyInfoList GetPropertyListCache(Type objectType)
	{
		// 快路径 1：已发布 ⇒ 初始化早已完成，无锁读是安全的。
		if (_propertyCache.TryGetValue(objectType, out var listInfo))
		{
			return listInfo;
		}

		// 快路径 2：本线程正持有占位（静态初始化器回调重入），直接认领。
		if (_initializing?.TryGetValue(objectType, out listInfo) == true)
		{
			return listInfo;
		}

		return CreateAndPublish(objectType);
	}

	/// <summary>
	/// 创建列表、执行一次静态初始化，然后才发布到缓存。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <returns>属性列表缓存。</returns>
	private static PropertyInfoList CreateAndPublish(Type objectType)
	{
		lock (_publishLock)
		{
			// 拿到锁后可能已被别的线程完成。
			if (_propertyCache.TryGetValue(objectType, out var listInfo))
			{
				return listInfo;
			}

			listInfo = new PropertyInfoList();

			_initializing ??= new Dictionary<Type, PropertyInfoList>();
			_initializing[objectType] = listInfo;

			try
			{
				FieldDataManager.ForceStaticFieldInit(objectType);
			}
			finally
			{
				_initializing.Remove(objectType);
			}

			// 初始化完成才发布：其他线程读到缓存条目时，属性注册必然已经结束。
			_propertyCache[objectType] = listInfo;

			return listInfo;
		}
	}

	/// <summary>
	/// 取属性列表的快照，并把列表置为锁定状态（禁止后续注册）。
	/// </summary>
	/// <remarks>
	/// 置锁与复制必须在同一次持锁内完成：否则 <see cref="RegisterProperty{T}(Type, PropertyInfo{T})"/>
	/// 可能读到 <see cref="PropertyInfoList.IsLocked"/> 为 <see langword="false"/>，
	/// 于是在快照复制的中途插入元素，导致 <c>AddRange</c> 抛「集合已修改」。
	/// </remarks>
	/// <param name="objectType">对象类型。</param>
	/// <returns>属性列表快照。</returns>
	internal static PropertyInfoList GetLockedSnapshot(Type objectType)
	{
		var list = GetPropertyListCache(objectType);
		lock (list)
		{
			list.IsLocked = true;
			return new PropertyInfoList(list);
		}
	}

	/// <summary>
	/// 获取已注册的属性。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <returns>已注册属性的列表。</returns>
	public static PropertyInfoList GetRegisteredProperties(Type objectType)
	{
		var list = GetPropertyListCache(objectType);
		lock (list)
		{
			return new PropertyInfoList(list);
		}
	}

	/// <summary>
	/// 获取已注册的属性。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>匹配的属性信息；如果未找到则为 <c>null</c>。</returns>
	public static IPropertyInfo GetRegisteredProperty(Type objectType, string propertyName)
	{
		return GetRegisteredProperties(objectType).FirstOrDefault(p => p.Name == propertyName);
	}

	/// <summary>
	/// 在对象类型的属性列表中注册属性，并按名称排序插入。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <param name="info">要注册的属性信息。</param>
	/// <returns>注册的属性信息。</returns>
	internal static PropertyInfo<T> RegisterProperty<T>(Type objectType, PropertyInfo<T> info)
	{
		var list = GetPropertyListCache(objectType);
		lock (list)
		{
			if (list.IsLocked)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_PROPERTY_LIST_LOCKED, objectType.FullName));
			}

			var index = list.BinarySearch(info, new PropertyComparer());

			if (index >= 0)
			{
				throw new InvalidOperationException(string.Format(Resources.IDS_PROPERTY_ALREADY_REGISTERED, info.Name, objectType.FullName));
			}

			// 在正确的排序索引处插入属性信息
			list.Insert(~index, info);
		}

		return info;
	}
}