# Euonia.Concurrency.ZooKeeper

Euonia 分布式同步的 Apache ZooKeeper 提供程序：基于临时顺序节点（ephemeral sequential node）实现分布式锁与信号量，支持会话超时、连接超时、认证信息与访问控制。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.ZooKeeper`
- **NuGet 包**：`Euonia.Concurrency.ZooKeeper`

## 安装

```bash
dotnet add package Euonia.Concurrency.ZooKeeper
```

## 主要功能

- **分布式锁**：`ZooKeeperLockModule` 注册 `ILockFactory`。
- **分布式信号量**：`ZooKeeperSemaphoreModule` 注册 `ISemaphoreFactory`。
- **会话与连接**：可分别配置 `SessionTimeout` 与 `ConnectTimeout`。
- **安全**：`ZooKeeperSynchronizationOptionsBuilder` 支持 `AddAuthInfo` 与 `AddAccessControl`。
- **崩溃自动释放**：进程断开后临时节点失效，锁自动释放。

## 快速开始

配置节：

```json
{
  "ZooKeeperSynchronization": {
    "ConnectionString": "localhost:2181",
    "SessionTimeout": 20000,
    "ConnectTimeout": 15000
  }
}
```

声明模块依赖：

```csharp
using Nerosoft.Euonia.Concurrency.ZooKeeper;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(ZooKeeperLockModule))]
public sealed class ClusterModule : ModuleContextBase
{
}
```

使用锁：

```csharp
public sealed class ElectionService(ILockFactory lockFactory)
{
    public async Task<bool> TryBecomeLeaderAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("cluster:leader");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);
        return handle is not null;
    }
}
```

自定义超时与认证（不经过 DI 时直接构造）：

```csharp
var factory = new ZooKeeperSynchronizationFactory(
    "localhost:2181",
    builder => builder
        .SessionTimeout(TimeSpan.FromSeconds(30))
        .ConnectTimeout(TimeSpan.FromSeconds(10))
        .AddAuthInfo("digest", "user:password"u8.ToArray()));

// Create 为显式接口实现，必须经接口调用
var lockProvider = ((ILockFactory)factory).Create("cluster:leader");
```

## 配置项

配置节 `ZooKeeperSynchronization` 由 `ZooKeeperSynchronizationModule`（`internal`，被锁/信号量模块共同依赖）在首次解析时读取，键名无常量类。

| 配置键 | 默认值 | 说明 |
|--------|--------|------|
| `ZooKeeperSynchronization:ConnectionString` | 必填 | ZooKeeper 连接字符串，如 `localhost:2181` |
| `ZooKeeperSynchronization:SessionTimeout` | 构建器默认 `20s` | 会话超时（毫秒），`> 0` 时才覆盖默认值 |
| `ZooKeeperSynchronization:ConnectTimeout` | 构建器默认 `15s` | 连接超时（毫秒），`> 0` 时才覆盖默认值 |

```bash
# .NET 原生：双下划线表示层级
ZooKeeperSynchronization__ConnectionString=localhost:2181
ZooKeeperSynchronization__SessionTimeout=20000
ZooKeeperSynchronization__ConnectTimeout=15000

# 仓库 alias：大写单下划线（Euonia.Modularity）
ZOOKEEPERSYNCHRONIZATION_CONNECTIONSTRING=localhost:2181
ZOOKEEPERSYNCHRONIZATION_SESSIONTIMEOUT=20000
ZOOKEEPERSYNCHRONIZATION_CONNECTTIMEOUT=15000
```

认证（`AddAuthInfo`）与 ACL（`AddAccessControl`）没有配置键，只能在构造 `ZooKeeperSynchronizationFactory` 时通过构建器委托设置。

## 核心类型

| 类型 | 作用 |
|------|------|
| `ZooKeeperLockModule` | 注册 `ILockFactory` |
| `ZooKeeperSemaphoreModule` | 注册 `ISemaphoreFactory` |
| `ZooKeeperSynchronizationFactory` | 同时实现 `ILockFactory` 与 `ISemaphoreFactory`，按名称创建 provider |
| `ZooKeeperSynchronizationOptionsBuilder` | `SessionTimeout`、`ConnectTimeout`、`AddAuthInfo`、`AddAccessControl` |
| `ZooKeeperLockProvider` / `ZooKeeperSemaphoreProvider` | ZooKeeper 上的锁与信号量实现，节点前缀分别为 `lock-` / `semaphore-` |
| `ZooKeeperSynchronizationHandle` | 同步句柄；释放即释放锁或许可 |
| `ZooKeeperPath` / `ZooKeeperNodeHandle` | 节点路径（构造时校验）与节点句柄辅助 |

## 使用示例

同一进程内同时使用锁与信号量（无模块系统时手工构造）：

```csharp
using System.Text;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.ZooKeeper;

var factory = new ZooKeeperSynchronizationFactory(
    "localhost:2181",
    builder => builder
        .SessionTimeout(TimeSpan.FromSeconds(30))
        .ConnectTimeout(TimeSpan.FromSeconds(10))
        .AddAuthInfo("digest", Encoding.UTF8.GetBytes("user:password")));

// 互斥：最多 1 个持有者
await using (var handle = await ((ILockFactory)factory).Create("orders:nightly-job")
                                             .TryAcquireAsync(TimeSpan.FromSeconds(5)))
{
    if (handle is not null)
    {
        await RunNightlyJobAsync();
    }
}

// 并发限流：最多 4 个持有者
await using (var slot = await ((ISemaphoreFactory)factory).Create("orders:export", 4).TryAcquireAsync(TimeSpan.FromSeconds(5)))
{
    if (slot is not null)
    {
        await ExportAsync();
    }
}
```

把节点建在子目录下（默认建在根 `/`）：

```csharp
var underDir = new ZooKeeperSynchronizationFactory(
    new ZooKeeperPath("/euonia/locks"),
    "localhost:2181");
```

## 注意事项 / 限制

- 连接字符串为 `null` 时构造函数抛 `ArgumentNullException`；配置节缺失会在首次解析 `ILockFactory` 时暴露，而不是启动期。
- 锁与信号量的节点名会经 `ZooKeeperPath` 规范化（非法字符替换为 `_`、必要时附加哈希）；`Name` 返回的是含前导 `/` 的完整路径，不是传入的名称。
- 信号量 `maxCount < 1` 直接抛 `ArgumentOutOfRangeException`；`maxCount` 在创建时固定。
- 同步 API（`Acquire` / `TryAcquire`）内部对 ZooKeeperNetEx 的异步接口做 sync-over-async，**不要在异步方法中优先使用**。
- `SessionTimeout` 越小，持锁进程崩溃后释放越快，但短暂网络抖动导致锁丢失的风险越高；不要小于 ZooKeeper 的 2 倍 tick 时间。
- 未设置 `AddAccessControl` 时节点 ACL 为 `(world, anyone)` 全权限；生产环境应显式配置或依赖已加固的 ZooKeeper。
- 未传 `AddAuthInfo` / `AddAccessControl` 时模块版本无法通过配置节补充，需要认证必须自行构造 `ZooKeeperSynchronizationFactory`。

## 常见问题与故障排查

### 启动正常，第一次取锁抛 `ArgumentNullException (connectionString)`

**原因**：`ZooKeeperSynchronizationModule` 只从 `ZooKeeperSynchronization:ConnectionString` 读值，缺失时把 `null` 传给工厂，工厂构造即抛错（`ZooKeeperSynchronizationModule.cs:19,23-27`）。
**解决**：补齐配置键；注意该异常发生在**首次解析 `ILockFactory`** 时，不在启动期。

### `TimeoutException: Timed out connecting to ZooKeeper after 15000ms`

**原因**：连接建立超过 `ConnectTimeout`（`ZooKeeperConnection.cs:137-141`），常见于地址写错、网络策略拦截、集群未选主。
**解决**：先用 `zkCli` 验证地址可达；把 `ZooKeeperSynchronization:ConnectTimeout` 调大只在网络确实慢时有效，不要用它掩盖不可达。

### `InvalidOperationException: Failed to connect to ZooKeeper. State: ...`

**原因**：连接首次建立就收到非 `SyncConnected` 事件（`ZooKeeperConnection.cs:279`），例如端口上不是 ZooKeeper 服务、或 `SessionTimeout` 小于集群 2 倍 tick。
**解决**：核对端口与 `SessionTimeout`（默认 20 秒）；错误状态会直接出现在异常消息里。

### 等待锁期间抛 `InvalidOperationException: Lost connection to ZooKeeper`

**原因**：等待期间会话过期或断连，重连窗口取 `SessionTimeout`（`ZooKeeperSynchronizationHelper.cs:129-131`）。
**解决**：把整个临界区当作可失败重试；用 `handle.HandleCancellationToken` 感知持锁期间的丢失（`ZooKeeperSynchronizationHandle.cs:28`）。

### `TryAcquireAsync` 返回 `null` 而不是抛异常

**原因**：等待超过传入的 `timeout` 时按设计返回 `null`（`ZooKeeperSynchronizationHelper.cs:138-141`）；同步 `Acquire` 才会抛 `TimeoutException`。
**解决**：选举/降级用 `TryAcquire`，必须串行的临界区才用 `Acquire`；不要把 `null` 当成异常处理。

## 最佳实践

- **连接参数走配置节，认证与 ACL 走代码**：`ConnectionString` / `SessionTimeout` / `ConnectTimeout` 可配置，`AddAuthInfo` / `AddAccessControl` 只能构造工厂时传，部署时别指望配置节。
- **用子目录隔离业务**：`new ZooKeeperSynchronizationFactory(new ZooKeeperPath("/euonia/locks"), ...)`，避免与其他应用抢根节点。
- **临界区必须幂等**：会话过期会丢锁，重试时不能重复扣款/重复发消息。
- **`maxCount` 创建后固定**：信号量并发度是节点语义的一部分，改值等于新建一个信号量。
- **异步路径优先异步 API**：同步 `Acquire` / `TryAcquire` 内部是 sync-over-async，在 ASP.NET Core 请求线程上使用会放大线程池压力。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；包版本 `2026.5.0`（`project.props:3`）。
- **关键依赖**：`ZooKeeperNetEx` `3.4.12.4`（`Directory.Packages.props:62`），对应 ZooKeeper 3.4 客户端协议；3.5+ 的容器节点等特性不可用。
- **升级注意**：连接按“连接串 + 超时 + 认证”作为池键缓存，会话最长保留 10 分钟（`ZooKeeperConnection.cs:20`）；改这些参数会产生新池条目而不是复用旧会话。
- **已知边界**：模块版本无法从配置注入认证；顺序节点名经 `ZooKeeperPath` 规范化，`Name` 返回完整路径。

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象与扩展方法
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器
- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 同步提供程序
- [`Euonia.Concurrency.FileSystem`](../Euonia.Concurrency.FileSystem/README.md) —— 单机文件锁提供程序

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
