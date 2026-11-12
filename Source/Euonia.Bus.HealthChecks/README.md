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

## 常见问题与故障排查

### 明明有死信，检查仍然返回 Healthy

**原因**：没有注册 `IDeadLetterStore`（例如未调用 `AddInMemoryDeadLetters()`），该项被跳过；或 `MaxDeadLetters` 设得过大。

**解决**：注册死信存储；把 `MaxDeadLetters` 设为可接受上限；通过响应 `data["deadLetters"]` 确认实际计数（`BusHealthCheck.cs:67-77`）。

### `transports` 一直是 0

**原因**：检查统计的是 `IConfigurator.StrategyAssignedTypes.Count`，即调用过 `SetStrategy` 的传输器名称，而不是 `ITransporter` 的注册数量。

**解决**：为实际使用的传输器调用 `SetStrategy`；或把 `RequireTransporter` 保持 `false`（`BusHealthCheck.cs:59-65`）。

### 检查报错“无法从根容器解析 scoped 服务”

**原因**：`IOutboxStore` / `IInboxStore` / `IDeadLetterStore` 可能注册为 Scoped，而健康检查运行在根容器上下文。

**解决**：`BusHealthCheck` 已在独立作用域中解析（`BusHealthCheck.cs:32-44`）；自定义存储若仍失败，检查其构造函数是否依赖请求级服务。

### 健康端点出现多条同名检查

**原因**：`AddEuoniaBusHealthChecks` 直接调用 `builder.Add`，重复调用不会去重。

**解决**：只在组合根注册一次；需要区分用途时用 `tags`，不要重复注册。

### 健康端点返回 404

**原因**：只调用了 `AddEuoniaBusHealthChecks`，没有调用 `app.MapHealthChecks(...)`；前者只注册检查，不创建路由。

**解决**：显式映射端点并确认路径与探针一致，例如 `app.MapHealthChecks("/health")`。

## 最佳实践

1. **为死信设置可执行阈值**：`MaxDeadLetters` 用业务可接受值（例如 0 或 10），并让告警直接指向 `{queue}.dlq`。
2. **按用途打标签**：liveness 只关心进程存活，readiness 才挂总线检查；用 `tags: ["bus", "ready"]` + `MapHealthChecks` 的 `Predicate` 分离。
3. **只有注册了存储才启用积压阈值**：`MaxOutboxFailed` / `MaxInboxFailed` 默认 `-1`，未注册存储时保持默认，避免误判。
4. **保持检查轻量**：不要在其中做远程调用或大数据扫描；设置 `timeout`（例如 3 秒）防止拖垮探针。
5. **阈值走环境变量**：`Euonia__Bus__HealthChecks__MaxDeadLetters` 或 `EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS`，按环境调整。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Microsoft.AspNetCore.App` 框架引用、`Euonia.Bus`。
- **版本策略**：随仓库 `Source/project.props` 统一管理，本文不固定版本号。
- **已知边界**：`transports` 是策略数量；未注册存储的项不参与判定；重复注册会产生多条同名检查。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎与可靠性存储
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 发件箱/收件箱/死信契约

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
