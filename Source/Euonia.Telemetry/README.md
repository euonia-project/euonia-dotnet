# Euonia.Telemetry

Euonia 遥测能力内核：Activity/Meter 源、标签脱敏、上下文、W3C 传播与日志 Scope。**不依赖 OpenTelemetry SDK。**

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Telemetry`
- **NuGet 包**：`Euonia.Telemetry`

## 快速开始

```csharp
services.AddEuoniaTelemetry(options =>
{
    options.Enabled = true;
    options.ServiceName = "order-service";
    options.RecordTenantId = true;
});
```

组件通过 `ITelemetrySourceFactory.Create("<源名>")` 获取 `TelemetrySource`：

```csharp
var source = factory.Create("Nerosoft.Euonia.Repository");
using var activity = source.StartActivity(TelemetryNames.RepositoryExecute);
activity.SetTag("db.system", "postgresql");
source.Counter("euonia.repository.duration", "s").Add(elapsedSeconds);
```

## 语义

- 无监听者 / 关闭时所有方法空操作；指标写入前检查 `IsMetricEnabled`。
- 标签写入前经 `ITelemetryRedactor`；Activity 创建后执行 `ITelemetryEnricher`。
- **租户与用户标识只进 Trace / 日志，绝不进入 Metrics**。
- 不自动记录耗时直方图；耗时由组件显式记录。
- 传播基于 `DistributedContextPropagator.Current`，非法 `traceparent` 按新根处理。
- 遥测异常一律吞掉，不改变业务控制流。

## 导出

本包不导出任何数据；接 OTel 请使用 `Euonia.Telemetry.Otel`。
