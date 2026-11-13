# Euonia.Caching.Runtime 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Caching.Runtime |
| 公共类型 | 4 |
| 命名空间 | 2 |

## 命名空间 Nerosoft.Euonia.Caching

### Nerosoft.Euonia.Caching.RuntimeCacheBuilderExtensions

**类型**：static class

- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithRuntimeCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.Boolean isBackplaneSource = false)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithRuntimeCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, System.Boolean isBackplaneSource = false)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithRuntimeCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions options)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithRuntimeCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, System.Boolean isBackplaneSource, Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions options)

## 命名空间 Nerosoft.Euonia.Caching.Runtime

### RuntimeCacheHandle<TValue>

**类型**：class
基类 BaseCacheHandle<TValue>；接口 ICache<TValue>、System.IDisposable

- new RuntimeCacheHandle<TValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration managerConfiguration, Nerosoft.Euonia.Caching.CacheHandleConfiguration configuration, Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions options = null)
- System.Collections.Specialized.NameValueCollection CacheSettings { get; }
- System.Int32 Count { get; }
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)

### Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions

**类型**：class

- new Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions()
- System.Int32 CacheMemoryLimitMegabytes { get; set; }
- Nullable<System.TimeSpan> Expires { get; set; }
- System.String InstanceName { get; set; }
- System.Boolean IsBackplaneSource { get; set; }
- System.String KeyPrefix { get; set; }
- System.Int32 MaxRetries { get; set; }
- System.Int32 PhysicalMemoryLimitPercentage { get; set; }
- System.TimeSpan PollingInterval { get; set; }
- System.Int32 RetryTimeout { get; set; }
- Nerosoft.Euonia.Caching.CacheUpdateMode UpdateMode { get; set; }
- System.Collections.Specialized.NameValueCollection AsNameValueCollection()

### Nerosoft.Euonia.Caching.Runtime.RuntimeCacheService

**类型**：class
基类 Nerosoft.Euonia.Caching.BaseCacheService；接口 Nerosoft.Euonia.Caching.ICacheService

- new Nerosoft.Euonia.Caching.Runtime.RuntimeCacheService(IOptions<Nerosoft.Euonia.Caching.Runtime.RuntimeCacheOptions> options)
- TValue AddOrUpdate<TValue>(System.String key, Func<TValue> factory, Nullable<System.TimeSpan> timeout = default)
- TValue AddOrUpdate<TValue>(System.String key, Func<TValue> factory, System.DateTime timeout, System.Boolean isUtcTime = true)
- TValue AddOrUpdate<TValue>(System.String key, TValue value, Nullable<System.TimeSpan> timeout = default)
- TValue AddOrUpdate<TValue>(System.String key, TValue value, System.DateTime timeout, System.Boolean isUtcTime = true)
- TValue AddOrUpdate<TValue>(CacheItem<TValue> item)
- Task<TValue> AddOrUpdateAsync<TValue>(System.String key, Func<Task<TValue>> factory, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Task<TValue> AddOrUpdateAsync<TValue>(System.String key, Func<Task<TValue>> factory, System.DateTime timeout, System.Boolean isUtcTime = true, System.Threading.CancellationToken cancellationToken = default)
- Task<TValue> AddOrUpdateAsync<TValue>(Func<Task<CacheItem<TValue>>> factory, System.Threading.CancellationToken cancellationToken = default)
- TValue Get<TValue>(System.String key)
- TValue GetOrAdd<TValue>(System.String key, Func<TValue> factory, Nullable<System.TimeSpan> timeout = default)
- TValue GetOrAdd<TValue>(System.String key, Func<TValue> factory, System.DateTime timeout, System.Boolean isUtcTime = true)
- Task<TValue> GetOrAddAsync<TValue>(System.String key, Func<Task<TValue>> factory, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Task<TValue> GetOrAddAsync<TValue>(System.String key, Func<Task<TValue>> factory, System.DateTime timeout, System.Boolean isUtcTime = true, System.Threading.CancellationToken cancellationToken = default)
- System.Boolean Remove<TValue>(System.String key)
- System.Boolean TryGet<TValue>(System.String key, out TValue value)
- Task<Tuple<System.Boolean, TValue>> TryGetAsync<TValue>(System.String key, System.Threading.CancellationToken cancellationToken = default)
