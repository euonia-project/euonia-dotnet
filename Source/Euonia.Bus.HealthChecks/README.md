# Euonia.Bus.HealthChecks

Euonia 总线的 ASP.NET Core 健康检查：报告已注册的传输器数量，以及发件箱、收件箱与死信的积压情况，可直接挂到健康检查端点。

Health checks for Euonia Bus: report configured transporters and outbox, inbox and dead-letter backlog, ready to plug into ASP.NET Core health endpoints.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.HealthChecks`
- **NuGet 包**：`Euonia.Bus.HealthChecks`

## 安装

```bash
dotnet add package Euonia.Bus.HealthChecks
```

## 主要功能

- **传输器检查**：可选要求至少注册一个传输器（`RequireTransporter`）。
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
| `RequireTransporter` | `false` | 未注册传输器时是否判定不健康 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `BusHealthCheck` | `IHealthCheck` 实现 |
| `BusHealthCheckOptions` | 阈值配置 |
| `AddEuoniaBusHealthChecks(...)` | 在 `IHealthChecksBuilder` 上注册检查 |

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎与可靠性存储
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 发件箱/收件箱/死信契约

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
