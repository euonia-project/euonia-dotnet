# Euonia.Concurrency.Redis

Euonia 分布式同步的 Redis 提供程序：基于 `StackExchange.Redis` 实现分布式锁与信号量，支持租约自动续期、最短有效时间与忙等待退避策略。

Redis provider for Euonia distributed synchronization: distributed locks and semaphores backed by `StackExchange.Redis`, with automatic lease renewal, minimum validity time and busy-wait backoff.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Redis`
- **NuGet 包**：`Euonia.Concurrency.Redis`

## 安装

```bash
dotnet add package Euonia.Concurrency.Redis
```

## 主要功能

- **分布式锁**：`RedisLockModule` 注册 `ILockFactory`。
- **分布式信号量**：`RedisSemaphoreModule` 注册 `ISemaphoreFactory`。
- **租约续期**：按 `ExtensionCadence` 自动延长持有期，并通过 `MinValidityTime` 保证续租安全边界。
- **配置驱动**：连接字符串、数据库与时间参数从 `RedisSynchronization:*` 配置节读取。
- **可定制**：`RedisSynchronizationOptionsBuilder` 可调整过期、续租节奏与忙等待区间。

## 快速开始

配置节：

```json
{
  "RedisSynchronization": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "Expiry": "00:00:30",
    "ExtensionCadence": "00:00:10",
    "MinValidityTime": "00:00:05",
    "MinBusyWaitSleepTime": "00:00:00.010",
    "MaxBusyWaitSleepTime": "00:00:00.800"
  }
}
```

在模块中声明依赖：

```csharp
using Nerosoft.Euonia.Concurrency.Redis;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(RedisLockModule))]
public sealed class OrderModule : ModuleContextBase
{
}
```

使用锁：

```csharp
public sealed class OrderService(ILockFactory lockFactory)
{
    public async Task ShipAsync(long orderId, CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create($"order:{orderId}:ship");
        await using var handle = await locker.AcquireAsync(cancellationToken: cancellationToken);

        // 跨节点互斥
    }
}
```

> 同时注册 `RedisLockModule` 与 `RedisSemaphoreModule` 时，二者共享内部的 `RedisSynchronizationModule` 单例，不会重复建立连接。

## 配置项

| 配置键 | 默认值 | 说明 |
|--------|--------|------|
| `RedisSynchronization:ConnectionString` | 必填 | StackExchange.Redis 连接字符串 |
| `RedisSynchronization:Database` | `-1` | 数据库编号，`-1` 表示使用默认值 |
| `RedisSynchronization:Expiry` | `00:00:30` | 锁/信号量默认过期时间 |
| `RedisSynchronization:ExtensionCadence` | `MinValidityTime / 3` | 续租节奏 |
| `RedisSynchronization:MinValidityTime` | `90% × Expiry` | 续租前要求的最小剩余有效期 |
| `RedisSynchronization:MinBusyWaitSleepTime` | `10ms` | 忙等待最小间隔 |
| `RedisSynchronization:MaxBusyWaitSleepTime` | `800ms` | 忙等待最大间隔 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `RedisLockModule` | 注册 `ILockFactory` |
| `RedisSemaphoreModule` | 注册 `ISemaphoreFactory` |
| `RedisSynchronizationFactory` | 同时实现 `ILockFactory` 与 `ISemaphoreFactory` |
| `RedisSynchronizationOptionsBuilder` | `Expiry`、`ExtensionCadence`、`MinValidityTime`、`BusyWaitSleepTime` |
| `RedisLockProvider` / `RedisSemaphoreProvider` | Redis 上的锁与信号量实现 |

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
