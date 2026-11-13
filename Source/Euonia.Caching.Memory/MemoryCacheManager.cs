using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Memory;

/// <summary>
/// 按 <see cref="MemoryCacheOptions"/> 构建内存缓存配置，并按缓存值类型复用 <see cref="ICacheManager{T}"/> 实例。
/// </summary>
internal class MemoryCacheManager
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="MemoryCacheManager"/> 的新实例，并按选项构建仅包含内存句柄的缓存配置。
    /// </summary>
    /// <param name="options">内存缓存选项，含实例名、更新与重试策略以及可选的绝对过期时间。</param>
    public MemoryCacheManager(MemoryCacheOptions options)
    {
        var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
        {
            var handle = settings.WithUpdateMode(options.UpdateMode)
                                 .WithMaxRetries(options.MaxRetries)
                                 .WithRetryTimeout(options.RetryTimeout)
                                 .WithMemoryCacheHandle(options.InstanceName, options);

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
        // Lazy + ExecutionAndPublication 保证每个类型只会创建并共享一个缓存管理器实例
        return (ICacheManager<T>)_instances.GetOrAdd(
            typeof(T),
            _ => new Lazy<object>(() => CacheFactory.FromConfiguration<T>(_configuration), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }
}
