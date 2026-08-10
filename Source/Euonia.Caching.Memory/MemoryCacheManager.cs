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
            settings.WithUpdateMode(options.UpdateMode)
                    .WithMaxRetries(options.MaxRetries)
                    .WithRetryTimeout(options.RetryTimeout)
                    .WithMemoryCacheHandle(options.InstanceName, options)
                    .WithExpiration(CacheExpirationMode.Default, options.Expires ?? TimeSpan.MaxValue);
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
