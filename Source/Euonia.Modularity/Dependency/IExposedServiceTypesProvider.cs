namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 公开服务类型提供程序的契约。
/// </summary>
public interface IExposedServiceTypesProvider
{
    /// <summary>
    /// 获取给定目标类型的公开服务类型。
    /// </summary>
    /// <param name="targetType">要获取公开服务类型的目标类型。</param>
    /// <returns>该目标类型对外公开的服务类型集合。</returns>
    IEnumerable<Type> GetExposedServiceTypes(Type targetType);
}
