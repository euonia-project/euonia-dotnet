using Nerosoft.Euonia.Caching;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> 中设置缓存服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加默认的缓存管理器及相关缓存基础设施服务。
    /// </summary>
    /// <typeparam name="TComponent">组件类型，用于创建默认缓存管理器。</typeparam>
    /// <param name="services">要注册缓存服务的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection"/>。</param>
    /// <returns>返回当前的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection"/> 实例，以便进行链式调用。</returns>
    public static IServiceCollection AddDefaultCacheManager<TComponent>(this IServiceCollection services)
    {
        return services.AddSingleton<ICacheClock, DefaultCacheClock>()
                       .AddSingleton<ICacheHolder, DefaultCacheHolder>()
                       .AddSingleton<ICacheContextAccessor, DefaultCacheContextAccessor>()
                       .AddSingleton<IParallelCacheContext, DefaultParallelCacheContext>()
                       .AddSingleton<IAsyncTokenProvider, DefaultAsyncTokenProvider>()
                       .AddSingleton<ICacheSignal, DefaultCacheSignal>()
                       .AddSingleton<ICacheManager, DefaultCacheManager<TComponent>>();
    }
}