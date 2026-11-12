# Euonia.Concurrency.Azure

Euonia 分布式同步的 Azure 提供程序：基于 Azure Blob 租约（lease）实现分布式锁，可配置租约时长、自动续约节奏与忙等待退避区间。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Azure`
- **NuGet 包**：`Euonia.Concurrency.Azure`
- **依赖**：`Euonia.Concurrency`、`Euonia.Modularity`、`Azure.Storage.Blobs`

## 安装

```bash
dotnet add package Euonia.Concurrency.Azure
```

## 快速开始

注册容器客户端与锁工厂（代码配置，无配置节）：

```csharp
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.Azure;

var container = new BlobContainerClient(
    "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    "locks");

services.AddSingleton(container);   // 容器需预先创建
services.AddSingleton<ILockFactory>(provider => new AzureSynchronizationFactory(
    provider.GetRequiredService<BlobContainerClient>(),
    builder => builder
        .Duration(TimeSpan.FromSeconds(30))
        .RenewalCadence(TimeSpan.FromSeconds(10))
        .BusyWaitSleepTime(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1))));
```

也可以声明模块依赖；`AzureLockModule` 会把 `AzureSynchronizationFactory` 注册为 `ILockFactory`，并从容器解析 `BlobContainerClient`：

```csharp
using Nerosoft.Euonia.Concurrency.Azure;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(AzureLockModule))]
public sealed class StorageModule : ModuleContextBase
{
}
```

> 通过模块注册时，若要覆盖默认租约参数，可同时向容器注册 `Action<AzureSynchronizationOptionsBuilder>`：
>
> ```csharp
> services.AddSingleton<Action<AzureSynchronizationOptionsBuilder>>(_ => builder =>
> {
>     builder.Duration(TimeSpan.FromSeconds(30))
>            .RenewalCadence(TimeSpan.FromSeconds(10));
> });
> ```

## 主要功能

- **Blob 租约锁**：每个锁名称对应一个 Blob，通过获取/续租租约实现互斥。
- **自动续约**：`RenewalCadence` 控制续约频率，设为 `Timeout.InfiniteTimeSpan` 可禁用。
- **租约时长**：`Duration` 支持无限时长或 15–60 秒区间。
- **忙等待退避**：`BusyWaitSleepTime(min, max)` 在竞争时随机休眠，降低“同步化”抢占。
- **句柄丢失监视**：租约丢失时 `ISynchronizationHandle.HandleCancellationToken` 取消。
- **只提供锁**：本包实现 `ILockFactory`，不实现 `ISemaphoreFactory`。

## 核心类型

| 类型 | 作用 |
|------|------|
| `AzureSynchronizationFactory` | `ILockFactory` 实现，绑定 `BlobContainerClient` 与选项回调 |
| `AzureLockProvider` / `AzureSynchronizationHandle` | Blob 租约锁与句柄 |
| `AzureSynchronizationOptionsBuilder` | `Duration`、`RenewalCadence`、`BusyWaitSleepTime` |
| `AzureSynchronizationOptions` | 解析后的租约选项（记录类型） |
| `AzureLockModule` | 把 `AzureSynchronizationFactory` 注册为 `ILockFactory` |

## 选项（代码配置）

本包不读取 `IConfiguration`，没有配置节；选项通过 `AzureSynchronizationFactory` 或 `AzureLockProvider` 的 `Action<AzureSynchronizationOptionsBuilder>` 回调设置。

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Duration` | `30 秒` | 租约时长；无限时长表示不自动续约；非无限时长限制在 15–60 秒 |
| `RenewalCadence` | `Duration / 3` | 自动续约间隔；可设为 `Timeout.InfiniteTimeSpan` 禁用；必须小于 `Duration` |
| `BusyWaitSleepTime` | `[250ms, 1s]` | 获取失败时的随机休眠区间；最小值不能无限，最大值必须有限且不小于最小值 |

> 不要为本包编写 `appsettings` 配置节；连接串由宿主自行读取并构造 `BlobContainerClient`。

## 使用示例

在业务服务中使用锁，并在租约丢失时提前退出：

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Concurrency;

public sealed class ReconciliationService(ILockFactory lockFactory, ILogger<ReconciliationService> logger)
{
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("reconcile");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);

        if (handle is null)
        {
            return; // 其他实例正在执行
        }

        using var leaseLost = handle.HandleCancellationToken.Register(
            () => logger.LogWarning("Azure blob lease was lost."));

        try
        {
            // 仅一个实例执行；长任务应通过 leaseLost 感知租约丢失
            await RunReconciliationAsync(cancellationToken);
        }
        finally
        {
            // await using 释放租约；若 Blob 由本锁创建，也会被清理
        }
    }
}
```

直接对单个 Blob 加锁（适合由外部管理的锁文件）：

```csharp
using Azure.Storage.Blobs;
using Nerosoft.Euonia.Concurrency.Azure;

var blobClient = container.GetBlobClient("jobs/reconcile.lock");
var provider = new AzureLockProvider(blobClient, builder => builder
    .Duration(TimeSpan.FromSeconds(45)));

await using var handle = await provider.AcquireAsync(TimeSpan.FromSeconds(5));
```

## 注意事项 / 限制

- **容器需预先存在**：本包只会在容器内按需创建锁 Blob，不会创建 Blob 容器；容器不存在时获取失败。
- **只支持锁**：没有信号量实现；需要限流式并发请使用 `Euonia.Concurrency.Redis` 或 `Euonia.Concurrency.ZooKeeper`。
- **锁名称会被规范化**：名称会转换为合法 Blob 名；过长或含特殊字符时使用哈希后缀，存储模拟器下上限更短（256）。
- **租约时长边界**：非无限时长必须在 15–60 秒之间；`RenewalCadence` 必须小于 `Duration`，否则构建选项时抛 `ArgumentOutOfRangeException`。
- **无限租约不续约**：`Duration` 为无限时不会自动续约；进程异常退出后该租约不会自行过期。
- **Blob 生命周期**：锁创建的 Blob 在释放时删除；若 Blob 由外部创建，本包只释放租约，不删除 Blob。

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 实现
- [`Euonia.Concurrency.ZooKeeper`](../Euonia.Concurrency.ZooKeeper/README.md) —— ZooKeeper 实现
- [`Euonia.Concurrency.FileSystem`](../Euonia.Concurrency.FileSystem/README.md) —— 文件系统实现
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。

## 常见问题与故障排查

### 启动时报 ContainerNotFound
原因：本包只在容器内创建锁 Blob，不会创建 Blob 容器；容器不存在或名称拼错时获取租约失败。
解决：在基础设施/启动阶段创建容器（或 `CreateIfNotExists`），确认 `BlobContainerClient` 指向正确账户与容器。

### TryAcquire 返回 null，或 Acquire 等待后超时
原因：同一锁 Blob 的租约被其他实例持有；竞争时按 `BusyWaitSleepTime` 区间随机休眠重试。
解决：这通常是预期互斥；需要立即返回时用 `TryAcquire(TimeSpan.Zero)`；需要等待时给 `Acquire` 足够超时并缩短持锁时间。

### 配置 Duration 时抛 ArgumentOutOfRangeException
原因：非无限租约必须在 15–60 秒之间；`RenewalCadence` 必须小于 `Duration`，忙等待的最大值必须是有限值且不小于最小值。
解决：使用 `Timeout.InfiniteTimeSpan` 或 15–60 秒内的值，并保证 `RenewalCadence < Duration`、`max >= min`。

### 通过 AzureLockModule 注册后自定义选项没有生效
原因：模块只注册 `AzureSynchronizationFactory`，不会自动读取代码里的 builder；选项来自容器中的 `Action<AzureSynchronizationOptionsBuilder>`。
解决：注册该委托（`services.AddSingleton<Action<AzureSynchronizationOptionsBuilder>>(...)`），或像快速开始那样手工构造工厂。

### 长任务执行中租约丢失但业务没有感知
原因：租约因续约失败/网络中断而丢失；只有读取 `HandleCancellationToken` 才能感知。
解决：用 `handle.HandleCancellationToken.Register(...)` 触发取消并及时退出；不要让无限租约掩盖进程崩溃。

## 最佳实践

- **按环境隔离容器**：开发/测试/生产使用不同容器或名称前缀，避免不同环境争用同一锁 Blob。
- **租约时长匹配任务**：任务通常短于租约时用 30 秒并保持续约；任务不可中断时不要用无限租约掩盖故障。
- **最小权限**：容器客户端只需 Blob 读写/租约权限，不要把账户密钥写进代码。
- **处理瞬时错误**：对 `RequestFailedException` 区分租约竞争与存储错误；存储错误交给 Azure SDK 重试策略并记录。
- **清理锁 Blob**：由锁创建的 Blob 在释放时删除；外部创建的 Blob 只释放租约，需自行管理生命周期。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：`Azure.Storage.Blobs`、`Euonia.Concurrency`、`Euonia.Modularity`。
- **已知边界**：只实现锁，不实现信号量；不创建容器；非无限租约限制 15–60 秒；存储模拟器下 Blob 名称上限更短（256）。
- **升级注意**：锁状态在 Azure 侧，升级不迁移数据；改动锁名前缀会让新旧实例锁不同 Blob，滚动升级期间需保持命名一致。
