# Euonia.Bus.HealthChecks

Euonia 总线的 ASP.NET Core 健康检查：报告已分配传输策略的数量，以及发件箱、收件箱与死信的积压情况，可直接挂到健康检查端点。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.HealthChecks`
- **NuGet 包**：`Euonia.Bus.HealthChecks`

## 安装

```bash
dotnet add package Euonia.Bus.HealthChecks
```

## 主要功能

- **传输器检查**：统计已分配传输策略的名称数量，可选要求至少有一个（`RequireTransporter`）。
- **积压阈值**：死信、发件箱失败、收件箱失败分别按阈值判定 `Unhealthy`。
- **按需参与**：未注册对应存储时该项自动跳过，不会误报。
- **无副作用**：检查过程不会实例化 `IBus`，不会触发发件箱轮询等后台工作。

## 快速开始

```csharp
services.AddHealthChecks()
        .AddEuoniaBusHealthChecks(options =>
        {
            options.MaxDeadLetters = 10;
            options.MaxOutboxFailed = 100;
            options.MaxInboxFailed = 100;
            options.RequireTransporter = true;
        });

var app = builder.Build();
app.MapHealthChecks("/health");
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `MaxDeadLetters` | `0` | 允许的死信上限；出现死信即不健康 |
| `MaxOutboxFailed` | `-1` | 发件箱失败上限；`-1` 表示不检查 |
| `MaxInboxFailed` | `-1` | 收件箱失败上限；`-1` 表示不检查 |
| `RequireTransporter` | `false` | 未配置任何传输策略时是否判定不健康 |

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

## 核心类型

| 类型 | 作用 |
|------|------|
| `BusHealthCheck` | `IHealthCheck` 实现 |
| `BusHealthCheckOptions` | 阈值配置 |
| `AddEuoniaBusHealthChecks(...)` | 在 `IHealthChecksBuilder` 上注册检查 |

## 使用示例

把总线检查挂到独立的健康端点，并带上标签与超时：

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nerosoft.Euonia.Bus.HealthChecks;

services.AddHealthChecks()
        .AddEuoniaBusHealthChecks(
            configure: options =>
            {
                options.MaxDeadLetters = 10;
                options.MaxOutboxFailed = 100;
                options.MaxInboxFailed = 100;
                options.RequireTransporter = true;
            },
            failureStatus: HealthStatus.Degraded,
            tags: ["bus", "ready"],
            timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
```

响应中的 `data` 字典包含实际计数，便于排障：

```csharp
app.MapGet("/health/bus", async (HealthCheckService health, CancellationToken cancellationToken) =>
{
    var report = await health.CheckHealthAsync(
        registration => registration.Name == HealthChecksBuilderExtensions.Name,
        cancellationToken);

    return Results.Ok(new
    {
        report.Status,
        report.TotalDuration,
        Entries = report.Entries.ToDictionary(
            pair => pair.Key,
            pair => new { pair.Value.Status, pair.Value.Description, pair.Value.Data })
    });
});
```

## 注意事项 / 限制

1. **`transports` 是策略数量而非真实注册数**：`BusHealthCheck` 读取 `IConfigurator.StrategyAssignedTypes.Count`（`Source/Euonia.Bus.HealthChecks/BusHealthCheck.cs:59-60`），未调用 `SetStrategy` 的传输器不会计入。
2. **阈值判断是严格大于**：只有计数 `> MaxDeadLetters` / `> MaxOutboxFailed` / `> MaxInboxFailed` 时才不健康；等于阈值仍为健康（`BusHealthCheck.cs:73-98`）。
3. **未注册的存储会被跳过**：没有 `IDeadLetterStore` / `IOutboxStore` / `IInboxStore` 时对应项既不参与判定也不写入 `data`，不会误报。
4. **每次检查都会创建 DI 作用域**：存储可能注册为 Scoped，健康检查在独立作用域中解析（`BusHealthCheck.cs:32-44`）；实现需支持在根容器外解析。
5. **不会实例化 `IBus`**：检查用 `IConfigurator` 判断总线是否装配，避免触发发件箱轮询等后台工作（`BusHealthCheck.cs:51-57`）。
6. **重复注册会产生多条同名检查**：`AddEuoniaBusHealthChecks` 直接调用 `builder.Add`，多次调用不会去重。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎与可靠性存储
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 发件箱/收件箱/死信契约

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
