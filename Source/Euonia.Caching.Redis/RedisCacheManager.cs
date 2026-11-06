using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 按 <see cref="RedisCacheOptions"/> 构建 Redis 缓存配置（缓存句柄与背板），并按缓存值类型复用缓存管理器实例。
/// </summary>
internal class RedisCacheManager
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> _instances = new();

    private readonly CacheManagerConfiguration _configuration;

    /// <summary>
    /// 初始化 <see cref="RedisCacheManager"/> 的新实例，并按指定选项构建 Redis 缓存配置。
    /// </summary>
    /// <param name="options">包含连接字符串、更新模式、重试与过期设置的 Redis 缓存选项。</param>
    public RedisCacheManager(RedisCacheOptions options)
    {
        var configuration = ConfigurationBuilder.BuildConfiguration(settings =>
        {
            // 顺序重要：必须先注册 Redis 连接配置。
            // WithRedisBackplane / WithRedisCacheHandle 需要在构建时取回连接字符串，
            // 并把它作为句柄/背板的构造参数（见各自的实现说明）。
            var handle = settings.WithUpdateMode(options.UpdateMode)
                                 .WithMaxRetries(options.MaxRetries)
                                 .WithRetryTimeout(options.RetryTimeout)
                                 .WithRedisConfiguration("redisConnection", options.ConnectionString, options.Database)
                                 .WithRedisBackplane("redisConnection")
                                 .WithRedisCacheHandle("redisConnection");

            // Default 模式会被 GetItemExpiration 当作“无过期”，必须用 Absolute 才会真正应用 Expires。
            if (options.Expires.HasValue)
            {
                handle.WithExpiration(CacheExpirationMode.Absolute, options.Expires.Value);
            }
        });

        _configuration = configuration;
    }

    /// <summary>
    /// 获取指定类型的缓存管理器，同一类型在整个应用内共享同一实例。
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
