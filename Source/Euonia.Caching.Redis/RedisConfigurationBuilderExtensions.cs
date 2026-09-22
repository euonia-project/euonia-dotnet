using Nerosoft.Euonia.Caching.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Extensions for the configuration builder specific to the redis cache handle.
/// </summary>
public static class RedisConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a redis configuration with the given <paramref name="configurationKey"/>.
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="configurationKey">
    /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
    /// </param>
    /// <param name="configuration">The redis configuration object.</param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="configuration"/> or <paramref name="configurationKey"/> are null.</exception>
    public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, Action<RedisConfigurationBuilder> configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));

        var builder = new RedisConfigurationBuilder(configurationKey);
        configuration(builder);
        RedisConfigurations.AddConfiguration(builder.Build());
        return part;
    }

    /// <summary>
    /// Adds a redis configuration with the given <paramref name="configurationKey"/>.
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="configurationKey">
    /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
    /// </param>
    /// <param name="connectionString">The redis connection string.</param>
    /// <param name="database">The redis database to be used.</param>
    /// <param name="enableKeyspaceNotifications">
    /// Enables keyspace notifications to react on eviction/expiration of items.
    /// Make sure that all servers are configured correctly and 'notify-keyspace-events' is at least set to 'Exe', otherwise CacheManager will not retrieve any events.
    /// See <see href="https://redis.io/topics/notifications#configuration"/> for configuration details.
    /// </param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="configurationKey"/> or <paramref name="connectionString"/> are null.
    /// </exception>
    public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, string connectionString, int database = 0, bool enableKeyspaceNotifications = false)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        Check.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));

        RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString, database, enableKeyspaceNotifications));
        return part;
    }

    /// <summary>
    /// Adds an existing <see cref="IConnectionMultiplexer"/> to the cache manager configuration which can be referenced by redis cache handle and/or backplane.
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="configurationKey">
    /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
    /// </param>
    /// <param name="redisClient">The connection multiplexer instance.</param>
    /// <param name="database">The redis database to use for caching.</param>
    /// <param name="enableKeyspaceNotifications">
    /// Enables keyspace notifications to react on eviction/expiration of items.
    /// Make sure that all servers are configured correctly and 'notify-keyspace-events' is at least set to 'Exe', otherwise CacheManager will not retrieve any events.
    /// See <see href="https://redis.io/topics/notifications#configuration"/> for configuration details.
    /// </param>
    /// <returns>The configuration builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="configurationKey"/> or <paramref name="redisClient"/> are null.
    /// </exception>
    public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, IConnectionMultiplexer redisClient, int database = 0, bool enableKeyspaceNotifications = false)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        Check.EnsureNotNull(redisClient, nameof(redisClient));

        var connectionString = redisClient.Configuration;
        part.WithRedisConfiguration(configurationKey, connectionString, database, enableKeyspaceNotifications);

        RedisConnectionManager.AddConnection(connectionString, redisClient);

        return part;
    }

    /// <summary>
    /// Configures a cache backplane for the cache manager.
    /// The <paramref name="redisConfigurationKey"/> is used to find a matching redis configuration.
    /// <para>
    /// If a backplane is defined, at least one cache handle must be marked as backplane
    /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
    /// </para>
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="redisConfigurationKey">
    /// The redis configuration key will be used to find a matching redis connection configuration.
    /// </param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
    public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey)
    {
        Check.EnsureNotNull(part, nameof(part));

        // 与 WithRedisCacheHandle 同理：RedisCacheBackplane 的构造函数需要连接字符串，
        // 而参数匹配只认「已知实例」。此处同样必须把它作为配置类型传入，
        // 否则背板创建会因参数匹配失败而抛异常。
        var configuration = RedisConfigurations.GetConfiguration(redisConfigurationKey)
            ?? throw new InvalidOperationException($"No Redis configuration has been registered for the key '{redisConfigurationKey}'. Call WithRedisConfiguration('{redisConfigurationKey}', ...) before adding the Redis backplane.");

        // 必须显式转换为 object 以命中 params object[] 重载：
        // 直接传 string 会被解析到 WithBackplane(Type, string configurationKey, string channelName, ...)，
        // 把连接字符串当成背板频道名，构造参数依然缺失。
        return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey, (object)configuration.ConnectionString);
    }

    /// <summary>
    /// Configures a cache backplane for the cache manager.
    /// The <paramref name="redisConfigurationKey"/> is used to find a matching redis configuration.
    /// <para>
    /// If a backplane is defined, at least one cache handle must be marked as backplane
    /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
    /// </para>
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="redisConfigurationKey">
    /// The redis configuration key will be used to find a matching redis connection configuration.
    /// </param>
    /// <param name="channelName">The pub sub channel name the backplane should use.</param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
    public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey, string channelName)
    {
        Check.EnsureNotNull(part, nameof(part));

        return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey, channelName);
    }
        
    /// <summary>
    /// Adds a <see cref="RedisCacheHandle{TCacheValue}"/>.
    /// This handle requires a redis configuration to be defined with the given <paramref name="redisConfigurationKey"/>.
    /// </summary>
    /// <param name="part">The builder instance.</param>
    /// <param name="redisConfigurationKey">
    /// The redis configuration key will be used to find a matching redis connection configuration.
    /// </param>
    /// <param name="isBackplaneSource">
    /// Set this to true if this cache handle should be the source of the backplane.
    /// This setting will be ignored if no backplane is configured.
    /// </param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
    public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationKey, bool isBackplaneSource = true)
    {
        Check.EnsureNotNull(part, nameof(part));

        // RedisCacheHandle 的构造函数签名为 (CacheManagerConfiguration, CacheHandleConfiguration, string connectionString)，
        // 该字符串只能作为句柄配置的 ConfigurationTypes 传入——CacheReflectionHelper.MatchArguments
        // 只从「已知实例」里按可赋值性匹配构造参数，不会去查 RedisConfigurations 注册表。
        // 此前这里调用的是不带 configurationTypes 的重载，导致该参数永远匹配不到，
        // 句柄创建直接抛 InvalidOperationException：整个 Redis 缓存后端因此完全不可用。
        var configuration = RedisConfigurations.GetConfiguration(redisConfigurationKey)
            ?? throw new InvalidOperationException($"No Redis configuration has been registered for the key '{redisConfigurationKey}'. Call WithRedisConfiguration('{redisConfigurationKey}', ...) before adding the Redis cache handle.");

        return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationKey, isBackplaneSource, configuration.ConnectionString);
    }
}