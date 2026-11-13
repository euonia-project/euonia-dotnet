# Euonia.Concurrency API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 的分布式同步抽象：用 `ILockFactory` 获取互斥锁、`ISemaphoreFactory` 限制并发数，业务代码只依赖接口，具体实现由文件系统/Redis/Azure/ZooKeeper 提供程序包决定。本页用“库存扣减 + 报表导出限流”演示。

- **NuGet 包**：`Euonia.Concurrency`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Concurrency/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `ILockFactory.Create(string name)` | 按名称创建锁 | 名称必填 |
| `ILockProvider.Name` | 锁名称 | 由实现返回 |
| `ILockProvider.Acquire(timeout?, ct)` | 阻塞获取，超时抛异常 | `timeout` 默认无限 |
| `ILockProvider.TryAcquire(timeout, ct)` | 尝试获取 | `timeout` 默认 0，失败返回 `null` |
| `ILockProvider.AcquireAsync(timeout?, ct)` | 异步获取 | 超时抛 `TimeoutException` |
| `ILockProvider.TryAcquireAsync(timeout, ct)` | 异步尝试 | 失败返回 `null` |
| `ILockProvider.TryAcquireAsync(TimeoutValue, ct)` | 支持“无限”的超时重载 | `TimeoutValue` 来自 `Euonia.Core` |
| `ISemaphoreFactory.Create(name, maxCount)` | 创建信号量 | `maxCount >= 1` |
| `ISemaphoreProvider.Name/MaxCount` | 名称与并发上限 | 由实现返回 |
| `ISemaphoreProvider.Acquire/TryAcquire` | 获取/尝试获取票据 | 语义同锁 |
| `ISynchronizationHandle.HandleCancellationToken` | 句柄丢失监视 | 不支持时返回 `None` |
| `ISynchronizationHandle.Dispose/DisposeAsync` | 释放锁/票据 | `await using` 自动调用 |
| `Extensions.Acquire(name, timeout?, ct)` | 工厂级快捷获取 | 内部 `Create` + `Acquire` |
| `Extensions.TryAcquire(name, timeout, ct)` | 工厂级尝试 | 失败返回 `null` |
| `Extensions.Acquire(name, maxCount, timeout?, ct)` | 信号量快捷获取 | 需要 `ISemaphoreFactory` |
| `Extensions.TryAcquire(name, maxCount, timeout, ct)` | 信号量尝试 | 失败返回 `null` |

### 选项（代码配置）

抽象层不读取 `IConfiguration`，没有配置节或 `*Options` 类型；超时、取消与 `maxCount` 在调用点传入。提供程序参数见各项目 README。

> 不要为 `Euonia.Concurrency` 编写 `appsettings` 节；文件系统与 Redis 的配置属于各自提供程序包。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：接口、扩展方法与限制](../../../Source/Euonia.Concurrency/README.md)
- [Euonia.Concurrency.FileSystem 用户手册](../Euonia.Concurrency.FileSystem/API.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/API.md)
- [Euonia.Concurrency.Azure 用户手册](../Euonia.Concurrency.Azure/API.md)
- [根 README](../../../README.md)
