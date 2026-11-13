using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 公开服务类型的探索器。
/// </summary>
public static class ExposedServiceExplorer
{
    private static readonly ExposeServicesAttribute _defaultExposeServicesAttribute =
        new()
        {
            IncludeDefaults = true,
            IncludeSelf = true
        };

    private static readonly ConcurrentDictionary<Type, List<Type>> _cache = new();

    /// <summary>
    /// 获取指定类型的公开服务类型。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>该类型公开的服务类型列表（已去重）；当类型上未声明 <see cref="IExposedServiceTypesProvider"/> 特性时，按默认规则返回其自身及其匹配的接口。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出。</exception>
    public static List<Type> GetExposedServices(Type type)
    {
        Check.EnsureNotNull(type, nameof(type));

        var exposedServices = _cache.GetOrAdd(
            type,
            static targetType => targetType
                .GetCustomAttributes(true)
                .OfType<IExposedServiceTypesProvider>()
                .DefaultIfEmpty(_defaultExposeServicesAttribute)
                .SelectMany(provider => provider.GetExposedServiceTypes(targetType))
                .Distinct()
                .ToList());

        return new List<Type>(exposedServices);
    }
}
