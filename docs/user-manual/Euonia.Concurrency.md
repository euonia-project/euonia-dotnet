# Euonia.Concurrency 用户手册

Euonia 的分布式同步抽象：用 `ILockFactory` 获取互斥锁、`ISemaphoreFactory` 限制并发数，业务代码只依赖接口，具体实现由文件系统/Redis/Azure/ZooKeeper 提供程序包决定。本页用“库存扣减 + 报表导出限流”演示。

- **NuGet 包**：`Euonia.Concurrency`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency`
- **完整安装与选项**：[项目 README](../../Source/Euonia.Concurrency/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

库存服务接收并发扣减请求，必须保证不超卖；报表服务最多允许 2 个并发导出。示例用文件系统锁保护库存（本机多进程即可验证），用 Redis 信号量限制导出（跨节点）。

### 前置条件

- .NET 10 或 11 SDK。
- 本机可写目录（示例 `/tmp/euonia-locks`）。
- 可连接的 Redis。
- 安装 `Euonia.Concurrency`、`Euonia.Concurrency.FileSystem`、`Euonia.Concurrency.Redis`、`Euonia.Modularity`。

### 1. 安装

```bash
dotnet add package Euonia.Concurrency
dotnet add package Euonia.Concurrency.FileSystem
dotnet add package Euonia.Concurrency.Redis
dotnet add package Euonia.Modularity
```

### 2. 模块接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency.FileSystem;
using Nerosoft.Euonia.Concurrency.Redis;
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);
var application = builder.Services.AddModularityApplication<AppModule>(builder.Configuration);

builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<ExportService>();

var app = builder.Build();
application.Initialize(app.Services);

[DependsOn(typeof(FileSystemLockModule), typeof(RedisSemaphoreModule))]
public sealed class AppModule : ModuleContextBase
{
}
```

### 3. 库存锁与导出信号量

```csharp
using System.Collections.Concurrent;
using Nerosoft.Euonia.Concurrency;

public sealed class InventoryService(ILockFactory lockFactory)
{
    private readonly ConcurrentDictionary<string, int> _stock = new() { ["sku-1"] = 10 };

    public async Task<bool> ReserveAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create($"inventory:{sku}");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.FromSeconds(2), cancellationToken);
        if (handle is null)
        {
            return false; // 2 秒内未拿到锁
        }

        if (!_stock.TryGetValue(sku, out var available) || available < quantity)
        {
            return false;
        }

        _stock[sku] = available - quantity;
        return true;
    }
}

public sealed class ExportService(ISemaphoreFactory semaphoreFactory)
{
    public async Task<string> ExportAsync(CancellationToken cancellationToken)
    {
        await using var handle = await semaphoreFactory.TryAcquireAsync(
            "report-export",
            maxCount: 2,
            timeout: TimeSpan.FromSeconds(1),
            cancellationToken: cancellationToken);

        if (handle is null)
        {
            return "busy";
        }

        await Task.Delay(500, cancellationToken);
        return "done";
    }
}
```

### 4. 暴露端点

```csharp
app.MapPost("/stock/{sku}/reserve", async (string sku, int quantity, InventoryService service, CancellationToken ct)
    => await service.ReserveAsync(sku, quantity, ct) ? Results.Ok() : Results.Conflict());

app.MapGet("/reports/export", async (ExportService service, CancellationToken ct) =>
{
    var result = await service.ExportAsync(ct);
    return result == "busy" ? Results.StatusCode(429) : Results.Ok(new { result });
});
```

### 运行与验证

```bash
export FileSynchronization__Directory=/tmp/euonia-locks
export RedisSynchronization__ConnectionString=localhost:6379

dotnet run --urls http://localhost:5080
```

并发扣减 20 次、每次 1 件，最终应恰好 10 次成功，库存不会变成负数：

```bash
seq 1 20 | xargs -P 20 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST "http://localhost:5080/stock/sku-1/reserve?quantity=1" | sort | uniq -c
```

并发导出 6 次，最多 2 个返回 200，其余返回 429（1 秒内拿不到信号量）。用 Redis 查看信号量键：

```bash
redis-cli --scan --pattern '*report-export*'
```

### 常见失败点

- **`Unable to resolve service for type ILockFactory`**：没有引用提供程序包或模块未进入 `[DependsOn]` 链；`Euonia.Concurrency` 本身不注册任何服务。
- **文件锁报目录未配置**：设置 `FileSynchronization:Directory` 或 `FileSynchronization__Directory`。
- **Redis 连接失败**：设置 `RedisSynchronization:ConnectionString`；模块在首次解析时读取一次。
- **`ISemaphoreFactory` 解析失败**：文件系统与 Azure 包只有锁，信号量需要 Redis 或 ZooKeeper。

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

- [项目 README：接口、扩展方法与限制](../../Source/Euonia.Concurrency/README.md)
- [Euonia.Concurrency.FileSystem 用户手册](./Euonia.Concurrency.FileSystem.md)
- [Euonia.Concurrency.Redis 用户手册](./Euonia.Concurrency.Redis.md)
- [Euonia.Concurrency.Azure 用户手册](./Euonia.Concurrency.Azure.md)
- [根 README](../../README.md)
