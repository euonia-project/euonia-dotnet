# Euonia.Caching.Redis 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Caching.Redis |
| 公共类型 | 9 |
| 命名空间 | 2 |

## 命名空间 Nerosoft.Euonia.Caching

### Nerosoft.Euonia.Caching.RedisCacheService

**类型**：class
基类 Nerosoft.Euonia.Caching.BaseCacheService；接口 Nerosoft.Euonia.Caching.ICacheService

### Nerosoft.Euonia.Caching.RedisConfigurationBuilderExtensions

**类型**：static class

- static Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRedisBackplane(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String redisConfigurationKey)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRedisBackplane(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String redisConfigurationKey, System.String channelName)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String redisConfigurationKey, System.Boolean isBackplaneSource = true)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRedisConfiguration(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String configurationKey, Action<Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder> configuration)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRedisConfiguration(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String configurationKey, System.String connectionString, System.Int32 database = 0, System.Boolean enableKeyspaceNotifications = false)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRedisConfiguration(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String configurationKey, StackExchange.Redis.IConnectionMultiplexer redisClient, System.Int32 database = 0, System.Boolean enableKeyspaceNotifications = false)

## 命名空间 Nerosoft.Euonia.Caching.Redis

### Nerosoft.Euonia.Caching.Redis.RedisCacheBackplane

**类型**：class
基类 Nerosoft.Euonia.Caching.Internal.CacheBackplane；接口 System.IDisposable

- new Nerosoft.Euonia.Caching.Redis.RedisCacheBackplane(Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration, System.String connectionString)
- void NotifyChange(System.String key, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction action)
- void NotifyChange(System.String key, System.String region, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction action)
- void NotifyClear()
- void NotifyClearRegion(System.String region)
- void NotifyRemove(System.String key)
- void NotifyRemove(System.String key, System.String region)

### RedisCacheHandle<TValue>

**类型**：class
基类 BaseCacheHandle<TValue>；接口 ICache<TValue>、System.IDisposable

- new RedisCacheHandle<TValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration managerConfiguration, Nerosoft.Euonia.Caching.CacheHandleConfiguration configuration, System.String connectionString)
- System.Int32 Count { get; }
- StackExchange.Redis.RedisFeatures Features { get; }
- System.Boolean IsDistributedCache { get; }
- System.Boolean IsLuaAllowed { get; }
- IEnumerable<StackExchange.Redis.IServer> Servers { get; }
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)
- Task<System.Int64> RemoveByPrefixAsync(System.String prefix, System.Threading.CancellationToken cancellationToken = default)
- CacheItemUpdateResult<TValue> Update(System.String key, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- CacheItemUpdateResult<TValue> Update(System.String key, System.String region, Func<TValue, TValue> updateValue, System.Int32 maxRetries)

### Nerosoft.Euonia.Caching.Redis.RedisCacheOptions

**类型**：class

- new Nerosoft.Euonia.Caching.Redis.RedisCacheOptions()
- System.String ConnectionString { get; set; }
- System.Int32 Database { get; set; }
- Nullable<System.TimeSpan> Expires { get; set; }
- System.String KeyPrefix { get; set; }
- System.Int32 MaxRetries { get; set; }
- System.Int32 RetryTimeout { get; set; }
- Nerosoft.Euonia.Caching.CacheUpdateMode UpdateMode { get; set; }

### Nerosoft.Euonia.Caching.Redis.RedisConfiguration

**类型**：class

- new Nerosoft.Euonia.Caching.Redis.RedisConfiguration()
- new Nerosoft.Euonia.Caching.Redis.RedisConfiguration(System.String key, IList<Nerosoft.Euonia.Caching.Redis.ServerEndPoint> endpoints, System.Int32 database = 0, System.String password = null, System.Boolean isSsl = false, System.String sslHost = null, System.Int32 connectionTimeout = 5000, System.Boolean allowAdmin = false, System.Boolean keyspaceNotificationsEnabled = false, System.Boolean twemproxyEnabled = false, System.String strictCompatibilityModeVersion = null)
- new Nerosoft.Euonia.Caching.Redis.RedisConfiguration(System.String key, System.String connectionString, System.Int32 database = 0, System.Boolean keyspaceNotificationsEnabled = false, System.String strictCompatibilityModeVersion = null)
- System.Boolean AllowAdmin { get; set; }
- StackExchange.Redis.ConfigurationOptions ConfigurationOptions { get; }
- System.String ConnectionString { get; set; }
- System.Int32 ConnectionTimeout { get; set; }
- System.Int32 Database { get; set; }
- IList<Nerosoft.Euonia.Caching.Redis.ServerEndPoint> Endpoints { get; }
- System.Boolean IsSsl { get; set; }
- System.String Key { get; set; }
- System.Boolean KeyspaceNotificationsEnabled { get; set; }
- System.String Password { get; set; }
- System.String SslHost { get; set; }
- System.String StrictCompatibilityModeVersion { get; set; }
- System.Boolean TwemproxyEnabled { get; set; }

### Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder

**类型**：class

- new Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder(System.String configurationKey)
- Nerosoft.Euonia.Caching.Redis.RedisConfiguration Build()
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder EnableKeyspaceEvents()
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder UseCompatibilityMode(System.String version)
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder UseTwemproxy()
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithAllowAdmin()
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithConnectionTimeout(System.Int32 timeout)
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithDatabase(System.Int32 databaseIndex)
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithEndpoint(System.String host, System.Int32 port)
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithPassword(System.String serverPassword)
- Nerosoft.Euonia.Caching.Redis.RedisConfigurationBuilder WithSsl(System.String host = null)

### Nerosoft.Euonia.Caching.Redis.RedisConfigurations

**类型**：static class

- static void AddConfiguration(Nerosoft.Euonia.Caching.Redis.RedisConfiguration configuration)
- static Nerosoft.Euonia.Caching.Redis.RedisConfiguration GetConfiguration(System.String configurationName, System.String connectionString)
- static Nerosoft.Euonia.Caching.Redis.RedisConfiguration GetConfiguration(System.String configurationName)

### Nerosoft.Euonia.Caching.Redis.ServerEndPoint

**类型**：class

- new Nerosoft.Euonia.Caching.Redis.ServerEndPoint()
- new Nerosoft.Euonia.Caching.Redis.ServerEndPoint(System.String host, System.Int32 port)
- System.String Host { get; set; }
- System.Int32 Port { get; set; }
