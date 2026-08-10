namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 属性信息管理器类。
/// </summary>
public static class PropertyInfoManager
{
	/// <summary>
	/// 存储对象类型与其属性列表映射的缓存。
	/// </summary>
	private static readonly Lazy<Dictionary<Type, PropertyInfoList>> _propertyCache = new();

	/// <summary>
	/// 获取属性缓存字典。
	/// </summary>
	private static Dictionary<Type, PropertyInfoList> PropertyCache => _propertyCache.Value;

	/// <summary>
	/// 获取对象类型的属性列表缓存；若不存在则创建。
	/// </summary>
	/// <param name="objectType">对象类型。</param>
	/// <returns>属性列表缓存。</returns>
	internal static PropertyInfoList GetPropertyListCache(Type objectType)
	{
		var found = PropertyCache.TryGetValue(objectType, out var listInfo);

		if (!found)
		{
			lock (_propertyCache)
			{
				if (!PropertyCache.TryGetValue(objectType, out listInfo))
				{
					listInfo = new PropertyInfoList();
					PropertyCache.Add(objectType, listInfo);
					FieldDataManager.ForceStaticFieldInit(objectType);
				}
			}
		}

		{
		}
		return listInfo;
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
				throw new InvalidOperationException();
			}

			var index = list.BinarySearch(info, new PropertyComparer());

			if (index >= 0)
			{
				throw new InvalidOperationException();
			}

			// 在正确的排序索引处插入属性信息
			list.Insert(~index, info);
		}

		return info;
	}
}