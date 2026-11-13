# Euonia.Concurrency.ZooKeeper 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Concurrency.ZooKeeper |
| 公共类型 | 8 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Concurrency.ZooKeeper

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockProvider

**类型**：class
接口 ILockProvider<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ILockProvider

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockProvider(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath path, System.String connectionString, System.Boolean assumePathExists = false, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockProvider(System.String name, System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperLockProvider(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath directoryPath, System.String name, System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath

**类型**：struct
接口 IEquatable<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath>

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath(System.String path)
- System.Boolean Equals(System.Object obj)
- System.Boolean Equals(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath that)
- System.Int32 GetHashCode()
- static System.Boolean operator Equality(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath this, Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath that)
- static System.Boolean operator Inequality(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath this, Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath that)
- System.String ToString()

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreProvider

**类型**：class
接口 ISemaphoreProvider<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle>、Nerosoft.Euonia.Concurrency.ISemaphoreProvider

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreProvider(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath path, System.Int32 maxCount, System.String connectionString, System.Boolean assumePathExists = false, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreProvider(System.String name, System.Int32 maxCount, System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSemaphoreProvider(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath directoryPath, System.String name, System.Int32 maxCount, System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- System.Int32 MaxCount { get; }
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath Path { get; }
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationFactory

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ILockFactory、Nerosoft.Euonia.Concurrency.ISemaphoreFactory

- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationFactory(System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)
- new Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationFactory(Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperPath directoryPath, System.String connectionString, Action<Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder> options = null)

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationHandle

**类型**：class
接口 Nerosoft.Euonia.Concurrency.ISynchronizationHandle、System.IDisposable、System.IAsyncDisposable

- System.Threading.CancellationToken HandleCancellationToken { get; }
- System.Threading.Tasks.ValueTask DisposeAsync()

### Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder

**类型**：class

- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder AddAccessControl(System.String scheme, System.String id, System.Int32 permissionFlags)
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder AddAuthInfo(System.String scheme, IReadOnlyList<System.Byte> auth)
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder ConnectTimeout(System.TimeSpan connectTimeout)
- Nerosoft.Euonia.Concurrency.ZooKeeper.ZooKeeperSynchronizationOptionsBuilder SessionTimeout(System.TimeSpan sessionTimeout)
