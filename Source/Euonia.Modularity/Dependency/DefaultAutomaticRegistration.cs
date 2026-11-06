using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 默认自动注册。
/// </summary>
public class DefaultAutomaticRegistration : AutomaticRegistrationBase
{
    /// <summary>
    /// 判断是否禁用指定类型的自动注册。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>如果该类型应跳过自动注册则返回 <see langword="true"/>，否则返回 <see langword="false"/>；默认实现始终返回 <see langword="false"/>。</returns>
    protected virtual bool IsAutomaticRegistrationDisabled(Type type) => false;

    /// <inheritdoc />
    public override void AddType(IServiceCollection services, Type type)
    {
        if (IsAutomaticRegistrationDisabled(type))
        {
            return;
        }

        var attribute = GetDependencyAttribute(type);
        var lifeTime = GetLifeTime(type, attribute);

        if (lifeTime == null)
        {
            return;
        }

        var exposedServiceTypes = attribute?.ServiceTypes;

        if (exposedServiceTypes == null || exposedServiceTypes.Count < 1)
        {
            exposedServiceTypes = GetExposedServiceTypes(type);
        }
        else
        {
            foreach (var serviceType in exposedServiceTypes)
            {
                if (serviceType == null)
                {
                    continue;
                }

                if (!serviceType.IsAssignableFrom(type))
                {
                    throw new InvalidOperationException(string.Format(Resources.IDS_IMPLEMENTATION_NOT_INHERIT_SERVICE, type.FullName, serviceType.FullName));
                }
            }
        }

        foreach (var exposedServiceType in exposedServiceTypes)
        {
            var descriptor = CreateServiceDescriptor(exposedServiceType, type, exposedServiceTypes, lifeTime.Value);

            if (attribute?.ReplaceServices == true)
            {
                services.Replace(descriptor);
            }
            else if (attribute?.TryRegister == true)
            {
                services.TryAdd(descriptor);
            }
            else
            {
                services.Add(descriptor);
            }
        }
    }
}