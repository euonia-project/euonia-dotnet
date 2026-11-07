using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块化应用程序的抽象基类，负责加载模块、配置服务并管理应用程序生命周期。
/// </summary>
public abstract class ModularityApplicationBase : IModularityApplication
{
    private int _initialized;
    private int _shutdown;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// 初始化模块化应用程序，注册核心服务、加载模块并执行服务配置。
    /// </summary>
    /// <param name="startupModuleType">启动模块类型，作为模块依赖图的根节点。</param>
    /// <param name="services">应用程序的服务集合。</param>
    /// <param name="configuration">应用程序配置；为 <c>null</c> 时按创建选项构建配置。</param>
    /// <param name="optionsAction">用于自定义应用程序创建选项的委托，可为 <c>null</c>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="startupModuleType"/> 或 <paramref name="services"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="startupModuleType"/> 不是有效的模块类型时抛出。</exception>
    internal ModularityApplicationBase(Type startupModuleType, IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction)
    {
        Check.EnsureNotNull(services, nameof(services));
        ModuleHelper.CheckModuleType(startupModuleType);

        StartupModuleType = startupModuleType;
        Services = services;

        services.TryAddObjectAccessor<IServiceProvider>();

        var options = new ApplicationCreationOptions(services);
        optionsAction?.Invoke(options);

        services.AddSingleton<IModularityApplication>(this);
        services.AddSingleton<IModuleContainer>(this);
        services.TryAddScoped<IServiceAccessor, ServiceAccessor>();
        services.TryAddScoped<DefaultRequestContextAccessor>();
        services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
        services.AddCoreServices();
        services.AddCoreServices(this, options, configuration);

        Configuration = configuration
                        ?? services.GetConfiguration()
                        ?? throw new InvalidOperationException(Resources.IDS_CONFIGURATION_NOT_AVAILABLE);

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
    /// <exception cref="InvalidOperationException">当应用程序尚未初始化时抛出。</exception>
    public IServiceProvider ServiceProvider => EnsureServiceProvider();

    /// <summary>
    /// 获取当前应用程序的配置实例。
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// 获取已注册的模块。
    /// </summary>
    public IReadOnlyList<IModuleDescriptor> Modules { get; }

    /// <summary>
    /// 获取一个值，指示模块是否已经完成初始化。
    /// </summary>
    public bool IsInitialized => Volatile.Read(ref _initialized) != 0;

    /// <summary>
    /// 获取一个值，指示应用程序是否已经关闭。
    /// </summary>
    public bool IsShutdown => Volatile.Read(ref _shutdown) != 0;

    /// <summary>
    /// 获取已设置的服务提供程序；尚未初始化时为 <see langword="null"/>。
    /// </summary>
    protected IServiceProvider? ServiceProviderOrNull => Volatile.Read(ref _serviceProvider);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 关闭应用程序并卸载所有模块。
    /// </summary>
    /// <remarks>该方法可安全地重复调用，后续调用将被忽略。</remarks>
    public void Shutdown()
    {
        if (Volatile.Read(ref _serviceProvider) == null)
        {
            return;
        }

        if (IsShutdown)
        {
            return;
        }

        using var scope = EnsureServiceProvider().CreateScope();
        var moduleManager = scope.ServiceProvider.GetRequiredService<IModuleManager>();

        if (Interlocked.Exchange(ref _shutdown, 1) != 0)
        {
            return;
        }

        moduleManager.UnloadModules(new ApplicationShutdownContext(scope.ServiceProvider));
    }

    /// <summary>
    /// 设置应用程序服务提供程序。
    /// </summary>
    /// <param name="serviceProvider">应用程序使用的服务提供程序。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="serviceProvider"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当应用程序已使用另一个服务提供程序初始化时抛出。</exception>
    protected virtual void SetServiceProvider(IServiceProvider serviceProvider)
    {
        Check.EnsureNotNull(serviceProvider, nameof(serviceProvider));

        var current = Interlocked.CompareExchange(ref _serviceProvider, serviceProvider, null);

        if (current == null)
        {
            serviceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>().Value = serviceProvider;
            return;
        }

        if (!ReferenceEquals(current, serviceProvider))
        {
            throw new InvalidOperationException(Resources.IDS_SERVICE_PROVIDER_ALREADY_SET);
        }
    }

    /// <summary>
    /// 初始化依赖模块。
    /// </summary>
    /// <remarks>该方法可安全地重复调用，后续调用将被忽略。</remarks>
    protected virtual void InitializeModules()
    {
        if (IsInitialized)
        {
            return;
        }

        using var scope = EnsureServiceProvider().CreateScope();
        var moduleManager = scope.ServiceProvider.GetRequiredService<IModuleManager>();

        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            return;
        }

        moduleManager.InitializeModules(new ApplicationInitializationContext(scope.ServiceProvider));
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

        try
        {
            InvokeModulePhase(Modules, nameof(ModuleContextBase.AheadConfigureServices), module => module.Instance.AheadConfigureServices(context));

            InvokeModulePhase(
                Modules,
                nameof(ModuleContextBase.ConfigureServices),
                module =>
                {
                    if (module.Instance is ModuleContextBase { AutomaticRegisterService: true })
                    {
                        Services.AddAssembly(module.Type.Assembly);
                    }

                    module.Instance.ConfigureServices(context);
                });

            InvokeModulePhase(Modules, nameof(ModuleContextBase.AfterConfigureServices), module => module.Instance.AfterConfigureServices(context));
        }
        finally
        {
            foreach (var module in Modules)
            {
                if (module.Instance is ModuleContextBase moduleContext)
                {
                    moduleContext.ConfigurationContext = null!;
                }
            }
        }
    }

    private static void InvokeModulePhase(IEnumerable<IModuleDescriptor> modules, string phase, Action<IModuleDescriptor> action)
    {
        foreach (var module in modules)
        {
            try
            {
                action(module);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    string.Format(Resources.IDS_MODULE_LIFECYCLE_PHASE_FAILED, phase, module.Type.AssemblyQualifiedName),
                    exception);
            }
        }
    }

    private IServiceProvider EnsureServiceProvider()
    {
        return Volatile.Read(ref _serviceProvider) ?? throw new InvalidOperationException(Resources.IDS_SERVICE_PROVIDER_UNAVAILABLE);
    }
}
