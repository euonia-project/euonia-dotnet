# Euonia.Caching.Runtime 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

用 `System.Runtime.Caching.MemoryCache` 作为 Euonia 缓存提供程序，适合已有该配置模型（内存上限、物理内存百分比、轮询间隔）的宿主。本页用报表缓存演示命名实例、限额与失效。

- **NuGet 包**：`Euonia.Caching.Runtime`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Runtime`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching.Runtime/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

报表服务生成开销较大，结果在 5 分钟内可复用；同时希望限制缓存最多占用 128 MB。服务使用独立的命名实例 `reports`，不与进程内其他 `MemoryCache` 共享键空间。

### 前置条件

- .NET 10 或 11 SDK。
- 无需外部服务；缓存随进程生命周期存在。
- 安装 `Euonia.Caching.Runtime`。

### 1. 安装与注册

```bash
dotnet add package Euonia.Caching.Runtime
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RuntimeCacheOptions>(options =>
{
    options.InstanceName = "reports";
    options.KeyPrefix = "reports";
    options.CacheMemoryLimitMegabytes = 128;
    options.PhysicalMemoryLimitPercentage = 10;
    options.PollingInterval = TimeSpan.FromSeconds(30);
    options.Expires = TimeSpan.FromMinutes(10);
    options.MaxRetries = 3;
});

builder.Services.AddSingleton<ICacheService, RuntimeCacheService>();
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();
```

### 2. 读穿与失效

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Caching;

public sealed record Report(string Id, DateTimeOffset GeneratedAt);

public sealed class ReportService(ICacheService cache, ILogger<ReportService> logger)
{
    public Report Get(string id)
        => cache.GetOrAdd(
            $"report:{id}",
            () =>
            {
                logger.LogInformation("Generating report {Id}.", id);
                return new Report(id, DateTimeOffset.UtcNow);
            },
            TimeSpan.FromMinutes(5));

    public bool Invalidate(string id) => cache.Remove<Report>($"report:{id}");
}
```

### 3. 暴露端点

```csharp
app.MapGet("/reports/{id}", (string id, ReportService service)
    => Results.Ok(service.Get(id)));

app.MapDelete("/reports/{id}", (string id, ReportService service)
    => service.Invalidate(id) ? Results.NoContent() : Results.NotFound());
```

### 运行与验证

```bash
dotnet run --urls http://localhost:5080

curl -s http://localhost:5080/reports/daily
curl -s http://localhost:5080/reports/daily
```

第一次请求打印一次 `Generating report daily`，第二次不再打印。删除后再次读取会重新生成：

```bash
curl -X DELETE http://localhost:5080/reports/daily
curl -s http://localhost:5080/reports/daily
```

内存限额是否生效取决于 `InstanceName` 是否为非默认值；上述配置传给了新建的命名 `MemoryCache`。

### 常见失败点

- **限额完全不生效**：`InstanceName = "default"` 时使用全局 `MemoryCache.Default`，代码选项不会传入；改用唯一实例名。
- **按前缀删除返回 0**：运行时句柄未实现前缀删除；维护自己的键集合或改用 Memory/Redis 提供程序。
- **多租户键没有前缀**：`RuntimeCacheService` 不注入 `IKeyTransformer`；需调用方自行拼前缀或改用 Memory/Redis 服务。
- **滑动过期不精确**：底层过期扫描粒度较粗，不要依赖秒级精度。

## 相关文档

- [API 参考](API.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Caching.Runtime/README.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/TUTORIAL.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/TUTORIAL.md)
- [根 README](../../../README.md)
