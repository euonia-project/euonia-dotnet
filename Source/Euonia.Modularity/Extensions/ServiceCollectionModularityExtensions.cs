using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Modularity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供模块化应用程序的注册与配置扩展方法。
/// </summary>
public static class ServiceCollectionModularityExtensions
{
    /// <summary>
    /// 注册模块化应用程序上下文。
    /// </summary>
    /// <typeparam name="TStartupModel">启动模块类型，必须为实现了 <see cref="IModuleContext"/> 的类，应用程序从该类型开始解析模块依赖。</typeparam>
    /// <param name="services">服务集合，应用程序及其各模块的服务将注册到该集合中。</param>
    /// <param name="configuration">应用程序配置；为 <see langword="null"/> 时按创建选项构建配置。</param>
    /// <param name="optionsAction">用于自定义 <see cref="ApplicationCreationOptions"/> 的委托；为 <see langword="null"/> 时使用默认选项。</param>
    /// <returns>已创建的 <see cref="IApplicationWithServiceProvider"/> 实例，后续需调用其 <see cref="IApplicationWithServiceProvider.Initialize"/> 完成初始化。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <typeparamref name="TStartupModel"/> 不是有效的模块类型时抛出。</exception>
    public static IApplicationWithServiceProvider AddModularityApplication<TStartupModel>(this IServiceCollection services, IConfiguration? configuration = null, Action<ApplicationCreationOptions>? optionsAction = null)
        where TStartupModel : class, IModuleContext
    {
        return ApplicationFactory.Create<TStartupModel>(services, configuration, optionsAction);
    }

    /// <summary>
    /// 注册模块化框架的基础服务，包括代理生成器、延迟服务提供程序与选项支持。
    /// </summary>
    /// <param name="services">要注册服务的服务集合。</param>
    internal static void AddCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ProxyGenerator>();
        services.TryAddTransient<ILazyServiceProvider, LazyServiceProvider>();
        services.AddOptions();
    }

    /// <summary>
    /// 注册模块加载与类型发现所需的单例服务，并注册已确保环境变量源位于最后的应用程序配置。
    /// </summary>
    /// <param name="services">要注册服务的服务集合。</param>
    /// <param name="application">当前的模块化应用程序实例，用于定位其程序集。</param>
    /// <param name="creationOptions">应用程序创建选项，提供用于构建配置的初始配置来源。</param>
    /// <param name="configuration">显式提供的应用程序配置；为 <see langword="null"/> 时使用已注册的单例配置或宿主上下文配置，均不存在时按创建选项构建。</param>
    /// <remarks>
    /// <para>
    /// 显式配置、已注册的 <see cref="IConfiguration"/> 单例与 <see cref="HostBuilderContext.Configuration"/> 三条路径
    /// 都会先经过 <see cref="ConfigurationHelper.EnsureEnvironmentVariables"/> 包装，再作为单例注册，
    /// 从而保证模块读取配置时环境变量源的优先级最高。
    /// </para>
    /// <para>
    /// 若上述三条路径都取不到配置实例，但服务集合中已存在 <see cref="IConfiguration"/> 的**非实例注册**
    /// （工厂或实现类型），则保留宿主原有注册，不新建也不替换配置；此时 <see cref="GetConfiguration"/> 返回
    /// <see langword="null"/>，由应用程序以 <c>IDS_CONFIGURATION_NOT_AVAILABLE</c> 明确失败，而不是静默使用错误的当前目录配置。
    /// </para>
    /// </remarks>
    internal static void AddCoreServices(this IServiceCollection services, IModularityApplication application, ApplicationCreationOptions creationOptions, IConfiguration? configuration)
    {
        var moduleLoader = new ModuleLoader();
        var assemblyFinder = new AssemblyFinder(application);
        var typeFinder = new TypeFinder(assemblyFinder);

        // 优先使用显式配置，其次是已注册的单例配置，最后是宿主上下文配置；均不存在时按创建选项构建。
        var configurationToRegister = configuration
                                      ?? services.GetSingletonInstanceOrNull<IConfiguration>()
                                      ?? services.GetSingletonInstanceOrNull<HostBuilderContext>()?.Configuration;

        if (configurationToRegister != null)
        {
            services.ReplaceConfiguration(ConfigurationHelper.EnsureEnvironmentVariables(configurationToRegister)!);
        }
        else if (!services.IsAdded<IConfiguration>())
        {
            services.ReplaceConfiguration(ConfigurationHelper.BuildConfiguration(creationOptions.Configuration));
        }

        services.TryAddSingleton<IModuleLoader>(moduleLoader);
        services.TryAddSingleton<IAssemblyFinder>(assemblyFinder);
        services.TryAddSingleton<ITypeFinder>(typeFinder);

        services.AddAssemblyOf<IModularityApplication>();

        services.Configure<ModuleLifecycleOptions>(options =>
        {
            options.Lifecycle.TryAdd<OnApplicationInitializationModuleLifecycle>();
            options.Lifecycle.TryAdd<OnApplicationShutdownModuleLifecycle>();
        });
    }

    /// <summary>
    /// 替换服务集合中注册的 <see cref="IConfiguration"/> 实例。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">要注册的配置实例，将作为单例替换已有的 <see cref="IConfiguration"/> 注册。</param>
    /// <returns>返回同一个 <see cref="IServiceCollection"/> 实例，以便链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="configuration"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IServiceCollection ReplaceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        Check.EnsureNotNull(services, nameof(services));
        Check.EnsureNotNull(configuration, nameof(configuration));

        services.RemoveAll<IConfiguration>();
        services.AddSingleton(typeof(IConfiguration), configuration);

        return services;
    }

    /// <summary>
    /// 获取服务集合中注册的 <see cref="IConfiguration"/> 实例。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>
    /// 优先返回服务集合中注册的 <see cref="IConfiguration"/> 单例（该实例已确保环境变量源位于最后）；
    /// 若不存在单例实例，则返回 <see cref="HostBuilderContext.Configuration"/>；均不存在时返回 <see langword="null"/>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 仅以工厂或实现类型注册的 <see cref="IConfiguration"/> 无法在构建服务提供程序之前解析出实例，
    /// 此时回退到 <see cref="HostBuilderContext.Configuration"/>，避免用新建配置覆盖宿主的注册。
    /// </para>
    /// <para>
    /// 若既没有单例实例也没有 <see cref="HostBuilderContext"/>，则返回 <see langword="null"/>；
    /// 应用程序随后以 <c>IDS_CONFIGURATION_NOT_AVAILABLE</c> 明确失败，不会静默改用当前目录的配置文件。
    /// </para>
    /// </remarks>
    public static IConfiguration? GetConfiguration(this IServiceCollection services)
    {
        Check.EnsureNotNull(services, nameof(services));

        var configuration = services.GetSingletonInstanceOrNull<IConfiguration>();
        if (configuration != null)
        {
            return configuration;
        }

        return services.GetSingletonInstanceOrNull<HostBuilderContext>()?.Configuration;
    }
}
