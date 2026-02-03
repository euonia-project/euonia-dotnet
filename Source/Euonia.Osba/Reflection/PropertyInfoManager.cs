namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The property information manager class.
/// </summary>
public static class PropertyInfoManager
{
    private static readonly Lazy<Dictionary<Type, PropertyInfoList>> _propertyCache = new();

    private static Dictionary<Type, PropertyInfoList> PropertyCache => _propertyCache.Value;

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
    /// Gets the registered properties.
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public static PropertyInfoList GetRegisteredProperties(Type objectType)
    {
        var list = GetPropertyListCache(objectType);
        lock (list)
        {
            return new PropertyInfoList(list);
        }
    }

    /// <summary>
    /// Gets the registered property.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static IPropertyInfo GetRegisteredProperty(Type objectType, string propertyName)
    {
        return GetRegisteredProperties(objectType).FirstOrDefault(p => p.Name == propertyName);
    }

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

            // insert info at correct sorted index
            list.Insert(~index, info);
        }

        return info;
    }
}