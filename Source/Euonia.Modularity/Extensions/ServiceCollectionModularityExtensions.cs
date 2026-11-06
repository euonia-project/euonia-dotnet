using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;
using Castle.DynamicProxy;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionModularityExtensions
{
    /// <summary>
    /// Register a modularity application context.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TStartupModel"></typeparam>
    /// <returns></returns>
    public static IApplicationWithServiceProvider AddModularityApplication<TStartupModel>(this IServiceCollection services, IConfiguration? configuration = null, Action<ApplicationCreationOptions>? optionsAction = null)
        where TStartupModel : class, IModuleContext
    {
        return ApplicationFactory.Create<TStartupModel>(services, configuration, optionsAction);
    }

    internal static void AddCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ProxyGenerator>();
        services.AddTransient<ILazyServiceProvider, LazyServiceProvider>();
        services.AddOptions();
    }

    internal static void AddCoreServices(this IServiceCollection services, IModularityApplication application, ApplicationCreationOptions creationOptions)
    {
        var moduleLoader = new ModuleLoader();
        var assemblyFinder = new AssemblyFinder(application);
        var typeFinder = new TypeFinder(assemblyFinder);

        if (!services.IsAdded<IConfiguration>())
        {
            services.ReplaceConfiguration(
                ConfigurationHelper.BuildConfiguration(
                    creationOptions.Configuration
                )
            );
        }

        services.TryAddSingleton<IModuleLoader>(moduleLoader);
        services.TryAddSingleton<IAssemblyFinder>(assemblyFinder);
        services.TryAddSingleton<ITypeFinder>(typeFinder);

        services.AddAssemblyOf<IModularityApplication>();

        //services.AddTransient(typeof(ISimpleStateCheckerManager<>), typeof(SimpleStateCheckerManager<>));

        services.Configure<ModuleLifecycleOptions>(options =>
        {
            options.Lifecycle.Add<OnApplicationInitializationModuleLifecycle>();
            options.Lifecycle.Add<OnApplicationShutdownModuleLifecycle>();
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ReplaceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        return services.Replace(ServiceDescriptor.Singleton(configuration));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IConfiguration? GetConfiguration(this IServiceCollection services)
    {
        var hostBuilderContext = services.GetSingletonInstanceOrNull<HostBuilderContext>();
        if (hostBuilderContext?.Configuration != null)
        {
            return hostBuilderContext.Configuration as IConfigurationRoot;
        }

        return services.GetSingletonInstance<IConfiguration>();
    }
}