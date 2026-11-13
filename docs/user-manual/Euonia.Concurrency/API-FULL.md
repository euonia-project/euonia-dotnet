# Euonia.Concurrency 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Concurrency |
| 公共类型 | 9 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Concurrency

### Nerosoft.Euonia.Concurrency.BusyWaitHelper

**类型**：static class

- static ValueTask<TResult> WaitAsync<TState, TResult>(TState state, Func<TState, System.Threading.CancellationToken, ValueTask<TResult>> tryGetValue, Nerosoft.Euonia.Threading.TimeoutValue timeout, Nerosoft.Euonia.Threading.TimeoutValue minSleepTime, Nerosoft.Euonia.Threading.TimeoutValue maxSleepTime, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Concurrency.Extensions

**类型**：static class

- static Nerosoft.Euonia.Concurrency.ISynchronizationHandle Acquire(Nerosoft.Euonia.Concurrency.ILockFactory provider, System.String name, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static Nerosoft.Euonia.Concurrency.ISynchronizationHandle Acquire(Nerosoft.Euonia.Concurrency.ISemaphoreFactory provider, System.String name, System.Int32 maxCount, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> AcquireAsync(Nerosoft.Euonia.Concurrency.ILockFactory provider, System.String name, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> AcquireAsync(Nerosoft.Euonia.Concurrency.ISemaphoreFactory provider, System.String name, System.Int32 maxCount, Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static Nerosoft.Euonia.Concurrency.ISynchronizationHandle TryAcquire(Nerosoft.Euonia.Concurrency.ILockFactory provider, System.String name, System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static Nerosoft.Euonia.Concurrency.ISynchronizationHandle TryAcquire(Nerosoft.Euonia.Concurrency.ISemaphoreFactory provider, System.String name, System.Int32 maxCount, System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Concurrency.ILockFactory provider, System.String name, System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- static ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Concurrency.ISemaphoreFactory provider, System.String name, System.Int32 maxCount, System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Concurrency.ILockFactory

**类型**：interface

- Nerosoft.Euonia.Concurrency.ILockProvider Create(System.String name)

### Nerosoft.Euonia.Concurrency.ILockProvider

**类型**：interface

- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.ISynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.ISynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken = default)

### ILockProvider<THandle>

**类型**：interface
接口 Nerosoft.Euonia.Concurrency.ILockProvider

- THandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- THandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Concurrency.ISemaphoreFactory

**类型**：interface

- Nerosoft.Euonia.Concurrency.ISemaphoreProvider Create(System.String name, System.Int32 maxCount)

### Nerosoft.Euonia.Concurrency.ISemaphoreProvider

**类型**：interface

- System.Int32 MaxCount { get; }
- System.String Name { get; }
- Nerosoft.Euonia.Concurrency.ISynchronizationHandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Concurrency.ISynchronizationHandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Concurrency.ISynchronizationHandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken = default)

### ISemaphoreProvider<THandle>

**类型**：interface
接口 Nerosoft.Euonia.Concurrency.ISemaphoreProvider

- THandle Acquire(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> AcquireAsync(Nullable<System.TimeSpan> timeout = default, System.Threading.CancellationToken cancellationToken = default)
- THandle TryAcquire(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> TryAcquireAsync(System.TimeSpan timeout = default, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<THandle> TryAcquireAsync(Nerosoft.Euonia.Threading.TimeoutValue timeout, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Concurrency.ISynchronizationHandle

**类型**：interface
接口 System.IDisposable、System.IAsyncDisposable

- System.Threading.CancellationToken HandleCancellationToken { get; }
