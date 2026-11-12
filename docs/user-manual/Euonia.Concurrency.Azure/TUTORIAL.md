# Euonia.Concurrency.Azure 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

用 Azure Blob 租约实现分布式锁：每个锁名对应一个 Blob，靠租约互斥并自动续约。本页用“月末对账任务只允许一个实例执行”演示容器注入、选项、模块接线与租约丢失处理。

- **NuGet 包**：`Euonia.Concurrency.Azure`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Azure`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Concurrency.Azure/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

对账任务可能被多个副本同时触发，但同一时刻只允许一个副本执行。锁使用 Azure Blob 租约，任务执行期间续约；若租约丢失，任务应尽快停止。容器由基础设施预先创建。

### 前置条件

- .NET 10 或 11 SDK。
- Azure Storage 账户与已创建的 Blob 容器（示例名 `locks`），或 Azurite。
- 安装 `Euonia.Concurrency.Azure` 与 `Euonia.Modularity`。

### 1. 安装

```bash
dotnet add package Euonia.Concurrency.Azure
dotnet add package Euonia.Modularity
```

### 2. 注入容器与选项，声明模块

```csharp
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Concurrency.Azure;
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);

var storageConnection = builder.Configuration.GetConnectionString("Storage")
    ?? throw new InvalidOperationException("ConnectionStrings:Storage is missing.");

builder.Services.AddSingleton(new BlobContainerClient(storageConnection, "locks"));
builder.Services.AddSingleton<Action<AzureSynchronizationOptionsBuilder>>(_ => options =>
{
    options.Duration(TimeSpan.FromSeconds(30))
           .RenewalCadence(TimeSpan.FromSeconds(10))
           .BusyWaitSleepTime(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1));
});

var application = builder.Services.AddModularityApplication<AppModule>(builder.Configuration);
builder.Services.AddSingleton<ReconciliationService>();

var app = builder.Build();
application.Initialize(app.Services);

[DependsOn(typeof(AzureLockModule))]
public sealed class AppModule : ModuleContextBase
{
}
```

### 3. 使用租约锁并监视丢失

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Concurrency;

public sealed class ReconciliationService(ILockFactory lockFactory, ILogger<ReconciliationService> logger)
{
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("reconcile");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);
        if (handle is null)
        {
            logger.LogInformation("Reconciliation is already running elsewhere.");
            return false;
        }

        using var leaseLost = handle.HandleCancellationToken.Register(
            () => logger.LogWarning("Azure blob lease was lost; stop writing results."));

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            return true;
        }
        finally
        {
            // await using 释放租约；由锁创建的 Blob 也会被清理。
        }
    }
}
```

### 4. 暴露触发端点

```csharp
app.MapPost("/reconcile", async (ReconciliationService service, CancellationToken ct)
    => await service.RunAsync(ct)
        ? Results.Accepted()
        : Results.Conflict(new { message = "Already running." }));
```

### 运行与验证

创建容器（已有可跳过）：

```bash
export AZURE_STORAGE_CONNECTION_STRING="UseDevelopmentStorage=true"
az storage container create --connection-string "$AZURE_STORAGE_CONNECTION_STRING" --name locks
```

启动并并发触发两次：

```bash
ConnectionStrings__Storage="$AZURE_STORAGE_CONNECTION_STRING" \
  dotnet run --urls http://localhost:5080

seq 1 2 | xargs -P 2 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST http://localhost:5080/reconcile
```

应只有一个 `202`，另一个 `409`。查看 Blob 租约状态：

```bash
az storage blob show --connection-string "$AZURE_STORAGE_CONNECTION_STRING" \
  --container-name locks --name reconcile --query "lease.status"
```

任务结束、句柄释放后租约消失，Blob 若由锁创建也会被删除。

### 常见失败点

- **`ContainerNotFound`**：本包不会创建容器；先在基础设施或启动阶段创建 `locks`。
- **`TryAcquire` 立刻返回 null**：另一个实例持有租约，这是预期互斥；需要等待时改用 `AcquireAsync` 并设置超时。
- **`ArgumentOutOfRangeException`**：`Duration` 必须是无限或 15–60 秒，且 `RenewalCadence < Duration`，忙等待最大值有限且不小于最小值。
- **模块注册后选项未生效**：`AzureLockModule` 只注册工厂；必须像上面那样注册 `Action<AzureSynchronizationOptionsBuilder>`，或手工构造 `AzureSynchronizationFactory`。

## 相关文档

- [API 参考](API.md)
- [项目 README：安装、选项与限制](../../../Source/Euonia.Concurrency.Azure/README.md)
- [Euonia.Concurrency 用户手册](../Euonia.Concurrency/TUTORIAL.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/TUTORIAL.md)
- [根 README](../../../README.md)
