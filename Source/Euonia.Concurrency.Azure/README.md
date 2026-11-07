# Euonia.Concurrency.Azure

Euonia 分布式同步的 Azure 提供程序：基于 Azure Blob 租约（lease）实现分布式锁，支持租约时长、自动续约节奏与忙等待退避配置。

Azure provider for Euonia distributed synchronization: distributed locks based on Azure Blob leases, with configurable lease duration, automatic renewal cadence and busy-wait backoff.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Azure`
- **NuGet 包**：`Euonia.Concurrency.Azure`

## 安装

```bash
dotnet add package Euonia.Concurrency.Azure
```

## 主要功能

- **Blob 租约锁**：每个锁名称对应一个 Blob，通过获取/续租租约实现互斥。
- **自动续约**：`RenewalCadence` 控制续约频率，可设为无限以禁用。
- **租约时长**：`Duration` 支持无限时长或 15–60 秒区间。
- **忙等待退避**：`BusyWaitSleepTime(min, max)` 在竞争时随机休眠，避免锁饥饿。
- **只提供锁**：实现 `ILockFactory`（不提供信号量）。

## 快速开始

注册容器客户端与锁工厂：

```csharp
using Azure.Storage.Blobs;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.Azure;

var container = new BlobContainerClient(
    "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    "locks");

services.AddSingleton(container);
services.AddSingleton<ILockFactory>(provider => new AzureSynchronizationFactory(
    provider.GetRequiredService<BlobContainerClient>(),
    builder => builder
        .Duration(TimeSpan.FromSeconds(30))
        .RenewalCadence(TimeSpan.FromSeconds(10))
        .BusyWaitSleepTime(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1))));
```

也可以声明模块依赖（要求容器中已能解析 `BlobContainerClient` 与 `Action<AzureSynchronizationOptionsBuilder>`）：

```csharp
[DependsOn(typeof(AzureLockModule))]
public sealed class StorageModule : ModuleContextBase
{
}
```

使用锁：

```csharp
public sealed class ReconciliationService(ILockFactory lockFactory)
{
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("reconcile");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);
        if (handle is null)
        {
            return;
        }

        // 仅一个实例执行
    }
}
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Duration` | `30s` | 租约时长；无限时长表示不自动续约 |
| `RenewalCadence` | 时长的 1/3 | 续约节奏；`Timeout.InfiniteTimeSpan` 表示禁用 |
| `BusyWaitSleepTime` | `[250ms, 1s]` | 获取租约失败时的随机休眠区间 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `AzureSynchronizationFactory` | `ILockFactory` 实现，绑定 `BlobContainerClient` |
| `AzureLockProvider` / `AzureSynchronizationHandle` | Blob 租约锁与句柄 |
| `AzureSynchronizationOptionsBuilder` | `Duration`、`RenewalCadence`、`BusyWaitSleepTime` |
| `AzureSynchronizationOptions` | 解析后的租约选项 |
| `AzureLockModule` | 将 `AzureSynchronizationFactory` 注册为 `ILockFactory` |

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 实现
- [`Euonia.Concurrency.ZooKeeper`](../Euonia.Concurrency.ZooKeeper/README.md) —— ZooKeeper 实现
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
