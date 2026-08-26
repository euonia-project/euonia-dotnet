using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// To be added.
/// </summary>
public abstract class ModularityApplicationBase : IModularityApplication
{
    internal ModularityApplicationBase(Type startupModuleType, IServiceCollection services, IConfiguration configuration, Action<ApplicationCreationOptions> optionsAction)
    {
        StartupModuleType = startupModuleType;
        Services = services;
        Configuration = configuration;

        services.TryAddObjectAccessor<IServiceProvider>();

        var options = new ApplicationCreationOptions(services);
        optionsAction?.Invoke(options);

        services.AddSingleton<IModularityApplication>(this);
        services.AddSingleton<IModuleContainer>(this);
        services.TryAddSingleton<IServiceAccessor, ServiceAccessor>();
        services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
        services.AddCoreServices();
        services.AddCoreServices(this, options);

        // ReSharper disable once VirtualMemberCallInConstructor
        Modules = LoadModules(services, options);
        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices();
    }

    /// <summary>
    /// Gets the startup module type of current application.
    /// </summary>
    public Type StartupModuleType { get; }

    /// <summary>
    /// Gets the service collection of current application.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the service provider of current application.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Gets the configuration context instance of current application.
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Gets the registered modules.
    /// </summary>
    public IReadOnlyList<IModuleDescriptor> Modules { get; }

    /// <summary>
    /// Disposes the application.
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Shutdown()
    {
        using var scope = ServiceProvider.CreateScope();
        scope.ServiceProvider
             .GetRequiredService<IModuleManager>()
             .UnloadModules(new ApplicationShutdownContext(scope.ServiceProvider));
    }

    /// <summary>
    /// Gets the application service provider.
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected virtual void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        ServiceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>().Value = ServiceProvider;
        Configuration ??= serviceProvider.GetService<IConfiguration>();
    }

    /// <summary>
    /// Initializes the dependent modules.
    /// </summary>
    protected virtual void InitializeModules()
    {
        using var scope = ServiceProvider.CreateScope();
        scope.ServiceProvider
             .GetRequiredService<IModuleManager>()
             .InitializeModules(new ApplicationInitializationContext(scope.ServiceProvider));
    }

    /// <summary>
    /// Loads the dependent modules.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected virtual IReadOnlyList<IModuleDescriptor> LoadModules(IServiceCollection services, ApplicationCreationOptions options)
    {
        return services.GetSingletonInstance<IModuleLoader>()
                       .LoadModules(services, StartupModuleType);
    }

    /// <summary>
    /// Configures the services of the application.
    /// </summary>
    /// <exception cref="Exception"></exception>
    protected virtual void ConfigureServices()
    {
        var context = new ServiceConfigurationContext(Services);
        Services.AddSingleton(context);

        foreach (var module in Modules)
        {
            if (module.Instance is ModuleContextBase moduleContext)
            {
                moduleContext.ConfigurationContext = context;
                moduleContext.Configuration = Configuration;
            }
        }

        foreach (var module in Modules)
        {
            try
            {
                module.Instance.AheadConfigureServices(context);
            }
            catch (Exception exception)
            {
                throw new Exception($"An error occurred during {nameof(ModuleContextBase.AheadConfigureServices)} phase of the module {module.Type.AssemblyQualifiedName}. See the inner exception for details.", exception);
            }
        }

        foreach (var module in Modules)
        {
            if (module.Instance is ModuleContextBase { AutomaticRegisterService: true })
            {
                Services.AddAssembly(module.Type.Assembly);
            }

            try
            {
                module.Instance.ConfigureServices(context);
            }
            catch (Exception exception)
            {
                throw new Exception($"An error occurred during {nameof(ModuleContextBase.ConfigureServices)} phase of the module {module.Type.AssemblyQualifiedName}. See the inner exception for details.", exception);
            }
        }

        foreach (var module in Modules)
        {
            try
            {
                module.Instance.AfterConfigureServices(context);
            }
            catch (Exception exception)
            {
                throw new Exception($"An error occurred during {nameof(ModuleContextBase.AfterConfigureServices)} phase of the module {module.Type.AssemblyQualifiedName}. See the inner exception for details.", exception);
            }
        }

        foreach (var module in Modules)
        {
            if (module.Instance is ModuleContextBase moduleContext)
            {
                moduleContext.ConfigurationContext = null;
            }
        }
    }
}