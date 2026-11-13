# Euonia.Caching 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Caching |
| 公共类型 | 58 |
| 命名空间 | 3 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDefaultCacheManager<TComponent>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Caching

### AcquireContext<TKey>

**类型**：class
接口 Nerosoft.Euonia.Caching.IAcquireContext

- new AcquireContext<TKey>(TKey key, Action<Nerosoft.Euonia.Caching.IVolatileToken> monitor)
- TKey Key { get; }
- Action<Nerosoft.Euonia.Caching.IVolatileToken> Monitor { get; }

### BaseCacheManager<TValue>

**类型**：class
基类 BaseCache<TValue>；接口 ICache<TValue>、System.IDisposable、ICacheManager<TValue>

- new BaseCacheManager<TValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration)
- Nerosoft.Euonia.Caching.Internal.CacheBackplane Backplane { get; }
- IEnumerable<BaseCacheHandle<TValue>> CacheHandles { get; }
- Nerosoft.Euonia.Caching.CacheManagerConfiguration Configuration { get; }
- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnAdd
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheClearEventArgs> OnClear
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheClearRegionEventArgs> OnClearRegion
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnGet
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnPut
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnRemove
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheItemRemovedEventArgs> OnRemoveByHandle
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnUpdate
- TValue AddOrUpdate(System.String key, TValue addValue, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(System.String key, System.String region, TValue addValue, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(System.String key, TValue addValue, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue AddOrUpdate(System.String key, System.String region, TValue addValue, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)
- void Expire(System.String key, Nerosoft.Euonia.Caching.CacheExpirationMode mode, System.TimeSpan timeout)
- void Expire(System.String key, System.String region, Nerosoft.Euonia.Caching.CacheExpirationMode mode, System.TimeSpan timeout)
- void Expire(System.String key, System.DateTimeOffset absoluteExpiration)
- void Expire(System.String key, System.String region, System.DateTimeOffset absoluteExpiration)
- void Expire(System.String key, System.TimeSpan slidingExpiration)
- void Expire(System.String key, System.String region, System.TimeSpan slidingExpiration)
- TValue GetOrAdd(System.String key, TValue value)
- TValue GetOrAdd(System.String key, System.String region, TValue value)
- TValue GetOrAdd(System.String key, Func<System.String, TValue> valueFactory)
- TValue GetOrAdd(System.String key, System.String region, Func<System.String, System.String, TValue> valueFactory)
- CacheItem<TValue> GetOrAdd(System.String key, Func<System.String, CacheItem<TValue>> valueFactory)
- CacheItem<TValue> GetOrAdd(System.String key, System.String region, Func<System.String, System.String, CacheItem<TValue>> valueFactory)
- Task<System.Int64> RemoveByPrefixAsync(System.String prefix, System.Threading.CancellationToken cancellationToken = default)
- void RemoveExpiration(System.String key)
- void RemoveExpiration(System.String key, System.String region)
- System.String ToString()
- System.Boolean TryGetOrAdd(System.String key, Func<System.String, TValue> valueFactory, out TValue value)
- System.Boolean TryGetOrAdd(System.String key, System.String region, Func<System.String, System.String, TValue> valueFactory, out TValue value)
- System.Boolean TryGetOrAdd(System.String key, Func<System.String, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
- System.Boolean TryGetOrAdd(System.String key, System.String region, Func<System.String, System.String, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
- System.Boolean TryUpdate(System.String key, Func<TValue, TValue> updateValue, out TValue value)
- System.Boolean TryUpdate(System.String key, System.String region, Func<TValue, TValue> updateValue, out TValue value)
- System.Boolean TryUpdate(System.String key, Func<TValue, TValue> updateValue, System.Int32 maxRetries, out TValue value)
- System.Boolean TryUpdate(System.String key, System.String region, Func<TValue, TValue> updateValue, System.Int32 maxRetries, out TValue value)
- TValue Update(System.String key, Func<TValue, TValue> updateValue)
- TValue Update(System.String key, System.String region, Func<TValue, TValue> updateValue)
- TValue Update(System.String key, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue Update(System.String key, System.String region, Func<TValue, TValue> updateValue, System.Int32 maxRetries)

### Nerosoft.Euonia.Caching.BaseCacheService

**类型**：class

- System.String KeyPrefix { get; set; }

### CacheContextTask<T>

**类型**：class
接口 System.IDisposable

- new CacheContextTask<T>(Nerosoft.Euonia.Caching.ICacheContextAccessor cacheContextAccessor, Func<T> function)
- IEnumerable<Nerosoft.Euonia.Caching.IVolatileToken> Tokens { get; }
- void Dispose()
- T Execute()
- void Finish()

### Nerosoft.Euonia.Caching.CacheExpirationMode

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Default |
| None |
| Sliding |
| Absolute |


### Nerosoft.Euonia.Caching.CacheFactory

**类型**：static class

- static ICacheManager<System.Object> Build(System.String cacheName, Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static ICacheManager<System.Object> Build(Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static ICacheManager<TCacheValue> Build<TCacheValue>(System.String cacheName, Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static ICacheManager<TCacheValue> Build<TCacheValue>(Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static System.Object Build(System.Type cacheValueType, System.String cacheName, Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static System.Object Build(System.Type cacheValueType, Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(System.String cacheName, Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration)
- static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration)
- static System.Object FromConfiguration(System.Type cacheValueType, System.String cacheName, Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration)
- static System.Object FromConfiguration(System.Type cacheValueType, Nerosoft.Euonia.Caching.CacheManagerConfiguration configuration)

### Nerosoft.Euonia.Caching.CacheHandleConfiguration

**类型**：class

- new Nerosoft.Euonia.Caching.CacheHandleConfiguration()
- new Nerosoft.Euonia.Caching.CacheHandleConfiguration(System.String handleName)
- new Nerosoft.Euonia.Caching.CacheHandleConfiguration(System.String handleName, System.String configurationKey)
- System.Boolean EnableStatistics { get; set; }
- Nerosoft.Euonia.Caching.CacheExpirationMode ExpirationMode { get; set; }
- System.TimeSpan ExpirationTimeout { get; set; }
- System.Type HandleType { get; set; }
- System.Boolean IsBackplaneSource { get; set; }
- System.String Key { get; set; }
- System.String Name { get; set; }
- System.String ToString()

### CacheItem<T>

**类型**：class
接口 System.Runtime.Serialization.ISerializable、Nerosoft.Euonia.Caching.Internal.ICacheItemProperties

- new CacheItem<T>(System.String key, T value)
- new CacheItem<T>(System.String key, System.String region, T value)
- new CacheItem<T>(System.String key, T value, Nerosoft.Euonia.Caching.CacheExpirationMode expiration, System.TimeSpan timeout)
- new CacheItem<T>(System.String key, System.String region, T value, Nerosoft.Euonia.Caching.CacheExpirationMode expiration, System.TimeSpan timeout)
- System.DateTime CreatedUtc { get; }
- Nerosoft.Euonia.Caching.CacheExpirationMode ExpirationMode { get; }
- System.TimeSpan ExpirationTimeout { get; }
- System.Boolean IsExpired { get; }
- System.String Key { get; }
- System.DateTime LastAccessedUtc { get; set; }
- System.String Region { get; }
- System.Boolean UsesExpirationDefaults { get; }
- T Value { get; }
- System.Type ValueType { get; }
- void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
- System.String ToString()
- CacheItem<T> WithAbsoluteExpiration(System.DateTimeOffset absoluteExpiration)
- CacheItem<T> WithAbsoluteExpiration(System.TimeSpan absoluteExpiration)
- CacheItem<T> WithCreated(System.DateTime created)
- CacheItem<T> WithDefaultExpiration()
- CacheItem<T> WithKey(System.String key)
- CacheItem<T> WithNoExpiration()
- CacheItem<T> WithSlidingExpiration(System.TimeSpan slidingExpiration)
- CacheItem<T> WithValue(T value)

### Nerosoft.Euonia.Caching.CacheManagerConfiguration

**类型**：class

- new Nerosoft.Euonia.Caching.CacheManagerConfiguration()
- System.String BackplaneChannelName { get; set; }
- System.String BackplaneConfigurationKey { get; set; }
- System.Type BackplaneType { get; set; }
- System.Object[] BackplaneTypeArguments { get; set; }
- Nerosoft.Euonia.Caching.ConfigurationBuilder Builder { get; }
- IList<Nerosoft.Euonia.Caching.CacheHandleConfiguration> CacheHandleConfigurations { get; }
- System.Boolean HasBackplane { get; }
- System.Int32 MaxRetries { get; set; }
- System.String Name { get; set; }
- System.Int32 RetryTimeout { get; set; }
- Nerosoft.Euonia.Caching.CacheUpdateMode UpdateMode { get; set; }
- System.String ToString()

### Nerosoft.Euonia.Caching.CacheManagerExtensions

**类型**：static class

- static TResult AddOrUpdate<TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, System.String key, Func<AcquireContext<System.String>, TResult> acquire)
- static TResult AddOrUpdate<TKey, TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, System.Boolean preventConcurrentCalls)
- static TResult AddOrUpdate<TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, System.String key, Func<AcquireContext<System.String>, TResult> acquire, System.Boolean preventConcurrentCalls)
- static TResult Get<TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, System.String key)
- static TResult GetOrAdd<TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, System.String key, Func<AcquireContext<System.String>, TResult> acquire)
- static TResult GetOrAdd<TKey, TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, System.Boolean preventConcurrentCalls)
- static TResult GetOrAdd<TResult>(Nerosoft.Euonia.Caching.ICacheManager manager, System.String key, Func<AcquireContext<System.String>, TResult> acquire, System.Boolean preventConcurrentCalls)

### Nerosoft.Euonia.Caching.CacheTelemetry

**类型**：static class

- static const System.String DurationMetric = euonia.cache.duration
- static const System.String GetActivity = euonia.cache.get
- static const System.String RemoveActivity = euonia.cache.remove
- static const System.String RequestsMetric = euonia.cache.requests
- static const System.String SetActivity = euonia.cache.set
- static readonly System.Diagnostics.ActivitySource Source
- static const System.String SourceName = Nerosoft.Euonia.Caching
- static void Complete(System.Diagnostics.Activity activity, System.String operation, System.Boolean success, System.Diagnostics.Stopwatch stopwatch)
- static void Record(System.String operation, System.String result, System.Double elapsedSeconds)
- static System.Diagnostics.Activity Start(System.String name, System.String operation, System.String system)

### Nerosoft.Euonia.Caching.CacheUpdateMode

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| Up |


### Nerosoft.Euonia.Caching.ConfigurationBuilder

**类型**：class
基类 Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart

- new Nerosoft.Euonia.Caching.ConfigurationBuilder()
- new Nerosoft.Euonia.Caching.ConfigurationBuilder(System.String name)
- new Nerosoft.Euonia.Caching.ConfigurationBuilder(Nerosoft.Euonia.Caching.CacheManagerConfiguration forConfiguration)
- new Nerosoft.Euonia.Caching.ConfigurationBuilder(System.String name, Nerosoft.Euonia.Caching.CacheManagerConfiguration forConfiguration)
- static Nerosoft.Euonia.Caching.CacheManagerConfiguration BuildConfiguration(Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static Nerosoft.Euonia.Caching.CacheManagerConfiguration BuildConfiguration(System.String name, Action<Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart> settings)
- static System.TimeSpan GetTimeSpan(System.String timespanCfgValue, System.String propName)

### Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart

**类型**：class

- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart And { get; }
- Nerosoft.Euonia.Caching.CacheManagerConfiguration Build()
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart DisableStatistics()
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart EnableStatistics()
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithExpiration(Nerosoft.Euonia.Caching.CacheExpirationMode expirationMode, System.TimeSpan timeout)

### Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart

**类型**：class

- Nerosoft.Euonia.Caching.CacheManagerConfiguration Build()
- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithBackplane(System.Type backplaneType, System.String configurationKey, System.Object[] args)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithBackplane(System.Type backplaneType, System.String configurationKey, System.String channelName, System.Object[] args)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithDictionaryHandle(System.Boolean isBackplaneSource = false)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithDictionaryHandle(System.String handleName, System.Boolean isBackplaneSource = false)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithHandle(System.Type cacheHandleBaseType, System.String handleName, System.Boolean isBackplaneSource, System.Object[] configurationTypes)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithHandle(System.Type cacheHandleBaseType, System.String handleName)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCacheHandlePart WithHandle(System.Type cacheHandleBaseType)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithMaxRetries(System.Int32 retries)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithRetryTimeout(System.Int32 timeoutMillis)
- Nerosoft.Euonia.Caching.ConfigurationBuilderCachePart WithUpdateMode(Nerosoft.Euonia.Caching.CacheUpdateMode updateMode)

### Nerosoft.Euonia.Caching.DefaultAsyncTokenProvider

**类型**：class
接口 Nerosoft.Euonia.Caching.IAsyncTokenProvider

- new Nerosoft.Euonia.Caching.DefaultAsyncTokenProvider()
- Nerosoft.Euonia.Caching.IVolatileToken GetToken(Action<Action<Nerosoft.Euonia.Caching.IVolatileToken>> task)

### Nerosoft.Euonia.Caching.DefaultCacheClock

**类型**：class
接口 Nerosoft.Euonia.Caching.ICacheClock、Nerosoft.Euonia.Caching.IVolatileProvider

- new Nerosoft.Euonia.Caching.DefaultCacheClock()
- System.DateTime UtcNow { get; }
- Nerosoft.Euonia.Caching.IVolatileToken When(System.TimeSpan duration)
- Nerosoft.Euonia.Caching.IVolatileToken WhenUtc(System.DateTime absoluteUtc)

### Nerosoft.Euonia.Caching.DefaultCacheContextAccessor

**类型**：class
接口 Nerosoft.Euonia.Caching.ICacheContextAccessor

- new Nerosoft.Euonia.Caching.DefaultCacheContextAccessor()
- Nerosoft.Euonia.Caching.IAcquireContext Current { get; set; }
- static Nerosoft.Euonia.Caching.IAcquireContext ThreadInstance { get; set; }

### Nerosoft.Euonia.Caching.DefaultCacheHolder

**类型**：class
接口 Nerosoft.Euonia.Caching.ICacheHolder

- new Nerosoft.Euonia.Caching.DefaultCacheHolder(Nerosoft.Euonia.Caching.ICacheContextAccessor accessor)

### Nerosoft.Euonia.Caching.DefaultCacheManager

**类型**：class
接口 Nerosoft.Euonia.Caching.ICacheManager

- new Nerosoft.Euonia.Caching.DefaultCacheManager(System.Type component, Nerosoft.Euonia.Caching.ICacheHolder holder)
- TResult AddOrUpdate<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
- TResult Get<TKey, TResult>(TKey key)
- TResult GetOrAdd<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
- System.Boolean TryGet<TKey, TResult>(TKey key, out TResult result)

### DefaultCacheManager<TComponent>

**类型**：class
基类 Nerosoft.Euonia.Caching.DefaultCacheManager；接口 Nerosoft.Euonia.Caching.ICacheManager

- new DefaultCacheManager<TComponent>(Nerosoft.Euonia.Caching.ICacheHolder holder)

### Nerosoft.Euonia.Caching.DefaultCacheSignal

**类型**：class
接口 Nerosoft.Euonia.Caching.ICacheSignal、Nerosoft.Euonia.Caching.IVolatileProvider

- new Nerosoft.Euonia.Caching.DefaultCacheSignal()
- void Trigger<T>(T signal)
- Nerosoft.Euonia.Caching.IVolatileToken When<T>(T signal)

### Nerosoft.Euonia.Caching.DefaultParallelCacheContext

**类型**：class
接口 Nerosoft.Euonia.Caching.IParallelCacheContext

- new Nerosoft.Euonia.Caching.DefaultParallelCacheContext(Nerosoft.Euonia.Caching.ICacheContextAccessor cacheContextAccessor)
- System.Boolean Disabled { get; set; }
- CacheContextTask<T> CreateContextAwareTask<T>(Func<T> function)
- IEnumerable<TResult> RunInParallel<T, TResult>(IEnumerable<T> source, Func<T, TResult> selector)

### Nerosoft.Euonia.Caching.IAcquireContext

**类型**：interface

- Action<Nerosoft.Euonia.Caching.IVolatileToken> Monitor { get; }

### Nerosoft.Euonia.Caching.IAsyncTokenProvider

**类型**：interface

- Nerosoft.Euonia.Caching.IVolatileToken GetToken(Action<Action<Nerosoft.Euonia.Caching.IVolatileToken>> task)

### ICache<TValue>

**类型**：interface
接口 System.IDisposable

- TValue this[System.String key] { get; set; }
- TValue this[System.String key, System.String region] { get; set; }
- System.Boolean Add(System.String key, TValue value)
- System.Boolean Add(System.String key, TValue value, System.String region)
- System.Boolean Add(CacheItem<TValue> item)
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)
- TValue Get(System.String key)
- TValue Get(System.String key, System.String region)
- TOut Get<TOut>(System.String key)
- TOut Get<TOut>(System.String key, System.String region)
- CacheItem<TValue> GetCacheItem(System.String key)
- CacheItem<TValue> GetCacheItem(System.String key, System.String region)
- void Put(System.String key, TValue value)
- void Put(System.String key, TValue value, System.String region)
- void Put(CacheItem<TValue> item)
- System.Boolean Remove(System.String key)
- System.Boolean Remove(System.String key, System.String region)
- Task<System.Int64> RemoveByPrefixAsync(System.String prefix, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Caching.ICacheClock

**类型**：interface
接口 Nerosoft.Euonia.Caching.IVolatileProvider

- System.DateTime UtcNow { get; }
- Nerosoft.Euonia.Caching.IVolatileToken When(System.TimeSpan duration)
- Nerosoft.Euonia.Caching.IVolatileToken WhenUtc(System.DateTime absoluteUtc)

### Nerosoft.Euonia.Caching.ICacheContextAccessor

**类型**：interface

- Nerosoft.Euonia.Caching.IAcquireContext Current { get; set; }

### Nerosoft.Euonia.Caching.ICacheHolder

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Caching.ICacheManager

**类型**：interface

- TResult AddOrUpdate<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
- TResult Get<TKey, TResult>(TKey key)
- TResult GetOrAdd<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire)
- System.Boolean TryGet<TKey, TResult>(TKey key, out TResult result)

### ICacheManager<TValue>

**类型**：interface
接口 ICache<TValue>、System.IDisposable

- IEnumerable<BaseCacheHandle<TValue>> CacheHandles { get; }
- Nerosoft.Euonia.Caching.CacheManagerConfiguration Configuration { get; }
- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnAdd
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheClearEventArgs> OnClear
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheClearRegionEventArgs> OnClearRegion
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnGet
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnPut
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnRemove
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheItemRemovedEventArgs> OnRemoveByHandle
- event EventHandler<Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs> OnUpdate
- TValue AddOrUpdate(System.String key, TValue addValue, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(System.String key, System.String region, TValue addValue, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(System.String key, TValue addValue, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue AddOrUpdate(System.String key, System.String region, TValue addValue, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue)
- TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- void Expire(System.String key, Nerosoft.Euonia.Caching.CacheExpirationMode mode, System.TimeSpan timeout)
- void Expire(System.String key, System.String region, Nerosoft.Euonia.Caching.CacheExpirationMode mode, System.TimeSpan timeout)
- void Expire(System.String key, System.DateTimeOffset absoluteExpiration)
- void Expire(System.String key, System.String region, System.DateTimeOffset absoluteExpiration)
- void Expire(System.String key, System.TimeSpan slidingExpiration)
- void Expire(System.String key, System.String region, System.TimeSpan slidingExpiration)
- TValue GetOrAdd(System.String key, TValue value)
- TValue GetOrAdd(System.String key, System.String region, TValue value)
- TValue GetOrAdd(System.String key, Func<System.String, TValue> valueFactory)
- TValue GetOrAdd(System.String key, System.String region, Func<System.String, System.String, TValue> valueFactory)
- CacheItem<TValue> GetOrAdd(System.String key, Func<System.String, CacheItem<TValue>> valueFactory)
- CacheItem<TValue> GetOrAdd(System.String key, System.String region, Func<System.String, System.String, CacheItem<TValue>> valueFactory)
- void RemoveExpiration(System.String key)
- void RemoveExpiration(System.String key, System.String region)
- System.Boolean TryGetOrAdd(System.String key, Func<System.String, TValue> valueFactory, out TValue value)
- System.Boolean TryGetOrAdd(System.String key, System.String region, Func<System.String, System.String, TValue> valueFactory, out TValue value)
- System.Boolean TryGetOrAdd(System.String key, Func<System.String, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
- System.Boolean TryGetOrAdd(System.String key, System.String region, Func<System.String, System.String, CacheItem<TValue>> valueFactory, out CacheItem<TValue> item)
- System.Boolean TryUpdate(System.String key, Func<TValue, TValue> updateValue, out TValue value)
- System.Boolean TryUpdate(System.String key, System.String region, Func<TValue, TValue> updateValue, out TValue value)
- System.Boolean TryUpdate(System.String key, Func<TValue, TValue> updateValue, System.Int32 maxRetries, out TValue value)
- System.Boolean TryUpdate(System.String key, System.String region, Func<TValue, TValue> updateValue, System.Int32 maxRetries, out TValue value)
- TValue Update(System.String key, Func<TValue, TValue> updateValue)
- TValue Update(System.String key, System.String region, Func<TValue, TValue> updateValue)
- TValue Update(System.String key, Func<TValue, TValue> updateValue, System.Int32 maxRetries)
- TValue Update(System.String key, System.String region, Func<TValue, TValue> updateValue, System.Int32 maxRetries)

### Nerosoft.Euonia.Caching.ICacheService

**类型**：interface

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

### Nerosoft.Euonia.Caching.ICacheSignal

**类型**：interface
接口 Nerosoft.Euonia.Caching.IVolatileProvider

- void Trigger<T>(T signal)
- Nerosoft.Euonia.Caching.IVolatileToken When<T>(T signal)

### Nerosoft.Euonia.Caching.IParallelCacheContext

**类型**：interface

- CacheContextTask<T> CreateContextAwareTask<T>(Func<T> function)
- IEnumerable<TResult> RunInParallel<T, TResult>(IEnumerable<T> source, Func<T, TResult> selector)

### Nerosoft.Euonia.Caching.IVolatileProvider

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Caching.IVolatileToken

**类型**：interface

- System.Boolean IsCurrent { get; }

### Nerosoft.Euonia.Caching.SimpleAcquireContext

**类型**：class
接口 Nerosoft.Euonia.Caching.IAcquireContext

- new Nerosoft.Euonia.Caching.SimpleAcquireContext(Action<Nerosoft.Euonia.Caching.IVolatileToken> monitor)
- Action<Nerosoft.Euonia.Caching.IVolatileToken> Monitor { get; }

## 命名空间 Nerosoft.Euonia.Caching.Internal

### Nerosoft.Euonia.Caching.Internal.BackplaneAction

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Invalid |
| Changed |
| Clear |
| ClearRegion |
| Removed |


### Nerosoft.Euonia.Caching.Internal.BackplaneMessage

**类型**：class

- Nerosoft.Euonia.Caching.Internal.BackplaneAction Action { get; }
- Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction ChangeAction { get; }
- System.String Key { get; }
- System.Byte[] OwnerIdentity { get; }
- System.String Region { get; set; }
- static IEnumerable<Nerosoft.Euonia.Caching.Internal.BackplaneMessage> Deserialize(System.Byte[] message, System.Byte[] skipOwner = null)
- System.Boolean Equals(System.Object obj)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForChanged(System.Byte[] owner, System.String key, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction changeAction)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForChanged(System.Byte[] owner, System.String key, System.String region, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction changeAction)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForClear(System.Byte[] owner)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForClearRegion(System.Byte[] owner, System.String region)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForRemoved(System.Byte[] owner, System.String key)
- static Nerosoft.Euonia.Caching.Internal.BackplaneMessage ForRemoved(System.Byte[] owner, System.String key, System.String region)
- System.Int32 GetHashCode()
- static System.Byte[] Serialize(Nerosoft.Euonia.Caching.Internal.BackplaneMessage[] messages)
- System.String ToString()

### BaseCache<TValue>

**类型**：class
接口 ICache<TValue>、System.IDisposable

- TValue this[System.String key] { get; set; }
- TValue this[System.String key, System.String region] { get; set; }
- System.Boolean Add(System.String key, TValue value)
- System.Boolean Add(System.String key, TValue value, System.String region)
- System.Boolean Add(CacheItem<TValue> item)
- void Clear()
- void ClearRegion(System.String region)
- void Dispose()
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)
- TValue Get(System.String key)
- TValue Get(System.String key, System.String region)
- TOut Get<TOut>(System.String key)
- TOut Get<TOut>(System.String key, System.String region)
- CacheItem<TValue> GetCacheItem(System.String key)
- CacheItem<TValue> GetCacheItem(System.String key, System.String region)
- void Put(System.String key, TValue value)
- void Put(System.String key, TValue value, System.String region)
- void Put(CacheItem<TValue> item)
- System.Boolean Remove(System.String key)
- System.Boolean Remove(System.String key, System.String region)

### BaseCacheHandle<TCacheValue>

**类型**：class
基类 BaseCache<TCacheValue>；接口 ICache<TCacheValue>、System.IDisposable

- Nerosoft.Euonia.Caching.CacheHandleConfiguration Configuration { get; }
- System.Int32 Count { get; }
- System.Boolean IsDistributedCache { get; }
- CacheStats<TCacheValue> Stats { get; }
- Task<System.Int64> RemoveByPrefixAsync(System.String prefix, System.Threading.CancellationToken cancellationToken = default)
- CacheItemUpdateResult<TCacheValue> Update(System.String key, Func<TCacheValue, TCacheValue> updateValue, System.Int32 maxRetries)
- CacheItemUpdateResult<TCacheValue> Update(System.String key, System.String region, Func<TCacheValue, TCacheValue> updateValue, System.Int32 maxRetries)

### Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Local |
| Remote |


### Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs(System.String key, System.String region)
- new Nerosoft.Euonia.Caching.Internal.CacheActionEventArgs(System.String key, System.String region, Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin origin)
- System.String Key { get; }
- Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin Origin { get; }
- System.String Region { get; }
- System.String ToString()

### Nerosoft.Euonia.Caching.Internal.CacheBackplane

**类型**：class
接口 System.IDisposable

- Nerosoft.Euonia.Caching.CacheManagerConfiguration CacheConfiguration { get; }
- System.String ConfigurationKey { get; }
- static System.Int64 MessagesReceived
- static System.Int64 MessagesSent
- static System.Int64 SentChunks
- void Dispose()
- void NotifyChange(System.String key, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction action)
- void NotifyChange(System.String key, System.String region, Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction action)
- void NotifyClear()
- void NotifyClearRegion(System.String region)
- void NotifyRemove(System.String key)
- void NotifyRemove(System.String key, System.String region)

### Nerosoft.Euonia.Caching.Internal.CacheClearEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Caching.Internal.CacheClearEventArgs(Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin origin = 0)
- Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin Origin { get; }
- System.String ToString()

### Nerosoft.Euonia.Caching.Internal.CacheClearRegionEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Caching.Internal.CacheClearRegionEventArgs(System.String region, Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin origin = 0)
- Nerosoft.Euonia.Caching.Internal.CacheActionEventArgOrigin Origin { get; }
- System.String Region { get; }
- System.String ToString()

### Nerosoft.Euonia.Caching.Internal.CacheItemChangedEventAction

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Invalid |
| Put |
| Add |
| Update |


### Nerosoft.Euonia.Caching.Internal.CacheItemRemovedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Caching.Internal.CacheItemRemovedEventArgs(System.String key, System.String region, Nerosoft.Euonia.Caching.Internal.CacheItemRemovedReason reason, System.Object value, System.Int32 level = 0)
- System.String Key { get; }
- System.Int32 Level { get; }
- Nerosoft.Euonia.Caching.Internal.CacheItemRemovedReason Reason { get; }
- System.String Region { get; }
- System.Object Value { get; }
- System.String ToString()

### Nerosoft.Euonia.Caching.Internal.CacheItemRemovedReason

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Expired |
| Evicted |
| ExternalDelete |


### Nerosoft.Euonia.Caching.Internal.CacheItemUpdateResult

**类型**：static class

- static CacheItemUpdateResult<TCacheValue> ForFactoryReturnedNull<TCacheValue>()
- static CacheItemUpdateResult<TValue> ForItemDidNotExist<TValue>()
- static CacheItemUpdateResult<TCacheValue> ForSuccess<TCacheValue>(CacheItem<TCacheValue> value, System.Boolean conflictOccurred = false, System.Int32 triesNeeded = 1)
- static CacheItemUpdateResult<TCacheValue> ForTooManyRetries<TCacheValue>(System.Int32 triesNeeded)

### CacheItemUpdateResult<TValue>

**类型**：class

- System.Int32 NumberOfTriesNeeded { get; }
- Nerosoft.Euonia.Caching.Internal.CacheItemUpdateResultState UpdateState { get; }
- CacheItem<TValue> Value { get; }
- System.Boolean VersionConflictOccurred { get; }

### Nerosoft.Euonia.Caching.Internal.CacheItemUpdateResultState

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Success |
| TooManyRetries |
| ItemDidNotExist |
| FactoryReturnedNull |


### CacheStats<TValue>

**类型**：class
接口 System.IDisposable

- new CacheStats<TValue>(System.String cacheName, System.String handleName, System.Boolean isStatsEnabled)
- void Dispose()
- System.Int64 GetStatistic(Nerosoft.Euonia.Caching.Internal.CacheStatsCounterType type, System.String region)
- System.Int64 GetStatistic(Nerosoft.Euonia.Caching.Internal.CacheStatsCounterType type)
- void OnAdd(CacheItem<TValue> item)
- void OnClear()
- void OnClearRegion(System.String region)
- void OnGet(System.String region = null)
- void OnHit(System.String region = null)
- void OnMiss(System.String region = null)
- void OnPut(CacheItem<TValue> item, System.Boolean itemAdded)
- void OnRemove(System.String region = null)
- void OnUpdate(System.String key, System.String region, CacheItemUpdateResult<TValue> result)

### Nerosoft.Euonia.Caching.Internal.CacheStatsCounterType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Hits |
| Misses |
| Items |
| RemoveCalls |
| AddCalls |
| PutCalls |
| GetCalls |
| ClearCalls |
| ClearRegionCalls |


### DictionaryCacheHandle<TValue>

**类型**：class
基类 BaseCacheHandle<TValue>；接口 ICache<TValue>、System.IDisposable

- new DictionaryCacheHandle<TValue>(Nerosoft.Euonia.Caching.CacheManagerConfiguration managerConfiguration, Nerosoft.Euonia.Caching.CacheHandleConfiguration configuration)
- System.Int32 Count { get; }
- void Clear()
- void ClearRegion(System.String region)
- System.Boolean Exists(System.String key)
- System.Boolean Exists(System.String key, System.String region)

### Nerosoft.Euonia.Caching.Internal.ICacheItemProperties

**类型**：interface

- System.DateTime CreatedUtc { get; }
- Nerosoft.Euonia.Caching.CacheExpirationMode ExpirationMode { get; }
- System.TimeSpan ExpirationTimeout { get; }
- System.String Key { get; }
- System.DateTime LastAccessedUtc { get; set; }
- System.String Region { get; }
- System.Boolean UsesExpirationDefaults { get; }
- System.Type ValueType { get; }

### Nerosoft.Euonia.Caching.Internal.TypeCache

**类型**：static class

- static System.Type ObjectType { get; }
- static System.Type GetType(System.String type)
- static void RegisterResolveType(Func<System.String, System.Type> resolve)
