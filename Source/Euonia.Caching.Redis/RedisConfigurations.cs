namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 管理缓存句柄使用的 redis 客户端配置。
/// <para>
/// 配置由缓存配置构建器/工厂或配置加载器添加。
/// 缓存句柄将选取与句柄名称匹配的配置。
/// </para>
/// </summary>
public static class RedisConfigurations
{
    private static Dictionary<string, RedisConfiguration> _config = null!;
    private static readonly object _configLock = new();

    private static Dictionary<string, RedisConfiguration> Configurations
    {
        get
        {
            if (_config == null)
            {
                lock (_configLock)
                {
                    _config ??= new Dictionary<string, RedisConfiguration>();
                }
            }

            return _config;
        }
    }

    /// <summary>
    /// 添加配置。
    /// </summary>
    /// <param name="configuration">配置。</param>
    /// <exception cref="ArgumentNullException">当 configuration 为 null 时抛出。</exception>
    public static void AddConfiguration(RedisConfiguration configuration)
    {
        lock (_configLock)
        {
            Check.EnsureNotNull(configuration, nameof(configuration));
            Check.EnsureNotNullOrWhiteSpace(configuration.Key, nameof(configuration.Key));

            Configurations.TryAdd(configuration.Key, configuration);
        }
    }

    /// <summary>
    /// 获取配置。
    /// </summary>
    /// <param name="configurationName">标识符。</param>
    /// <param name="connectionString">当指定名称尚无已注册配置时，用于创建该配置的 Redis 连接字符串。</param>
    /// <returns><c>RedisConfiguration</c>。</returns>
    /// <exception cref="ArgumentNullException">当 id 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">
    /// 当该 id 未添加任何配置时抛出。
    /// </exception>
    public static RedisConfiguration GetConfiguration(string configurationName, string connectionString)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationName, nameof(configurationName));

        if (!Configurations.ContainsKey(configurationName))
        {
            Check.EnsureNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            // 默认使用数据库 0，目前无法通过连接字符串设置。
            var configuration = new RedisConfiguration(configurationName, connectionString);
            AddConfiguration(configuration);
        }

        return Configurations[configurationName];
    }

    /// <summary>
    /// 获取已注册的配置；未注册时返回 <c>null</c>。
    /// </summary>
    /// <param name="configurationName">配置标识。</param>
    /// <returns>对应的 <see cref="RedisConfiguration"/>；未注册时为 <c>null</c>。</returns>
    /// <remarks>
    /// 供 <c>WithRedisCacheHandle</c> 在构建句柄前取回连接字符串使用：
    /// <c>RedisCacheHandle</c> 的构造函数需要连接字符串，而它只能经由句柄配置的
    /// <c>ConfigurationTypes</c> 传入（见 <c>CacheReflectionHelper.MatchArguments</c>）。
    /// </remarks>
    public static RedisConfiguration? GetConfiguration(string configurationName)
    {
        Check.EnsureNotNullOrWhiteSpace(configurationName, nameof(configurationName));

        return Configurations.TryGetValue(configurationName, out var configuration) ? configuration : null;
    }
}