# Euonia.Concurrency

Euonia 的分布式同步抽象：定义锁（`ILockFactory` / `ILockProvider`）与信号量（`ISemaphoreFactory` / `ISemaphoreProvider`）契约，并统一同步、异步、超时与取消语义；具体实现由 Redis、ZooKeeper、文件系统或 Azure Blob 提供程序包完成。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency`
- **NuGet 包**：`Euonia.Concurrency`
- **依赖**：`Euonia.Core`（`Nerosoft.Euonia.Threading` 中的 `TimeoutValue`）

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

## 主要功能

- **锁契约**：`ILockProvider` 提供 `Acquire`/`TryAcquire` 及异步版本，超时抛 `TimeoutException` 或返回 `null`。
- **信号量契约**：`ISemaphoreProvider` 以 `MaxCount` 限制并发持有者数量。
- **工厂模式**：`ILockFactory.Create(name)` 与 `ISemaphoreFactory.Create(name, maxCount)` 按名称创建实例，便于依赖注入。
- **统一句柄**：`ISynchronizationHandle` 封装释放与句柄丢失监视（`HandleCancellationToken`），实现 `IDisposable` 与 `IAsyncDisposable`。
- **便捷扩展**：`Extensions` 直接在工厂上提供按名称获取的重载，省去先 `Create` 再获取。

## 核心类型

| 类型 | 作用 |
|------|------|
| `ILockFactory` | 按名称创建 `ILockProvider` |
| `ILockProvider` / `ILockProvider<THandle>` | 锁的获取与释放契约 |
| `ISemaphoreFactory` | 按名称与最大并发数创建 `ISemaphoreProvider` |
| `ISemaphoreProvider` / `ISemaphoreProvider<THandle>` | 信号量获取与释放契约 |
| `ISynchronizationHandle` | 同步句柄：释放与句柄丢失监视（`HandleCancellationToken`） |
| `Extensions` | 工厂级 `TryAcquire`/`Acquire`/`TryAcquireAsync`/`AcquireAsync` 重载 |
| `TimeoutValue` | 支持“无限”的超时值（来自 `Nerosoft.Euonia.Threading`） |

## 选项（代码配置）

抽象层本身不读取 `IConfiguration`，也没有 `*Options` 类型或配置节；行为参数在调用点或由具体提供程序包决定。

| 参数 / 来源 | 默认值 | 说明 |
|-------------|--------|------|
| `Acquire` 的 `timeout` | `Timeout.InfiniteTimeSpan` | 超时抛 `TimeoutException` |
| `TryAcquire` 的 `timeout` | `TimeSpan.Zero` | 非阻塞，失败返回 `null` |
| `cancellationToken` | `default` | 取消等待并抛出 `OperationCanceledException` |
| `ISemaphoreFactory.Create(name, maxCount)` | 无默认 | `maxCount` 为可同时授予的票据数 |
| `TimeoutValue` 重载 | 无 | 支持无限等待且避免与 `TimeSpan` 重载混淆 |
| 提供程序选项 | 各包不同 | Redis/Azure 的过期、续租、忙等待参数由对应包提供 |

## 注意事项 / 限制

- **抽象层不注册服务**：本包不包含 `IServiceCollection` 扩展；需要 `ILockFactory`/`ISemaphoreFactory` 时必须引用具体提供程序包并注册。
- **并非所有实现都支持信号量**：文件系统与 Azure Blob 提供程序只实现锁（`ILockFactory`）；Redis 与 ZooKeeper 的工厂同时实现 `ILockFactory` 和 `ISemaphoreFactory`。
- **句柄必须释放**：锁/票据通过释放句柄归还；进程崩溃时依赖实现自身的过期或句柄回收，恢复时间取决于提供程序的租约/过期配置。
- **超时语义不同**：`Acquire` 超时抛异常，`TryAcquire` 超时返回 `null`；不要混用两者的错误处理。
- **`HandleCancellationToken` 可能不可取消**：提供程序不支持句柄丢失监视时返回 `CancellationToken.None`，可用 `CanBeCanceled` 判断。
- **取消只影响等待**：取消获取不会自动释放已获取的句柄，仍需 `await using` 或显式 `Dispose`。

## 相关包

- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 锁与信号量实现
- [`Euonia.Concurrency.ZooKeeper`](../Euonia.Concurrency.ZooKeeper/README.md) —— ZooKeeper 实现
- [`Euonia.Concurrency.FileSystem`](../Euonia.Concurrency.FileSystem/README.md) —— 文件系统实现
- [`Euonia.Concurrency.Azure`](../Euonia.Concurrency.Azure/README.md) —— Azure Blob 租约实现
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 常见问题与故障排查

### Unable to resolve service for type 'ILockFactory'
原因：抽象包只定义接口，不注册任何服务；未引用/未加载具体提供程序模块时容器无法解析。
解决：引用 `Euonia.Concurrency.Redis`/`FileSystem`/`Azure` 等实现包，并在模块上声明 `[DependsOn(typeof(RedisLockModule))]` 等依赖。

### Acquire 抛 TimeoutException: Timeout exceeded when trying to acquire the lock
原因：锁被其他实例持有，或传入的 `timeout` 小于实际等待时间；`Acquire` 超时按异常处理。
解决：先确认是否必须阻塞；可用 `TryAcquire`/`TryAcquireAsync`（失败返回 `null`），或增大超时并缩短临界区。

### 注入 ISemaphoreFactory 失败
原因：该能力并非所有提供程序都实现；文件系统与 Azure Blob 包只实现 `ILockFactory`。
解决：信号量改用 `Euonia.Concurrency.Redis` 或 `Euonia.Concurrency.ZooKeeper` 并加载对应 `*SemaphoreModule`。

### HandleCancellationToken 一直不会取消
原因：并非所有锁实现都支持句柄丢失监视；不支持时返回 `CancellationToken.None`。
解决：先检查 `CanBeCanceled`；不要把该令牌当作任务取消令牌，业务取消仍应使用方法参数上的 `cancellationToken`。

### 锁在临界区完成前就被释放
原因：句柄在 `await using` 作用域结束时释放；提前 `return`、把句柄存到字段或让作用域覆盖不到异步后续都会导致提前释放。
解决：让整个临界区位于句柄作用域内，不要跨作用域传递句柄；长任务定期检查 `HandleCancellationToken`。

## 最佳实践

- **按资源粒度命名**：锁名精确到业务资源（如 `order:{id}:ship`）并加应用前缀，避免全局大锁。
- **始终设置超时与取消**：`Acquire` 用有限超时，配合请求 `cancellationToken`；不要在请求线程上无限等待。
- **优先 TryAcquire**：可跳过的工作用 `TryAcquire`（`TimeSpan.Zero`）快速失败，避免堆积等待者。
- **临界区尽量短**：锁内只做必要的互斥操作；耗时 IO 或外部调用移到锁外，或改用信号量限制并发。
- **监控等待时间**：记录获取耗时与超时率，持续升高通常意味着锁粒度过粗或持有时间过长。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：`Euonia.Core`（`Nerosoft.Euonia.Threading.TimeoutValue`）；提供程序包各自依赖 Redis/ZooKeeper/Azure SDK。
- **已知边界**：抽象层不注册服务、不读取配置；信号量与句柄丢失监视并非所有实现都支持。
- **升级注意**：升级抽象包时同步升级提供程序包，避免接口实现缺失；锁/票据本身不持久化业务数据，无数据迁移。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
