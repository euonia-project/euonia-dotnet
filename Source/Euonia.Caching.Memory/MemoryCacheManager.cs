using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Memory;

internal class MemoryCacheManager
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

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
                handle.WithExpiration(CacheExpirationMode.Absolute, options.Expires.Value);
            }
        });

        _configuration = configuration;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ICacheManager<T> Instance<T>()
    {
        // Lazy + ExecutionAndPublication 保证每个类型只会创建并共享一个缓存管理器实例
        return (ICacheManager<T>)_instances.GetOrAdd(
            typeof(T),
            _ => new Lazy<object>(() => CacheFactory.FromConfiguration<T>(_configuration), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }
}
