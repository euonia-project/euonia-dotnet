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
    /// 注册模块加载与类型发现所需的单例服务，并在尚未注册配置时使用显式配置或本次创建选项提供的配置。
    /// </summary>
    /// <param name="services">要注册服务的服务集合。</param>
    /// <param name="application">当前的模块化应用程序实例，用于定位其程序集。</param>
    /// <param name="creationOptions">应用程序创建选项，提供用于构建配置的初始配置来源。</param>
    /// <param name="configuration">显式提供的应用程序配置；为 <see langword="null"/> 时按创建选项构建。</param>
    internal static void AddCoreServices(this IServiceCollection services, IModularityApplication application, ApplicationCreationOptions creationOptions, IConfiguration? configuration)
    {
        var moduleLoader = new ModuleLoader();
        var assemblyFinder = new AssemblyFinder(application);
        var typeFinder = new TypeFinder(assemblyFinder);

        if (configuration != null)
        {
            services.ReplaceConfiguration(configuration);
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
    /// 若存在 <see cref="HostBuilderContext"/> 且其配置非空，则返回该配置；
    /// 否则返回服务集合中注册的 <see cref="IConfiguration"/>；均不存在时返回 <see langword="null"/>。
    /// </returns>
    public static IConfiguration? GetConfiguration(this IServiceCollection services)
    {
        Check.EnsureNotNull(services, nameof(services));

        var hostBuilderContext = services.GetSingletonInstanceOrNull<HostBuilderContext>();
        if (hostBuilderContext?.Configuration != null)
        {
            return hostBuilderContext.Configuration;
        }

        return services.GetSingletonInstanceOrNull<IConfiguration>();
    }
}
