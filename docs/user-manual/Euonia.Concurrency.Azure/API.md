# Euonia.Concurrency.Azure API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

用 Azure Blob 租约实现分布式锁：每个锁名对应一个 Blob，靠租约互斥并自动续约。本页用“月末对账任务只允许一个实例执行”演示容器注入、选项、模块接线与租约丢失处理。

- **NuGet 包**：`Euonia.Concurrency.Azure`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Azure`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Concurrency.Azure/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AzureLockModule` | 把工厂注册为 `ILockFactory` | 依赖容器中的 `BlobContainerClient` |
| `AzureSynchronizationFactory(BlobContainerClient, Action<...>?)` | `ILockFactory` 实现 | 容器必填 |
| `AzureLockProvider(BlobBaseClient, Action<...>?)` | 对单个 Blob 加锁 | Blob 客户端必填 |
| `AzureLockProvider(BlobContainerClient, string name, Action<...>?)` | 按名称定位 Blob | 名称必填 |
| `AzureLockProvider.Acquire/TryAcquire` | 同步获取 | 超时/失败语义同接口 |
| `AzureLockProvider.AcquireAsync/TryAcquireAsync` | 异步获取 | 支持 `TimeoutValue` 重载 |
| `AzureSynchronizationHandle.LeaseId` | 底层租约 ID | 仅诊断用 |
| `AzureSynchronizationHandle.HandleCancellationToken` | 租约丢失监视 | 丢失时取消 |
| `AzureSynchronizationOptions.Duration` | 租约时长 | 默认 30 秒 |
| `AzureSynchronizationOptions.RenewalCadence` | 续约节奏 | 默认 `Duration / 3` |
| `AzureSynchronizationOptions.MinBusyWaitSleepTime` | 忙等待最小值 | 默认 250ms |
| `AzureSynchronizationOptions.MaxBusyWaitSleepTime` | 忙等待最大值 | 默认 1s |
| `AzureSynchronizationOptionsBuilder.Duration(TimeSpan)` | 设置租约时长 | 15–60 秒或无限 |
| `AzureSynchronizationOptionsBuilder.RenewalCadence(TimeSpan)` | 设置续约节奏 | 必须小于 `Duration`，可无限禁用 |
| `AzureSynchronizationOptionsBuilder.BusyWaitSleepTime(min, max)` | 设置退避区间 | 最小有限，最大有限且 ≥ 最小 |
| `ISynchronizationHandle.DisposeAsync` | 释放租约/清理 Blob | `await using` 自动调用 |

### 选项（代码配置）

本包不读取 `IConfiguration`，没有配置节；容器客户端由宿主注入，租约参数通过 `Action<AzureSynchronizationOptionsBuilder>` 设置。

> 不要为本包编写 `appsettings` 节；连接串由宿主从 `ConnectionStrings` 读取并构造 `BlobContainerClient`。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、选项与限制](../../../Source/Euonia.Concurrency.Azure/README.md)
- [Euonia.Concurrency 用户手册](../Euonia.Concurrency/API.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/API.md)
- [根 README](../../../README.md)
