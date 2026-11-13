# Euonia.Caching.Memory 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Caching.Memory |
| 公共类型 | 4 |
| 命名空间 | 2 |

## 命名空间 Nerosoft.Euonia.Caching

### Nerosoft.Euonia.Caching.MemoryCacheBuilderExtensions

**类型**：static class

- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithMemoryCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.Boolean isBackplaneSource = false)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithMemoryCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, System.Boolean isBackplaneSource = false)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithMemoryCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions options)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithMemoryCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions options)
- static Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithMemoryCacheHandle(Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart part, System.String instanceName, System.Boolean isBackplaneSource, Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions options)

## 命名空间 Nerosoft.Euonia.Caching.Memory

### MemoryCacheHandle<TCacheValue>

**类型**：class
基类 BaseCacheHandle<TCacheValue>；接口 ICache<TCacheValue>、System.IDisposable

- new MemoryCacheHandle<TCacheValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration managerConfiguration, Nerosoft.Euonia.Caching.CacheHandleConfiguration configuration, Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions options = null)
- System.Int32 Count { get; }
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)
- Task<System.Int64> RemoveByPrefixAsync(System.String prefix, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions

**类型**：class
基类 Microsoft.Extensions.Caching.Memory.MemoryCacheOptions；接口 IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>

- new Nerosoft.Euonia.Caching.Memory.MemoryCacheOptions()
- Nullable<System.TimeSpan> Expires { get; set; }
- System.String InstanceName { get; set; }
- System.Boolean IsBackplaneSource { get; set; }
- System.String KeyPrefix { get; set; }
- System.Int32 MaxRetries { get; set; }
- System.Int32 RetryTimeout { get; set; }
- Nerosoft.Euonia.Caching.CacheUpdateMode UpdateMode { get; set; }

### Nerosoft.Euonia.Caching.Memory.MemoryCacheService

**类型**：class
基类 Nerosoft.Euonia.Caching.BaseCacheService；接口 Nerosoft.Euonia.Caching.ICacheService
