using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up business object related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds business object related services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
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