using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// The default automatic registration.
/// </summary>
public class DefaultAutomaticRegistration : AutomaticRegistrationBase
{
    /// <summary>
    /// Detect whether the specified type should register service automatically or not.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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

                if (!type.IsAssignableFrom(serviceType))
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