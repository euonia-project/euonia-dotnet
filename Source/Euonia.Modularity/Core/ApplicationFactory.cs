using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 包含用于创建 <see cref="IApplicationWithServiceProvider"/> 新实例的方法。
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    /// 创建 <see cref="IApplicationWithServiceProvider"/> 的新实例。
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型，应用程序从该类型开始解析模块依赖关系。</typeparam>
    /// <param name="services">服务集合，应用程序及其各模块的服务将注册到该集合中。</param>
    /// <param name="configuration">应用程序配置；可为 <see langword="null"/>，此时将按创建选项构建配置。</param>
    /// <param name="optionsAction">用于自定义 <see cref="ApplicationCreationOptions"/> 的委托；为 <see langword="null"/> 时使用默认选项。</param>
    /// <returns>已创建并完成模块加载与依赖注册的应用程序实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <typeparamref name="TStartupModule"/> 不是有效的模块类型（非抽象、非泛型且实现 <see cref="IModuleContext"/> 的类）时抛出。</exception>
    public static IApplicationWithServiceProvider Create<TStartupModule>(IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction = null)
        where TStartupModule : IModuleContext
    {
        return Create(typeof(TStartupModule), services, configuration, optionsAction);
    }

    /// <summary>
    /// 使用默认配置创建 <see cref="IApplicationWithServiceProvider"/> 的新实例。
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块类型，应用程序从该类型开始解析模块依赖关系。</typeparam>
    /// <param name="services">服务集合，应用程序及其各模块的服务将注册到该集合中。</param>
    /// <returns>已创建并完成模块加载与依赖注册的应用程序实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <typeparamref name="TStartupModule"/> 不是有效的模块类型时抛出。</exception>
    public static IApplicationWithServiceProvider Create<TStartupModule>(IServiceCollection services)
        where TStartupModule : IModuleContext
    {
        return Create(typeof(TStartupModule), services, null, null);
    }

    /// <summary>
    /// 创建 <see cref="IApplicationWithServiceProvider"/> 的新实例。
    /// </summary>
    /// <param name="startupModuleType">启动模块类型，应用程序从该类型开始解析模块依赖关系。</param>
    /// <param name="services">服务集合，应用程序及其各模块的服务将注册到该集合中。</param>
    /// <param name="configuration">应用程序配置；可为 <see langword="null"/>，此时将按创建选项构建配置。</param>
    /// <param name="optionsAction">用于自定义 <see cref="ApplicationCreationOptions"/> 的委托；为 <see langword="null"/> 时使用默认选项。</param>
    /// <returns>已创建并完成模块加载与依赖注册的应用程序实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="startupModuleType"/> 或 <paramref name="services"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="startupModuleType"/> 不是有效的模块类型时抛出。</exception>
    public static IApplicationWithServiceProvider Create(Type startupModuleType, IServiceCollection services, IConfiguration? configuration, Action<ApplicationCreationOptions>? optionsAction = null)
    {
        Check.EnsureNotNull(startupModuleType, nameof(startupModuleType));
        Check.EnsureNotNull(services, nameof(services));

        return new ApplicationWithServiceProvider(startupModuleType, services, configuration, optionsAction);
    }
}
