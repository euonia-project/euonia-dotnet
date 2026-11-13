using Nerosoft.Euonia.Caching.Redis;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 针对 redis 缓存句柄的配置构建器扩展。
/// </summary>
public static class RedisConfigurationBuilderExtensions
{
    /// <summary>
    /// 使用给定的 <paramref name="configurationKey"/> 添加 redis 配置。
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="configurationKey">
    /// 配置键，可用于让 redis 缓存句柄或背板引用此配置。
    /// </param>
    /// <param name="configuration">redis 配置对象。</param>
    /// <returns>配置构建器。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="configuration"/> 或 <paramref name="configurationKey"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, Action<RedisConfigurationBuilder> configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));

        var builder = new RedisConfigurationBuilder(configurationKey);
        configuration(builder);
        RedisConfigurations.AddConfiguration(builder.Build());
        return part;
    }

    /// <summary>
    /// 使用给定的 <paramref name="configurationKey"/> 添加 redis 配置。
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="configurationKey">
    /// 配置键，可用于让 redis 缓存句柄或背板引用此配置。
    /// </param>
    /// <param name="connectionString">redis 连接字符串。</param>
    /// <param name="database">要使用的 redis 数据库。</param>
    /// <param name="enableKeyspaceNotifications">
    /// 启用键空间通知，以响应项的逐出/过期。
    /// 请确保所有服务器均已正确配置，且 'notify-keyspace-events' 至少设置为 'Exe'，否则 CacheManager 将获取不到任何事件。
    /// 有关配置详情，请参阅 <see href="https://redis.io/topics/notifications#configuration"/>。
    /// </param>
    /// <returns>配置构建器。</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="configurationKey"/> 或 <paramref name="connectionString"/> 为 null 时抛出。
    /// </exception>
    public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, string connectionString, int database = 0, bool enableKeyspaceNotifications = false)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

        Check.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));

        RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString, database, enableKeyspaceNotifications));
        return part;
    }

    /// <summary>
    /// 将现有的 <see cref="IConnectionMultiplexer"/> 添加到缓存管理器配置中，可供 redis 缓存句柄和/或背板引用。
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="configurationKey">
    /// 配置键，可用于让 redis 缓存句柄或背板引用此配置。
    /// </param>
    /// <param name="redisClient">连接多路复用器实例。</param>
    /// <param name="database">用于缓存的 redis 数据库。</param>
    /// <param name="enableKeyspaceNotifications">
    /// 启用键空间通知，以响应项的逐出/过期。
    /// 请确保所有服务器均已正确配置，且 'notify-keyspace-events' 至少设置为 'Exe'，否则 CacheManager 将获取不到任何事件。
    /// 有关配置详情，请参阅 <see href="https://redis.io/topics/notifications#configuration"/>。
    /// </param>
    /// <returns>配置构建器。</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="configurationKey"/> 或 <paramref name="redisClient"/> 为 null 时抛出。
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
    /// 为缓存管理器配置缓存背板。
    /// 使用 <paramref name="redisConfigurationKey"/> 查找匹配的 redis 配置。
    /// <para>
    /// 如果定义了背板，则必须至少将一个缓存句柄标记为背板源。
    /// 随后缓存管理器将尝试同步使用相同配置的多个实例。
    /// </para>
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="redisConfigurationKey">
    /// redis 配置键，用于查找匹配的 redis 连接配置。
    /// </param>
    /// <returns>构建器实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="redisConfigurationKey"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey)
    {
        Check.EnsureNotNull(part, nameof(part));

        // 与 WithRedisCacheHandle 同理：RedisCacheBackplane 的构造函数需要连接字符串，
        // 而参数匹配只认「已知实例」。此处同样必须把它作为配置类型传入，
        // 否则背板创建会因参数匹配失败而抛异常。
        var configuration = RedisConfigurations.GetConfiguration(redisConfigurationKey)
            ?? throw new InvalidOperationException(string.Format(Resources.IDS_NO_REDIS_CONFIG_FOR_BACKPLANE, redisConfigurationKey));

        // 必须显式转换为 object 以命中 params object[] 重载：
        // 直接传 string 会被解析到 WithBackplane(Type, string configurationKey, string channelName, ...)，
        // 把连接字符串当成背板频道名，构造参数依然缺失。
        return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey, (object)configuration.ConnectionString);
    }

    /// <summary>
    /// 为缓存管理器配置缓存背板。
    /// 使用 <paramref name="redisConfigurationKey"/> 查找匹配的 redis 配置。
    /// <para>
    /// 如果定义了背板，则必须至少将一个缓存句柄标记为背板源。
    /// 随后缓存管理器将尝试同步使用相同配置的多个实例。
    /// </para>
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="redisConfigurationKey">
    /// redis 配置键，用于查找匹配的 redis 连接配置。
    /// </param>
    /// <param name="channelName">背板应使用的发布/订阅通道名称。</param>
    /// <returns>构建器实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="redisConfigurationKey"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey, string channelName)
    {
        Check.EnsureNotNull(part, nameof(part));

        var configuration = RedisConfigurations.GetConfiguration(redisConfigurationKey)
            ?? throw new InvalidOperationException(string.Format(Resources.IDS_NO_REDIS_CONFIG_FOR_BACKPLANE, redisConfigurationKey));

        // 连接字符串必须作为构造参数传入：RedisCacheBackplane 的构造函数为
        // (CacheManagerConfiguration, string connectionString)，不会自行查配置注册表。
        return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey, channelName, (object)configuration.ConnectionString);
    }
        
    /// <summary>
    /// 添加一个 <see cref="RedisCacheHandle{TCacheValue}"/>。
    /// 此句柄要求已使用给定的 <paramref name="redisConfigurationKey"/> 定义了 redis 配置。
    /// </summary>
    /// <param name="part">构建器实例。</param>
    /// <param name="redisConfigurationKey">
    /// redis 配置键，用于查找匹配的 redis 连接配置。
    /// </param>
    /// <param name="isBackplaneSource">
    /// 如果此缓存句柄应作为背板源，请将其设置为 true。
    /// 如果未配置背板，此设置将被忽略。
    /// </param>
    /// <returns>构建器实例。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="redisConfigurationKey"/> 为 null 时抛出。</exception>
    public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationKey, bool isBackplaneSource = true)
    {
        Check.EnsureNotNull(part, nameof(part));

        // RedisCacheHandle 的构造函数签名为 (CacheManagerConfiguration, CacheHandleConfiguration, string connectionString)，
        // 该字符串只能作为句柄配置的 ConfigurationTypes 传入——CacheReflectionHelper.MatchArguments
        // 只从「已知实例」里按可赋值性匹配构造参数，不会去查 RedisConfigurations 注册表。
        // 此前这里调用的是不带 configurationTypes 的重载，导致该参数永远匹配不到，
        // 句柄创建直接抛 InvalidOperationException：整个 Redis 缓存后端因此完全不可用。
        var configuration = RedisConfigurations.GetConfiguration(redisConfigurationKey)
            ?? throw new InvalidOperationException(string.Format(Resources.IDS_NO_REDIS_CONFIG_FOR_CACHE_HANDLE, redisConfigurationKey));

        return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationKey, isBackplaneSource, configuration.ConnectionString);
    }
}