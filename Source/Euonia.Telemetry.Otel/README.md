# Euonia.Telemetry.Otel

Euonia 遥测的 OpenTelemetry 适配层：显式源目录、Resource、OTLP 导出与传播器桥接。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Telemetry.Otel`
- **NuGet 包**：`Euonia.Telemetry.Otel`
- **依赖**：`Euonia.Telemetry` + OpenTelemetry SDK / Hosting / OTLP

## 快速开始

```csharp
services.AddEuoniaTelemetry(options => options.ServiceName = "order-service");

services.AddEuoniaOpenTelemetry(
    configureTelemetry: options =>
    {
        options.Enabled = true;
        options.Environment = "Production";
    },
    configure: otel =>
    {
        otel.AddOtlpExporter();
        // 宿主按需添加 instrumentation：
        // otel.WithTracing(t => t.AddAspNetCoreInstrumentation());
    });
```

## 说明

- `AddEuoniaSources()` 使用**显式源目录** + 可选 `TelemetrySourceRegistry`，不依赖组件注册顺序。
- 适配层只强依赖 OTel SDK / Hosting / OTLP；不替宿主引入 `OpenTelemetry.Instrumentation.*`。
- Resource 属性：`service.name`、`service.version`、`deployment.environment`、`euonia.version`。
- `UseEuoniaPropagator()` 会覆盖全局默认传播器（TraceContext + Baggage）；宿主已自行配置时不要调用。
