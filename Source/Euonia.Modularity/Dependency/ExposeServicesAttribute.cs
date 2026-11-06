using System.Reflection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 公开类的所有公共服务。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExposeServicesAttribute : Attribute, IExposedServiceTypesProvider
{
    /// <summary>
    /// 公开的服务类型列表。
    /// </summary>
    public Type[] ServiceTypes { get; }

    /// <summary>
    /// 获取或设置一个值，指示是否包含默认服务。
    /// </summary>
    public bool IncludeDefaults { get; set; }

    /// <summary>
    /// 获取或设置一个值，指示是否包含自身。
    /// </summary>
    public bool IncludeSelf { get; set; }

    /// <summary>
    /// 初始化 <see cref="ExposeServicesAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="serviceTypes">要公开的服务类型；为 <see langword="null"/> 时使用空数组。</param>
    public ExposeServicesAttribute(params Type[] serviceTypes)
    {
        ServiceTypes = serviceTypes ?? Array.Empty<Type>();
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetExposedServiceTypes(Type targetType)
    {
        var serviceList = ServiceTypes.ToList();

        if (IncludeDefaults)
        {
            foreach (var type in GetDefaultServices(targetType))
            {
                serviceList.AddIfNotContains(type);
            }

            if (IncludeSelf)
            {
                serviceList.AddIfNotContains(targetType);
            }
        }
        else if (IncludeSelf)
        {
            serviceList.AddIfNotContains(targetType);
        }

        return serviceList.ToArray();
    }

    private static List<Type> GetDefaultServices(Type type)
    {
        var serviceTypes = new List<Type>();

        foreach (var interfaceType in type.GetTypeInfo().GetInterfaces())
        {
            var interfaceName = interfaceType.Name;

            if (interfaceName.StartsWith("I"))
            {
                interfaceName = interfaceName.Right(interfaceName.Length - 1);
            }

            if (type.Name.EndsWith(interfaceName))
            {
                serviceTypes.Add(interfaceType);
            }
        }

        return serviceTypes;
    }
}