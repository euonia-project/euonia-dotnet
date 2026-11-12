# Euonia.Concurrency.Redis 用户手册

用 Redis 实现跨节点分布式锁与信号量，支持自动续期与最短有效时间。本页用“订单发货互斥 + 报表导出限流”演示配置、模块、并发验证与故障定位。

- **NuGet 包**：`Euonia.Concurrency.Redis`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Redis`
- **完整安装与选项**：[项目 README](../../Source/Euonia.Concurrency.Redis/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

订单服务多副本部署：同一订单的发货操作必须串行，避免重复出库；报表导出最多允许 3 个并发。两者都通过 Redis 协调，服务实例之间不共享内存。

### 前置条件

- .NET 10 或 11 SDK。
- 可连接的 Redis。
- 安装 `Euonia.Concurrency.Redis` 与 `Euonia.Modularity`。

### 1. 安装

```bash
dotnet add package Euonia.Concurrency.Redis
dotnet add package Euonia.Modularity
```

### 2. 配置 7 个键

`appsettings.json`：

```json
{
  "RedisSynchronization": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "Expiry": "00:00:30",
    "MinValidityTime": "00:00:25",
    "ExtensionCadence": "00:00:05",
    "MinBusyWaitSleepTime": "00:00:00.010",
    "MaxBusyWaitSleepTime": "00:00:00.800"
  }
}
```

两种等价的环境变量写法（其余键同理）：

```bash
export RedisSynchronization__ConnectionString=localhost:6379   # .NET 原生
export REDISSYNCHRONIZATION_CONNECTIONSTRING=localhost:6379    # 仓库 alias
```

### 3. 模块接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency.Redis;
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);
var application = builder.Services.AddModularityApplication<AppModule>(builder.Configuration);
builder.Services.AddSingleton<ShippingService>();

var app = builder.Build();
application.Initialize(app.Services);

[DependsOn(typeof(RedisLockModule), typeof(RedisSemaphoreModule))]
public sealed class AppModule : ModuleContextBase
{
}
```

### 4. 锁与信号量

```csharp
using Nerosoft.Euonia.Concurrency;

public sealed class ShippingService(ILockFactory lockFactory, ISemaphoreFactory semaphoreFactory)
{
    public async Task<bool> ShipAsync(long orderId, CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create($"orders:{orderId}:ship");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.FromSeconds(2), cancellationToken);
        if (handle is null)
        {
            return false;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        return true;
    }

    public async Task<bool> ExportAsync(string report, CancellationToken cancellationToken)
    {
        await using var handle = await semaphoreFactory.TryAcquireAsync(
            $"exports:{report}",
            maxCount: 3,
            timeout: TimeSpan.FromSeconds(1),
            cancellationToken: cancellationToken);

        if (handle is null)
        {
            return false;
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        return true;
    }
}
```

### 5. 暴露端点

```csharp
app.MapPost("/orders/{id:long}/ship", async (long id, ShippingService service, CancellationToken ct)
    => await service.ShipAsync(id, ct) ? Results.Accepted() : Results.Conflict());

app.MapGet("/reports/{name}/export", async (string name, ShippingService service, CancellationToken ct)
    => await service.ExportAsync(name, ct) ? Results.Ok() : Results.StatusCode(429));
```

### 运行与验证

```bash
dotnet run --urls http://localhost:5080
```

同一订单并发发货两次，应一个 `202`、一个 `409`：

```bash
seq 1 2 | xargs -P 2 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST http://localhost:5080/orders/1001/ship
```

并发导出 6 次，最多 3 个 `200`，其余 `429`：

```bash
seq 1 6 | xargs -P 6 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5080/reports/daily/export
```

查看 Redis 中的锁/信号量键：

```bash
redis-cli --scan --pattern '*orders:1001:ship*'
redis-cli --scan --pattern '*exports:daily*'
```

### 常见失败点

- **首次解析工厂时报连接失败**：`RedisSynchronization:ConnectionString` 为空或 Redis 不可达；先 `redis-cli PING`。
- **`ArgumentOutOfRangeException`**：必须满足 `MinValidityTime < Expiry` 且 `ExtensionCadence < MinValidityTime`；自动续期不能禁用。
- **改配置后不生效**：单例工厂只在首次解析时读取配置，修改后需重启宿主。
- **同名锁在不同应用互相干扰**：锁键来自锁名称本身，没有自动命名空间；给锁名加应用/环境前缀。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `RedisLockModule` | 注册 `ILockFactory` | 依赖内部同步模块 |
| `RedisSemaphoreModule` | 注册 `ISemaphoreFactory` | 与锁模块共享单例 |
| `RedisSynchronizationFactory(IDatabase, Action<...>?)` | 同时实现锁/信号量工厂 | database 必填 |
| `RedisSynchronizationFactory(IEnumerable<IDatabase>, Action<...>?)` | 多库构造 | 信号量只用第一库 |
| `RedisSynchronizationFactory.Create(RedisKey)` | 创建锁 | 键不能为默认值 |
| `RedisSynchronizationFactory.Create(RedisKey, int maxCount)` | 创建信号量 | `maxCount >= 1` |
| `RedisLockProvider(RedisKey, IDatabase, Action<...>?)` | 单库锁 | 键与库必填 |
| `RedisLockProvider(RedisKey, IEnumerable<IDatabase>, Action<...>?)` | 多库锁 | 列表不能为空/含 null |
| `RedisSemaphoreProvider(RedisKey, int, IDatabase, Action<...>?)` | 信号量 | `maxCount` 必填 |
| `RedisSynchronizationOptionsBuilder.Expiry(TimeSpan)` | 锁过期时间 | 默认 30s，最小 100ms |
| `RedisSynchronizationOptionsBuilder.MinValidityTime(TimeSpan)` | 获取后最小有效时间 | 默认 90% × Expiry |
| `RedisSynchronizationOptionsBuilder.ExtensionCadence(TimeSpan)` | 续期节奏 | 默认 `MinValidity / 3`，必须更小 |
| `RedisSynchronizationOptionsBuilder.BusyWaitSleepTime(min, max)` | 忙等待区间 | 默认 `[10ms, 800ms]` |
| `RedisSynchronizationHandle` | 锁/票据句柄 | `HandleCancellationToken` 监视丢失 |
| `RedisLockProvider.Acquire/TryAcquire` | 同步获取 | 失败返回 `null`/超时抛异常 |
| `RedisSemaphoreProvider.MaxCount` | 最大并发票据 | 构造时指定 |
| `ISemaphoreFactory.Create(name, maxCount)` | 按名称创建 | 名称必填 |

### 配置项

| 配置键 | 默认值 | 说明 |
|---|---|---|
| `RedisSynchronization:ConnectionString` | 必填 | StackExchange.Redis 连接串 |
| `RedisSynchronization:Database` | `-1` | 逻辑库；`-1` 使用服务端默认 |
| `RedisSynchronization:Expiry` | `00:00:30` | 锁/票据过期时间，最小 100ms |
| `RedisSynchronization:MinValidityTime` | `90% × Expiry` | 必须小于 `Expiry` |
| `RedisSynchronization:ExtensionCadence` | `MinValidity / 3` | 必须小于 `MinValidityTime` |
| `RedisSynchronization:MinBusyWaitSleepTime` | `10ms` | 忙等待最小间隔 |
| `RedisSynchronization:MaxBusyWaitSleepTime` | `800ms` | 忙等待最大间隔 |

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

## 相关文档

- [项目 README：安装、完整选项与限制](../../Source/Euonia.Concurrency.Redis/README.md)
- [Euonia.Concurrency 用户手册](./Euonia.Concurrency.md)
- [Euonia.Concurrency.Azure 用户手册](./Euonia.Concurrency.Azure.md)
- [Euonia.Caching.Redis 用户手册](./Euonia.Caching.Redis.md)
- [根 README](../../README.md)
