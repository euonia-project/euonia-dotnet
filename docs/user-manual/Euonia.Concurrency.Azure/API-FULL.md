# Euonia.Concurrency.Azure 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Concurrency.Azure |
| 公共类型 | 6 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Concurrency.Azure

### Nerosoft.Euonia.Concurrency.Azure.AzureLockModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.Azure.AzureLockModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Concurrency.Azure.AzureLockProvider

**类型**：class
接口 ILockProvider<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ILockProvider

- new Nerosoft.Euonia.Concurrency.Azure.AzureLockProvider(Azure.Storage.Blobs.Specialized.BlobBaseClient blobClient, Action<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.Azure.AzureLockProvider(Azure.Storage.Blobs.BlobContainerClient blobContainerClient, System.String name, Action<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder> options = null)
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationFactory

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ILockFactory

- new Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationFactory(Azure.Storage.Blobs.BlobContainerClient blobContainerClient, Action<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder> options = null)

### Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationHandle

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ISynchronizationHandle、System.IDisposable、System.IAsyncDisposable

- System.Threading.CancellationToken HandleCancellationToken { get; }
- System.String LeaseId { get; }
- void Dispose()
- System.Threading.Tasks.ValueTask DisposeAsync()

### Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptions

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptions>

- new Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptions(Nerosoft.Euonia.Threading.TimeoutValue Duration, Nerosoft.Euonia.Threading.TimeoutValue RenewalCadence, Nerosoft.Euonia.Threading.TimeoutValue MinBusyWaitSleepTime, Nerosoft.Euonia.Threading.TimeoutValue MaxBusyWaitSleepTime)
- Nerosoft.Euonia.Threading.TimeoutValue Duration { get; set; }
- Nerosoft.Euonia.Threading.TimeoutValue MaxBusyWaitSleepTime { get; set; }
- Nerosoft.Euonia.Threading.TimeoutValue MinBusyWaitSleepTime { get; set; }
- Nerosoft.Euonia.Threading.TimeoutValue RenewalCadence { get; set; }

### Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder

**类型**：class

- Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder BusyWaitSleepTime(System.TimeSpan min, System.TimeSpan max)
- Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder Duration(System.TimeSpan duration)
- Nerosoft.Euonia.Concurrency.Azure.AzureSynchronizationOptionsBuilder RenewalCadence(System.TimeSpan renewalCadence)
