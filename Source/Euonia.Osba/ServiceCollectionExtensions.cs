using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="IServiceCollection" /> 中设置业务对象相关服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 向指定的 <see cref="IServiceCollection" /> 添加业务对象相关服务。
    /// </summary>
    /// <param name="services">要注册业务对象服务的 <see cref="IServiceCollection" />。</param>
    /// <param name="assemblies">要扫描业务对象类型的程序集数组。</param>
    public static void AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddScoped<BusinessContextAccessor>();
        services.TryAddScoped<BusinessContext>();
        services.TryAddScoped<IObjectFactory, BusinessObjectFactory>();

        if (assemblies?.Length > 0)
        {
            var types = assemblies.SelectMany(assembly => assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IBusinessObject))));

            foreach (var type in types)
            {
                services.TryAddTransient(type);
            }
        }

        {
        }
    }
}