# Euonia.Bus.HealthChecks 用户手册

Euonia 总线的 ASP.NET Core 健康检查：统计已分配传输策略的数量，并按阈值报告死信、发件箱失败与收件箱失败的积压。本页用运维视角演示就绪/存活探针与阈值验证；完整说明见 [项目 README](../../Source/Euonia.Bus.HealthChecks/README.md)。

- **NuGet 包**：`Euonia.Bus.HealthChecks`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.HealthChecks`
- **完整选项**：[Source/Euonia.Bus.HealthChecks/README.md](../../Source/Euonia.Bus.HealthChecks/README.md)

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

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddEuoniaBusHealthChecks(...)` | 在 `IHealthChecksBuilder` 上注册总线检查 | `configure`、`failureStatus`、`tags`、`timeout` |
| `HealthChecksBuilderExtensions.Name` | 注册名称常量 | `euonia-bus` |
| `BusHealthCheck` | `IHealthCheck` 实现 | 依赖 `IConfigurator` 与可选存储 |
| `BusHealthCheckOptions.MaxDeadLetters` | 死信上限 | 默认 `0` |
| `BusHealthCheckOptions.MaxOutboxFailed` | 发件箱失败上限 | 默认 `-1`（不检查） |
| `BusHealthCheckOptions.MaxInboxFailed` | 收件箱失败上限 | 默认 `-1`（不检查） |
| `BusHealthCheckOptions.RequireTransporter` | 无策略时是否不健康 | 默认 `false` |
| `IDeadLetterStore` | 死信存储契约 | `AddInMemoryDeadLetters()` 注册 |
| `IOutboxStore` / `IInboxStore` | 发件箱/收件箱存储契约 | 未注册则对应项跳过 |
| `IConfigurator.StrategyAssignedTypes` | 检查使用的传输器计数来源 | 已调用 `SetStrategy` 的名称 |

### 配置项

配置节 `Euonia:Bus:HealthChecks`（`Source/Euonia.Bus.HealthChecks/BusHealthCheckOptions.cs:12`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `MaxDeadLetters` | `int` | `0` |
| `MaxOutboxFailed` | `int` | `-1` |
| `MaxInboxFailed` | `int` | `-1` |
| `RequireTransporter` | `bool` | `false` |

```json
{
  "Euonia": {
    "Bus": {
      "HealthChecks": {
        "MaxDeadLetters": 10,
        "MaxOutboxFailed": 100,
        "MaxInboxFailed": 100,
        "RequireTransporter": true
      }
    }
  }
}
```

```bash
Euonia__Bus__HealthChecks__MaxDeadLetters=10    # .NET 原生：双下划线表示层级
EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS=10        # 仓库 alias：大写单下划线
Euonia__Bus__HealthChecks__RequireTransporter=true
EUONIA_BUS_HEALTHCHECKS_REQUIRETRANSPORTER=true
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus.HealthChecks/README.md)
- [Euonia.Bus 用户手册](Euonia.Bus.md)
- [Euonia.Bus.Abstract 用户手册](Euonia.Bus.Abstract.md)
- [Euonia.Bus.InMemory 用户手册](Euonia.Bus.InMemory.md)
- [仓库根 README](../../README.md)
