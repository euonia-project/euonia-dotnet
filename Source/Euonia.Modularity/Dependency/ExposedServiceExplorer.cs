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

    /// <summary>
    /// 获取指定类型的公开服务类型。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>该类型公开的服务类型列表（已去重）；当类型上未声明 <see cref="IExposedServiceTypesProvider"/> 特性时，按默认规则返回其自身及其匹配的接口。</returns>
    public static List<Type> GetExposedServices(Type type)
    {
        return type
            .GetCustomAttributes(true)
            .OfType<IExposedServiceTypesProvider>()
            .DefaultIfEmpty(_defaultExposeServicesAttribute)
            .SelectMany(p => p.GetExposedServiceTypes(type))
            .Distinct()
            .ToList();
    }
}
