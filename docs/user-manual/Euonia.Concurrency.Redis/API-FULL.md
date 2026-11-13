# Euonia.Concurrency.Redis 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Concurrency.Redis |
| 公共类型 | 7 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Concurrency.Redis

### Nerosoft.Euonia.Concurrency.Redis.RedisLockModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.Redis.RedisLockModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Concurrency.Redis.RedisLockProvider

**类型**：class
接口 ILockProvider<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ILockProvider

- new Nerosoft.Euonia.Concurrency.Redis.RedisLockProvider(StackExchange.Redis.RedisKey key, StackExchange.Redis.IDatabase database, Action<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.Redis.RedisLockProvider(StackExchange.Redis.RedisKey key, IEnumerable<StackExchange.Redis.IDatabase> databases, Action<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder> options = null)
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Concurrency.Redis.RedisSemaphoreModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.Redis.RedisSemaphoreModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Concurrency.Redis.RedisSemaphoreProvider

**类型**：class
接口 ISemaphoreProvider<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ISemaphoreProvider

- new Nerosoft.Euonia.Concurrency.Redis.RedisSemaphoreProvider(StackExchange.Redis.RedisKey key, System.Int32 maxCount, StackExchange.Redis.IDatabase database, Action<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder> options = null)
- System.Int32 MaxCount { get; }
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationFactory

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ILockFactory、Nerosoft.Euonia.Concurrency.ISemaphoreFactory

- new Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationFactory(StackExchange.Redis.IDatabase database, Action<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationFactory(IEnumerable<StackExchange.Redis.IDatabase> databases, Action<Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder> options = null)
- Nerosoft.Euonia.Concurrency.Redis.RedisLockProvider Create(StackExchange.Redis.RedisKey key)
- Nerosoft.Euonia.Concurrency.Redis.RedisSemaphoreProvider Create(StackExchange.Redis.RedisKey key, System.Int32 maxCount)

### Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationHandle

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ISynchronizationHandle、System.IDisposable、System.IAsyncDisposable

- System.Threading.CancellationToken HandleCancellationToken { get; }
- void Dispose()
- System.Threading.Tasks.ValueTask DisposeAsync()

### Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder

**类型**：class

- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder BusyWaitSleepTime(System.TimeSpan min, System.TimeSpan max)
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder Expiry(System.TimeSpan expiry)
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder ExtensionCadence(System.TimeSpan extensionCadence)
- Nerosoft.Euonia.Concurrency.Redis.RedisSynchronizationOptionsBuilder MinValidityTime(System.TimeSpan minValidityTime)
