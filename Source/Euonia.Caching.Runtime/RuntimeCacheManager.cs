using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Runtime;

internal class RuntimeCacheManager
{
    private readonly ConcurrentDictionary<Type, object> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

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
        return (ICacheManager<T>)_instances.GetOrAdd(typeof(T), _ => CacheFactory.FromConfiguration<T>(_configuration));
    }
}
