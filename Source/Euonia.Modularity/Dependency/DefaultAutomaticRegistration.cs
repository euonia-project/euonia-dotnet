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
    /// <returns>如果该类型应跳过自动注册则返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    /// <remarks>默认实现会检查类型上是否标记了 <see cref="DisableAutomaticRegistrationAttribute"/>。</remarks>
    protected virtual bool IsAutomaticRegistrationDisabled(Type type)
    {
        return type.IsDefined(typeof(DisableAutomaticRegistrationAttribute), true);
    }

    /// <inheritdoc />
    public override void AddType(IServiceCollection services, Type type)
    {
        Check.EnsureNotNull(services, nameof(services));

        if (type == null || !IsRegistrableType(type) || IsAutomaticRegistrationDisabled(type))
        {
            return;
        }

        var attributes = GetDependencyAttributes(type);
        var registeredServiceTypes = new HashSet<Type>();

        if (attributes.Count == 0)
        {
            RegisterServices(services, type, null, registeredServiceTypes);
            return;
        }

        foreach (var attribute in attributes)
        {
            RegisterServices(services, type, attribute, registeredServiceTypes);
        }
    }

    private void RegisterServices(IServiceCollection services, Type implementationType, ExportServiceAttribute? attribute, ISet<Type> registeredServiceTypes)
    {
        var lifeTime = GetLifeTime(implementationType, attribute);
        if (lifeTime == null)
        {
            return;
        }

        var exposedServiceTypes = attribute?.ServiceTypes is { Count: > 0 }
            ? new List<Type>(attribute.ServiceTypes)
            : GetExposedServiceTypes(implementationType);

        if (attribute?.IncludeSelf == true)
        {
            exposedServiceTypes.AddIfNotContains(implementationType);
        }

        exposedServiceTypes = exposedServiceTypes
                              .Where(serviceType => serviceType != null)
                              .Distinct()
                              .ToList();

        if (exposedServiceTypes.Count == 0)
        {
            return;
        }

        TriggerServiceExposing(services, implementationType, exposedServiceTypes);
        TriggerServiceRegistration(services, implementationType, lifeTime.Value, exposedServiceTypes);

        exposedServiceTypes = exposedServiceTypes
                              .Where(serviceType => serviceType != null)
                              .Distinct()
                              .ToList();

        foreach (var serviceType in exposedServiceTypes)
        {
            ValidateServiceType(implementationType, serviceType);
        }

        foreach (var serviceType in exposedServiceTypes)
        {
            if (!registeredServiceTypes.Add(serviceType))
            {
                continue;
            }

            var descriptor = CreateServiceDescriptor(serviceType, implementationType, exposedServiceTypes, lifeTime.Value);

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

    private static void ValidateServiceType(Type implementationType, Type serviceType)
    {
        if (!serviceType.IsAssignableFrom(implementationType))
        {
            throw new InvalidOperationException(string.Format(Resources.IDS_IMPLEMENTATION_NOT_INHERIT_SERVICE, implementationType.FullName, serviceType.FullName));
        }
    }
}
