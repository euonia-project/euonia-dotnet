using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Redis;

internal class RedisCacheManager
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    public RedisCacheManager(RedisCacheOptions options)
    {
        var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
        {
            settings.WithUpdateMode(options.UpdateMode)
                    .WithMaxRetries(options.MaxRetries)
                    .WithRetryTimeout(options.RetryTimeout)
                    .WithRedisBackplane("redisConnection")
                    .WithRedisConfiguration("redisConnection", options.ConnectionString, options.Database)
                    .WithRedisCacheHandle("redisConnection")
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
