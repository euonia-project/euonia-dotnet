using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Runtime;

/// <summary>
/// 按 <see cref="RuntimeCacheOptions"/> 构建基于 <c>MemoryCache.Default</c> 的缓存配置，并按缓存值类型复用缓存管理器实例。
/// </summary>
internal class RuntimeCacheManager
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="RuntimeCacheManager"/> 的新实例，并按选项构建运行时缓存配置。
    /// </summary>
    /// <param name="options">运行时缓存选项，含实例名、更新与重试策略以及可选的绝对过期时间。</param>
    public RuntimeCacheManager(RuntimeCacheOptions options)
    {
        var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
        {
            var part = settings.WithUpdateMode(options.UpdateMode)
                               .WithMaxRetries(options.MaxRetries)
                               .WithRetryTimeout(options.RetryTimeout);

            // MemoryCache.Default 不允许通过代码配置；此前的实现无条件传入 options，
            // 导致默认实例名 "default" 时构造 RuntimeCacheHandle 直接抛异常。
            var handle = string.Equals(options.InstanceName, "default", StringComparison.OrdinalIgnoreCase)
                             ? part.WithRuntimeCacheHandle(options.InstanceName)
                             : part.WithRuntimeCacheHandle(options.InstanceName, options);

            // Default 模式会被 GetItemExpiration 当作“无过期”，必须用 Absolute 才会真正应用 Expires。
            if (options.Expires.HasValue)
            {
                handle?.WithExpiration(CacheExpirationMode.Absolute, options.Expires.Value);
            }
        });

        _configuration = configuration;
    }

    /// <summary>
    /// 获取指定类型的缓存管理器，同一类型共享同一实例。
    /// </summary>
    /// <typeparam name="T">缓存项的类型。</typeparam>
    /// <returns>与 <typeparamref name="T"/> 对应的缓存管理器实例。</returns>
    public ICacheManager<T> Instance<T>()
    {
        return (ICacheManager<T>)_instances.GetOrAdd(typeof(T), _ => CacheFactory.FromConfiguration<T>(_configuration));
    }
}
