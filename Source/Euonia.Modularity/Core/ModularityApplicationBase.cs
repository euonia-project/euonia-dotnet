using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块化应用程序的抽象基类，负责加载模块、配置服务并管理应用程序生命周期。
/// </summary>
public abstract class ModularityApplicationBase : IModularityApplication
{
    /// <summary>
    /// 初始化模块化应用程序，注册核心服务、加载模块并执行服务配置。
    /// </summary>
    /// <param name="startupModuleType">启动模块类型，作为模块依赖图的根节点。</param>
    /// <param name="services">应用程序的服务集合。</param>
    /// <param name="configuration">应用程序配置；为 <c>null</c> 时后续从服务提供程序解析。</param>
    /// <param name="optionsAction">用于自定义应用程序创建选项的委托，可为 <c>null</c>。</param>
    internal ModularityApplicationBase(Type startupModuleType, IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction)
    {
        StartupModuleType = startupModuleType;
        Services = services;
        Configuration = configuration!;

        services.TryAddObjectAccessor<IServiceProvider>();

        var options = new ApplicationCreationOptions(services);
        optionsAction?.Invoke(options);

        services.AddSingleton<IModularityApplication>(this);
        services.AddSingleton<IModuleContainer>(this);
        services.TryAddScoped<IServiceAccessor, ServiceAccessor>();
        services.TryAddScoped<DefaultRequestContextAccessor>();
        services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
        services.AddCoreServices();
        services.AddCoreServices(this, options);

        // ReSharper disable once VirtualMemberCallInConstructor
        Modules = LoadModules(services, options);
        // ReSharper disable once VirtualMemberCallInConstructor
        ConfigureServices();
    }

    /// <summary>
    /// 获取当前应用程序的启动模块类型。
    /// </summary>
    public Type StartupModuleType { get; }

    /// <summary>
    /// 获取当前应用程序的服务集合。
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 获取当前应用程序的服务提供程序。
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 获取当前应用程序的配置上下文实例。
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// 获取已注册的模块。
    /// </summary>
    public IReadOnlyList<IModuleDescriptor> Modules { get; }

    /// <summary>
    /// 释放应用程序。
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 关闭应用程序并卸载所有模块。
    /// </summary>
    public void Shutdown()
    {
        using var scope = ServiceProvider.CreateScope();
        scope.ServiceProvider
             .GetRequiredService<IModuleManager>()
             .UnloadModules(new ApplicationShutdownContext(scope.ServiceProvider));
    }

    /// <summary>
    /// 获取应用程序服务提供程序。
    /// </summary>
    /// <param name="serviceProvider">应用程序使用的服务提供程序。</param>
    protected virtual void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        ServiceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>().Value = ServiceProvider;
        Configuration ??= serviceProvider.GetService<IConfiguration>()!;
    }

    /// <summary>
    /// 初始化依赖模块。
    /// </summary>
    protected virtual void InitializeModules()
    {
        using var scope = ServiceProvider.CreateScope();
        scope.ServiceProvider
             .GetRequiredService<IModuleManager>()
             .InitializeModules(new ApplicationInitializationContext(scope.ServiceProvider));
    }

    /// <summary>
    /// 加载依赖模块。
    /// </summary>
    /// <param name="services">用于加载模块的服务集合。</param>
    /// <param name="options">应用程序创建选项。</param>
    /// <returns>已加载的模块描述符列表。</returns>
    protected virtual IReadOnlyList<IModuleDescriptor> LoadModules(IServiceCollection services, ApplicationCreationOptions options)
    {
        return services.GetSingletonInstance<IModuleLoader>()
                       .LoadModules(services, StartupModuleType);
    }

    /// <summary>
    /// 配置应用程序的服务。
    /// </summary>
    /// <exception cref="Exception">任一模块在服务配置生命周期阶段抛出异常时，包装并附带阶段与模块信息后抛出。</exception>
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
                throw new Exception(string.Format(Resources.IDS_MODULE_LIFECYCLE_PHASE_FAILED, nameof(ModuleContextBase.AheadConfigureServices), module.Type.AssemblyQualifiedName), exception);
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
                throw new Exception(string.Format(Resources.IDS_MODULE_LIFECYCLE_PHASE_FAILED, nameof(ModuleContextBase.ConfigureServices), module.Type.AssemblyQualifiedName), exception);
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
                throw new Exception(string.Format(Resources.IDS_MODULE_LIFECYCLE_PHASE_FAILED, nameof(ModuleContextBase.AfterConfigureServices), module.Type.AssemblyQualifiedName), exception);
            }
        }

        foreach (var module in Modules)
        {
            if (module.Instance is ModuleContextBase moduleContext)
            {
                moduleContext.ConfigurationContext = null!;
            }
        }
    }
}