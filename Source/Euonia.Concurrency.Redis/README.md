# Euonia.Concurrency.Redis

Euonia 分布式同步的 Redis 提供程序：基于 `StackExchange.Redis` 实现分布式锁与信号量，支持自动续期、最短有效时间与忙等待退避。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Redis`
- **NuGet 包**：`Euonia.Concurrency.Redis`
- **依赖**：`Euonia.Concurrency`、`Euonia.Modularity`、`StackExchange.Redis`
- **配置键**：`RedisSynchronization:*`（模块内手写键，无常量类型）

## 安装

```bash
dotnet add package Euonia.Concurrency.Redis
```

## 快速开始

配置节（7 个键均可省略，`ConnectionString` 除外）：

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

环境变量（两种写法等价）：

```bash
export RedisSynchronization__ConnectionString=localhost:6379   # .NET 原生
export REDISSYNCHRONIZATION_CONNECTIONSTRING=localhost:6379    # 仓库 alias
```

声明模块依赖并获取锁：

```csharp
using Nerosoft.Euonia.Concurrency.Redis;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(RedisLockModule))]
public sealed class OrderModule : ModuleContextBase
{
}
```

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

需要并发限流时改为依赖 `RedisSemaphoreModule`：

```csharp
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.Redis;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(RedisSemaphoreModule))]
public sealed class ExportModule : ModuleContextBase
{
}

public sealed class ExportService(ISemaphoreFactory semaphoreFactory)
{
    public async Task ExportAsync(CancellationToken cancellationToken)
    {
        await using var handle = await semaphoreFactory.AcquireAsync(
            "report-export",
            maxCount: 4,
            timeout: TimeSpan.FromMinutes(1),
            cancellationToken: cancellationToken);
    }
}
```

## 主要功能

- **分布式锁**：`RedisLockModule` 注册 `ILockFactory`。
- **分布式信号量**：`RedisSemaphoreModule` 注册 `ISemaphoreFactory`。
- **租约续期**：按 `ExtensionCadence` 自动延长持有期，并以 `MinValidityTime` 保证获取完成后的最小剩余有效期。
- **配置驱动**：连接字符串、数据库与时间参数从 `RedisSynchronization:*` 读取。
- **可定制**：`RedisSynchronizationOptionsBuilder` 可调整过期、续期节奏与忙等待区间。
- **单例共享**：两个模块依赖同一个内部同步模块，连接与工厂只创建一次。

## 核心类型

| 类型 | 作用 |
|------|------|
| `RedisLockModule` | 注册 `ILockFactory` |
| `RedisSemaphoreModule` | 注册 `ISemaphoreFactory` |
| `RedisSynchronizationFactory` | 同时实现 `ILockFactory` 与 `ISemaphoreFactory` |
| `RedisSynchronizationOptionsBuilder` | `Expiry`、`ExtensionCadence`、`MinValidityTime`、`BusyWaitSleepTime` |
| `RedisLockProvider` / `RedisSemaphoreProvider` | Redis 上的锁与信号量实现 |

## 配置项

| 配置键 | 默认值 | 说明 |
|--------|--------|------|
| `RedisSynchronization:ConnectionString` | 必填 | StackExchange.Redis 连接字符串 |
| `RedisSynchronization:Database` | `-1` | 数据库编号，`-1` 表示使用服务端默认值 |
| `RedisSynchronization:Expiry` | `00:00:30` | 锁/信号量默认过期时间，最小 100ms |
| `RedisSynchronization:ExtensionCadence` | `MinValidityTime / 3` | 续租节奏，必须小于 `MinValidityTime` |
| `RedisSynchronization:MinValidityTime` | `90% × Expiry` | 获取完成后要求的最小剩余有效期，必须小于 `Expiry` |
| `RedisSynchronization:MinBusyWaitSleepTime` | `10ms` | 忙等待最小间隔 |
| `RedisSynchronization:MaxBusyWaitSleepTime` | `800ms` | 忙等待最大间隔，必须有限且不小于最小值 |

```json
{
  "RedisSynchronization": {
    "ConnectionString": "localhost:6379",
    "Database": 0
  }
}
```

```bash
RedisSynchronization__ConnectionString=localhost:6379   # .NET 原生
REDISSYNCHRONIZATION_CONNECTIONSTRING=localhost:6379    # 仓库 alias
```

## 注意事项 / 限制

- **`ConnectionString` 必填**：模块直接用连接字符串创建 `ConnectionMultiplexer`；为空或不可达时在首次解析工厂时失败。
- **配置只在首次解析时读取**：连接与选项由单例工厂在构造时读取一次，运行期修改 `IConfiguration` 不会生效，需重启宿主。
- **自动续期不能禁用**：`ExtensionCadence` 必须小于 `MinValidityTime`，传入无限或更大的值会抛 `ArgumentOutOfRangeException`。
- **两个模块共享单例**：同时注册 `RedisLockModule` 与 `RedisSemaphoreModule` 不会重复建连；只需要其中一个能力时不要注册另一个。
- **时间参数需满足顺序**：`MinValidityTime < Expiry`、`ExtensionCadence < MinValidityTime`，否则构建选项时立即失败。
- **键空间与部署**：锁键由锁名称直接构成；多个应用共享同一 Redis 数据库时，请使用唯一前缀避免名称碰撞。

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块、配置绑定与环境变量 alias
- [`Euonia.Concurrency.ZooKeeper`](../Euonia.Concurrency.ZooKeeper/README.md) —— ZooKeeper 实现
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 缓存与背板
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
