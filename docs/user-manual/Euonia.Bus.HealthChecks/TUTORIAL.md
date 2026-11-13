# Euonia.Bus.HealthChecks 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

Euonia 总线的 ASP.NET Core 健康检查：统计已分配传输策略的数量，并按阈值报告死信、发件箱失败与收件箱失败的积压。本页用运维视角演示就绪/存活探针与阈值验证；完整说明见 [项目 README](../../../Source/Euonia.Bus.HealthChecks/README.md)。

- **NuGet 包**：`Euonia.Bus.HealthChecks`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.HealthChecks`
- **完整选项**：[Source/Euonia.Bus.HealthChecks/README.md](../../../Source/Euonia.Bus.HealthChecks/README.md)

## 教程

### 场景：为总线宿主增加就绪探针并验证死信阈值

宿主启用总线与内存可靠性存储，暴露 `/health/live`（进程存活）与 `/health/ready`（总线就绪）。当死信数超过 `MaxDeadLetters` 时，就绪探针返回 `Unhealthy`。教程通过直接写入死信存储模拟积压。

### 前置条件

- ASP.NET Core 项目；
- 已引用 `Euonia.Bus` 与 `Euonia.Bus.InMemory`；
- 无外部依赖。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.InMemory
dotnet add package Euonia.Bus.HealthChecks
```

### 2. 接线：总线 + 存储 + 健康检查

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.HealthChecks;
using Nerosoft.Euonia.Bus.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddInMemoryBus("InMemory");
builder.Services.AddInMemoryOutbox();
builder.Services.AddInMemoryInbox();
builder.Services.AddInMemoryDeadLetters();
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention => convention.Add<DefaultMessageConvention>())
    .SetStrategy("InMemory", strategy =>
    {
        strategy.Add<LocalMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    }));

builder.Services.AddHealthChecks()
        .AddEuoniaBusHealthChecks(
            configure: options =>
            {
                options.MaxDeadLetters = 0;
                options.MaxOutboxFailed = 10;
                options.MaxInboxFailed = 10;
                options.RequireTransporter = true;
            },
            tags: ["bus", "ready"],
            timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false          // 只表示进程存活
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
```

### 3. 模拟死信积压

```csharp
using Nerosoft.Euonia.Bus;

app.MapPost("/debug/dead-letter", (IDeadLetterStore store) =>
{
    var envelope = new RoutedMessage<object>(new object(), "orders");
    store.Add(new DeadLetterEntry
    {
        MessageId = Guid.NewGuid().ToString("N"),
        Channel = "orders",
        MessageType = typeof(object).FullName,
        Content = envelope,
        Source = DeadLetterSource.Inbox,
        Target = "GetOrderStatusHandler",
        Error = "simulated failure"
    });

    return Results.Ok();
});
```

`MaxDeadLetters=0` 时，只要出现一条死信，就绪检查即失败。

### 4. 运行与验证

```bash
dotnet run

curl -i http://localhost:5200/health/live
# HTTP/1.1 200 OK

curl -i http://localhost:5200/health/ready
# HTTP/1.1 200 OK

curl -X POST http://localhost:5200/debug/dead-letter
curl -i http://localhost:5200/health/ready
# HTTP/1.1 503 Service Unavailable
# 响应体：Unhealthy，data 中 deadLetters=1
```

### 常见失败点

- **有死信仍 Healthy**：未注册 `IDeadLetterStore`，该项被跳过；或 `MaxDeadLetters` 设得过大。
- **`transports=0`**：检查统计的是 `SetStrategy` 的传输器名称数量，不是 `ITransporter` 注册数。
- **健康端点 404**：只调用了 `AddEuoniaBusHealthChecks`，没有 `MapHealthChecks(...)`。
- **重复条目**：多次调用 `AddEuoniaBusHealthChecks` 会注册多条同名检查。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Bus.HealthChecks/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/TUTORIAL.md)
- [Euonia.Bus.Abstract 用户手册](../Euonia.Bus.Abstract/TUTORIAL.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/TUTORIAL.md)
- [仓库根 README](../../../README.md)
