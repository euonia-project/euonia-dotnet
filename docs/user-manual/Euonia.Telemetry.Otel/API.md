# Euonia.Telemetry.Otel API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

把 Euonia 遥测源接入 OpenTelemetry：显式源目录、Resource 快照、日志 Scope 与传播器桥接，导出交给宿主选定的 exporter。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Telemetry.Otel` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Telemetry.Otel` |
| 依赖 | `Euonia.Telemetry` + OpenTelemetry SDK / Hosting / OTLP 导出器 |
| 完整参考 | [`Source/Euonia.Telemetry.Otel/README.md`](../../../Source/Euonia.Telemetry.Otel/README.md) |

## API 参考

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `AddEuoniaOpenTelemetry(IServiceCollection, Action<TelemetryOptions>?, Action<OpenTelemetryBuilder>?, IConfiguration?, bool)` | 一次注册内核 + OTel 管道 | `configurePropagator=false`；重复调用幂等 |
| `AddEuoniaSources(TracerProviderBuilder, TelemetrySourceRegistry? = null, Func<string,bool>? = null)` | 订阅默认源目录 + 注册表 | `filter` 默认全通过 |
| `AddEuoniaSources(MeterProviderBuilder, TelemetrySourceRegistry? = null, Func<string,bool>? = null)` | 订阅指标的源 | 用 `MeterName`，可与源名不同 |
| `AddEuoniaOtlpExporter(TracerProviderBuilder)` | `AddOtlpExporter()` 的包装 | 仅 tracer 侧 |
| `UseEuoniaPropagator()` | 设置 TraceContext + Baggage | 进程级全局，最后一次调用生效 |
| `EuoniaSources.Default` | 16 个默认源名快照 | 含 `Nerosoft.Euonia.Bus`、`Euonia.Osba` 等 |
| `EuoniaSources.Add(string sourceName)` | 声明自定义源 | 必须在构建 Provider 之前调用 |
| Resource 属性 | `service.name`、`service.version`、`deployment.environment`、`euonia.version` | 前三个取自 `TelemetryOptions` |
| `OpenTelemetryLoggerOptions` | 包内已设 `IncludeScopes=true`、`IncludeFormattedMessage=true` | 日志 Scope 可与 Trace 对齐 |

### 配置项

只读 `Euonia:Telemetry` 节，且**注册期读一次**：

| 键 | 说明 | 默认值 |
|---|---|---|
| `Enabled` | 是否构建 Tracing / Metrics 管道 | `false` |
| `TracesEnabled` | 是否 `WithTracing` | `true` |
| `MetricsEnabled` | 是否 `WithMetrics` | `true` |
| `ServiceName` / `ServiceVersion` / `Environment` | Resource 属性 | `null` |

```bash
# .NET 原生：双下划线表示层级
Euonia__Telemetry__Enabled=true
Euonia__Telemetry__ServiceName=orders-api
Euonia__Telemetry__Environment=Production

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_TELEMETRY_ENABLED=true
EUONIA_TELEMETRY_SERVICENAME=orders-api
EUONIA_TELEMETRY_ENVIRONMENT=Production
```

OTLP 端点等参数属于 OTel 自己的配置，用 `OTEL_EXPORTER_OTLP_ENDPOINT` / `OTEL_SERVICE_NAME` 等标准变量，不写进 `Euonia:Telemetry`。

## 相关文档

- [教程](TUTORIAL.md)
- [`Euonia.Telemetry.Otel` README](../../../Source/Euonia.Telemetry.Otel/README.md) —— 完整说明与限制
- [Telemetry 手册](../Euonia.Telemetry/API.md) —— 埋点内核、脱敏与日志 Scope
- [Tenancy 手册](../Euonia.Tenancy/API.md) —— 租户标签来源
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` 与管道顺序
- [根 README](../../../README.md)
