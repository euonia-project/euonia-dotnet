using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// Contains methods to create new instance of <see cref="IApplicationWithServiceProvider"/>.
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    /// Create new instance of <see cref="IApplicationWithServiceProvider"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TStartupModule"></typeparam>
    /// <returns></returns>
    public static IApplicationWithServiceProvider Create<TStartupModule>(IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction = null)
        where TStartupModule : IModuleContext
    {
        return Create(typeof(TStartupModule), services, configuration, optionsAction);
    }

    /// <summary>
    /// Create new instance of <see cref="IApplicationWithServiceProvider"/>.
    /// </summary>
    /// <param name="startupModuleType"></param>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="optionsAction"></param>
    /// <returns></returns>
    public static IApplicationWithServiceProvider Create(Type startupModuleType, IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction = null)
    {
        return new ApplicationWithServiceProvider(startupModuleType, services, configuration, optionsAction);
    }
}