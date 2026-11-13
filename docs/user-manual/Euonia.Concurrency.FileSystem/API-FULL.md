# Euonia.Concurrency.FileSystem 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Concurrency.FileSystem |
| 公共类型 | 5 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Concurrency.FileSystem

### Nerosoft.Euonia.Concurrency.FileSystem.FileLockProvider

**类型**：class
接口 ILockProvider<Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ILockProvider

- new Nerosoft.Euonia.Concurrency.FileSystem.FileLockProvider(System.IO.FileInfo lockFile)
- new Nerosoft.Euonia.Concurrency.FileSystem.FileLockProvider(System.IO.DirectoryInfo lockFileDirectory, System.String name)
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationFactory

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ILockFactory

- new Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationFactory(System.IO.DirectoryInfo lockFileDirectory)

### Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationHandle

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ISynchronizationHandle、System.IDisposable、System.IAsyncDisposable

- void Dispose()
- System.Threading.Tasks.ValueTask DisposeAsync()

### Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationOptions

**类型**：class

- new Nerosoft.Euonia.Concurrency.FileSystem.FileSynchronizationOptions()
- System.String Directory { get; set; }
- static const System.String ConfigurationSection = FileSynchronization

### Nerosoft.Euonia.Concurrency.FileSystem.FileSystemLockModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.FileSystem.FileSystemLockModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
