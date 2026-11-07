# Euonia.Concurrency

Euonia 的分布式同步抽象：定义锁（`ILockFactory` / `ILockProvider`）与信号量（`ISemaphoreFactory` / `ISemaphoreProvider`）契约，并提供同步、异步、超时与取消的统一获取方式。具体实现由 Redis、ZooKeeper、文件系统或 Azure Blob 等提供程序包完成。

Distributed synchronization abstractions for Euonia: contracts for locks (`ILockFactory` / `ILockProvider`) and semaphores (`ISemaphoreFactory` / `ISemaphoreProvider`) with unified synchronous, asynchronous, timeout and cancellation semantics. Concrete implementations live in the Redis, ZooKeeper, file system and Azure Blob provider packages.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency`
- **NuGet 包**：`Euonia.Concurrency`

## 安装

```bash
dotnet add package Euonia.Concurrency
```

按需选择提供程序：

```bash
dotnet add package Euonia.Concurrency.Redis
dotnet add package Euonia.Concurrency.ZooKeeper
dotnet add package Euonia.Concurrency.FileSystem
dotnet add package Euonia.Concurrency.Azure
```

## 主要功能

- **锁契约**：`ILockProvider` 提供 `Acquire` / `TryAcquire` 及其异步版本。
- **信号量契约**：`ISemaphoreProvider` 以 `maxCount` 限制并发持有者数量。
- **工厂模式**：`ILockFactory.Create(name)` 与 `ISemaphoreFactory.Create(name, maxCount)` 按名称创建实例，便于依赖注入。
- **统一句柄**：`ISynchronizationHandle` 封装释放与句柄丢失监视（`HandleCancellationToken`），实现 `IDisposable` 与 `IAsyncDisposable`。
- **便捷扩展**：`Extensions` 直接在工厂上提供 `Acquire`、`TryAcquire`、`AcquireAsync`、`TryAcquireAsync` 重载。

## 快速开始

```csharp
using Nerosoft.Euonia.Concurrency;

public sealed class InventoryService(ILockFactory lockFactory)
{
    public async Task ReserveAsync(long sku, int quantity, CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create($"inventory:{sku}");

        await using var handle = await locker.AcquireAsync(
            TimeSpan.FromSeconds(10),
            cancellationToken);

        // 临界区：跨进程/跨节点互斥
    }
}
```

使用信号量限制并发：

```csharp
public sealed class ExportService(ISemaphoreFactory semaphoreFactory)
{
    public async Task ExportAsync(CancellationToken cancellationToken)
    {
        var semaphore = semaphoreFactory.Create("report-export", maxCount: 4);

        await using var handle = await semaphore.AcquireAsync(
            TimeSpan.FromMinutes(2),
            cancellationToken);

        // 同时最多 4 个执行者
    }
}
```

尝试获取而不阻塞：

```csharp
var locker = lockFactory.Create("job:daily");
await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero);
if (handle is null)
{
    // 已被其他实例持有
    return;
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ILockFactory` | 按名称创建 `ILockProvider` |
| `ILockProvider` / `ILockProvider<THandle>` | 锁的获取与释放契约 |
| `ISemaphoreFactory` | 按名称与最大并发数创建 `ISemaphoreProvider` |
| `ISemaphoreProvider` / `ISemaphoreProvider<THandle>` | 信号量获取与释放契约 |
| `ISynchronizationHandle` | 同步句柄：释放与句柄丢失监视（`HandleCancellationToken`） |
| `Acquire` / `TryAcquire` / `AcquireAsync` / `TryAcquireAsync` | 工厂扩展方法 |

> 说明：包内只包含抽象与内部等待辅助（忙等待退避、租约监视），不注册任何服务；需要 `ILockFactory` / `ISemaphoreFactory` 时请引用对应提供程序包。

## 相关包

- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 实现
- [`Euonia.Concurrency.ZooKeeper`](../Euonia.Concurrency.ZooKeeper/README.md) —— ZooKeeper 实现
- [`Euonia.Concurrency.FileSystem`](../Euonia.Concurrency.FileSystem/README.md) —— 文件系统实现
- [`Euonia.Concurrency.Azure`](../Euonia.Concurrency.Azure/README.md) —— Azure Blob 租约实现
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
