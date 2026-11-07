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
        var serviceList = new List<Type>(ServiceTypes.Length + 1);

        foreach (var serviceType in ServiceTypes)
        {
            if (serviceType != null)
            {
                serviceList.AddIfNotContains(serviceType);
            }
        }

        if (IncludeDefaults)
        {
            foreach (var serviceType in GetDefaultServices(targetType))
            {
                serviceList.AddIfNotContains(serviceType);
            }
        }

        if (IncludeSelf)
        {
            serviceList.AddIfNotContains(targetType);
        }

        return serviceList;
    }

    private static List<Type> GetDefaultServices(Type type)
    {
        var serviceTypes = new List<Type>();
        var targetName = TrimGenericArity(type.Name);

        foreach (var interfaceType in type.GetTypeInfo().GetInterfaces())
        {
            var interfaceName = TrimGenericArity(interfaceType.Name);

            if (interfaceName.StartsWith("I", StringComparison.Ordinal))
            {
                interfaceName = interfaceName.Right(interfaceName.Length - 1);
            }

            if (targetName.EndsWith(interfaceName, StringComparison.Ordinal))
            {
                serviceTypes.Add(interfaceType);
            }
        }

        return serviceTypes;
    }

    private static string TrimGenericArity(string name)
    {
        var arityIndex = name.IndexOf('`');
        return arityIndex >= 0 ? name.Left(arityIndex) : name;
    }
}
