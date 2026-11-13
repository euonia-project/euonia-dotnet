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

## 常见问题与故障排查

### 首次解析 ILockFactory 时连接失败或抛参数异常
原因：`RedisSynchronization:ConnectionString` 为空/不可达；模块直接用它创建 `ConnectionMultiplexer`，没有兜底默认值。
解决：设置 `RedisSynchronization:ConnectionString`（或 `REDISSYNCHRONIZATION_CONNECTIONSTRING`），先用 `redis-cli PING` 验证网络与认证。

### ArgumentOutOfRangeException 提示时间参数必须小于某个值
原因：选项要求 `MinValidityTime < Expiry` 且 `ExtensionCadence < MinValidityTime`；`ExtensionCadence` 不能设为无限（自动续期不可关闭）。
解决：按 `Expiry = 30s`、`MinValidityTime = 25s`、`ExtensionCadence = 5s` 之类的顺序配置，三者都必须是有限正数。

### 模块已加载，但容器里没有 ILockFactory 或 ISemaphoreFactory
原因：只依赖了内部同步模块，或加错了模块：`RedisLockModule` 只注册锁，`RedisSemaphoreModule` 只注册信号量。
解决：在模块上声明对应的 `[DependsOn(typeof(RedisLockModule))]` / `[DependsOn(typeof(RedisSemaphoreModule))]`；两个都需要时同时声明。

### 修改 RedisSynchronization 配置后没有生效
原因：连接与选项由单例工厂在首次解析时读取一次，之后不会重新绑定 `IConfiguration`。
解决：修改配置后重启宿主；需要热更新时改为手工构造 `RedisSynchronizationFactory` 并自行管理生命周期。

### 不同应用共用同一 Redis 数据库时锁互相干扰
原因：锁键由锁名称直接构成，没有自动应用名空间前缀。
解决：所有锁名统一加应用/环境前缀（如 `orders:prod:order:{id}:ship`），或为各应用分配独立 database。

## 最佳实践

- **七个键显式配置**：生产环境明确设置 `ConnectionString`、`Database`、`Expiry`、`MinValidityTime`、`ExtensionCadence` 与忙等待区间，不要依赖隐式默认。
- **锁粒度到资源**：锁名精确到业务实体并加环境前缀；避免 `global-lock` 之类的粗粒度锁。
- **临界区短于租约**：预期执行时间应显著小于 `Expiry`；长任务依赖续期时监控续期失败与 Redis 延迟。
- **信号量做容量控制**：批量任务用 `ISemaphoreFactory.Create(name, maxCount)` 限制并发，而不是让所有实例争抢同一把锁。
- **操作幂等**：锁过期/续期失败时可能短暂重叠执行，业务写操作应具备幂等键或唯一约束。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：`StackExchange.Redis`、`Euonia.Concurrency`、`Euonia.Modularity`。
- **已知边界**：配置键是模块内手写字符串（无常量类型）；`Database` 默认 `-1`；自动续期不可禁用；锁键无自动前缀。
- **升级注意**：锁状态在 Redis 侧，升级不迁移数据；若升级改变锁键格式或名称，新旧实例会锁定不同键，需停写窗口或双写过渡。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
