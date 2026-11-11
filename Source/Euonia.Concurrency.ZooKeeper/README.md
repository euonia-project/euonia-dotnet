# Euonia.Concurrency.ZooKeeper

Euonia 分布式同步的 Apache ZooKeeper 提供程序：基于临时顺序节点（ephemeral sequential node）实现分布式锁与信号量，支持会话超时、连接超时、认证信息与访问控制。

Apache ZooKeeper provider for Euonia distributed synchronization: distributed locks and semaphores built on ephemeral sequential nodes, with session timeout, connect timeout, authentication and access control.

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

自定义超时与认证：

```csharp
var factory = new ZooKeeperSynchronizationFactory(
    "localhost:2181",
    builder => builder
        .SessionTimeout(TimeSpan.FromSeconds(30))
        .ConnectTimeout(TimeSpan.FromSeconds(10))
        .AddAuthInfo("digest", "user:password"u8.ToArray()));
```

## 配置项

| 配置键 | 默认值 | 说明 |
|--------|--------|------|
| `ZooKeeperSynchronization:ConnectionString` | 必填 | ZooKeeper 连接字符串 |
| `ZooKeeperSynchronization:SessionTimeout` | 构建器默认 `20s` | 会话超时（毫秒），`> 0` 时生效 |
| `ZooKeeperSynchronization:ConnectTimeout` | 构建器默认 `15s` | 连接超时（毫秒），`> 0` 时生效 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `ZooKeeperLockModule` | 注册 `ILockFactory` |
| `ZooKeeperSemaphoreModule` | 注册 `ISemaphoreFactory` |
| `ZooKeeperSynchronizationFactory` | 同时实现 `ILockFactory` 与 `ISemaphoreFactory` |
| `ZooKeeperSynchronizationOptionsBuilder` | 会话/连接超时、认证与 ACL |
| `ZooKeeperLockProvider` / `ZooKeeperSemaphoreProvider` | ZooKeeper 上的锁与信号量实现 |
| `ZooKeeperPath` / `ZooKeeperNodeHandle` | 节点路径与句柄辅助 |

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
