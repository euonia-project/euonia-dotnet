# Euonia.Concurrency.ZooKeeper API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

基于 ZooKeeper 临时顺序节点的分布式锁与信号量：崩溃自动释放，配置只覆盖连接与超时。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Concurrency.ZooKeeper` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Concurrency.ZooKeeper` |
| 依赖 | `Euonia.Concurrency`（同步抽象）+ ZooKeeperNetEx `3.4.12.4` |
| 完整参考 | [`Source/Euonia.Concurrency.ZooKeeper/README.md`](../../../Source/Euonia.Concurrency.ZooKeeper/README.md) |

## API 参考

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `ZooKeeperSynchronizationFactory(string? connectionString, Action<ZooKeeperSynchronizationOptionsBuilder>? options = null)` | 同时实现锁/信号量工厂 | 节点建在根 `/` 下 |
| `ZooKeeperSynchronizationFactory(ZooKeeperPath directoryPath, string? connectionString, Action<...>? options = null)` | 指定父目录 | 目录不存在时自动创建 |
| `ZooKeeperLockProvider.TryAcquire(TimeSpan timeout = default, CancellationToken = default)` | 同步尝试 | 超时返回 `null` |
| `ZooKeeperLockProvider.Acquire(TimeSpan? timeout = null, CancellationToken = default)` | 同步获取 | 超时抛 `TimeoutException` |
| `ZooKeeperLockProvider.TryAcquireAsync(TimeSpan timeout = default, CancellationToken = default)` | 异步尝试 | 返回 `ZooKeeperSynchronizationHandle?` |
| `ZooKeeperSemaphoreProvider.MaxCount` | 并发度 | 构造时固定，`< 1` 抛异常 |
| `ZooKeeperSynchronizationHandle.DisposeAsync()` | 释放锁/槽位 | 同步 `Dispose` 内部 sync-over-async |
| `ZooKeeperSynchronizationHandle.HandleCancellationToken` | 丢锁感知 | 释放后再访问抛 `ObjectDisposedException` |
| `ZooKeeperPath(string path)` | 路径值对象 | 构造时校验，非法抛 `FormatException` |
| `ZooKeeperSynchronizationOptionsBuilder.SessionTimeout(TimeSpan)` | 会话超时 | 默认 `20s`，必须为正且有限 |
| `ZooKeeperSynchronizationOptionsBuilder.ConnectTimeout(TimeSpan)` | 连接超时 | 默认 `15s` |
| `ZooKeeperSynchronizationOptionsBuilder.AddAuthInfo(string scheme, IReadOnlyList<byte> auth)` | 认证 | 可多次调用 |
| `ZooKeeperSynchronizationOptionsBuilder.AddAccessControl(string scheme, string id, int permissionFlags)` | 节点 ACL | 未设置时 `(world, anyone)` 全权限 |
| `ILockFactory.Create(string name)` / `ISemaphoreFactory.Create(string name, int maxCount)` | 显式接口实现 | 需经接口调用，不能直接用具体类型调 `Create` |
| `Extensions.TryAcquireAsync(this ILockFactory, string, TimeSpan, CancellationToken)` | 一步取锁便捷方法 | 名称、超时、令牌 |

### 配置项

配置节 `ZooKeeperSynchronization`（无常量类，由模块手写读取）：

| 键 | 说明 | 默认值 |
|---|---|---|
| `ConnectionString` | ZooKeeper 连接串 | 必填 |
| `SessionTimeout` | 会话超时（毫秒），`> 0` 才生效 | 构建器默认 `20s` |
| `ConnectTimeout` | 连接超时（毫秒），`> 0` 才生效 | 构建器默认 `15s` |

认证与 ACL 没有配置键，只能通过选项构建器传入。

## 相关文档

- [教程](TUTORIAL.md)
- [`Euonia.Concurrency.ZooKeeper` README](../../../Source/Euonia.Concurrency.ZooKeeper/README.md) —— 完整限制与节点语义
- [`Source/Euonia.Concurrency/README.md`](../../../Source/Euonia.Concurrency/README.md) —— `ILockProvider` / `ISynchronizationHandle` 抽象
- [`Source/Euonia.Modularity/README.md`](../../../Source/Euonia.Modularity/README.md) —— 模块与 `[DependsOn]`
- [Tenancy 手册](../Euonia.Tenancy/API.md) —— 多实例部署时的租户上下文注意事项
- [根 README](../../../README.md)
