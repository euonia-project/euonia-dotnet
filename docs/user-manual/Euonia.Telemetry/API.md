# Euonia.Telemetry API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

遥测内核：用统一的 `TelemetrySource` 同时产出 `Activity` 与指标，并把租户、关联标识写进 Trace 而不污染指标。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Telemetry` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Telemetry` |
| 依赖 | `Euonia.Core`（`AmbientExecutionContext`）与 `Microsoft.Extensions.*` 抽象包 |
| 完整参考 | [`Source/Euonia.Telemetry/README.md`](../../../Source/Euonia.Telemetry/README.md) |

## API 参考

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `TelemetryOptions` | 全局选项，配置节 `Euonia:Telemetry` | `Enabled=false`、`TracesEnabled=true`、`MetricsEnabled=true` |
| `AddEuoniaTelemetry(IServiceCollection, Action<TelemetryOptions>?)` | 注册内核服务 | `configure` 可空；不注册导出器 |
| `ITelemetrySourceFactory.Create(string sourceName, string? meterName = null)` | 取或建组件源 | 同名幂等；`meterName` 默认等于 `sourceName` |
| `ITelemetrySourceFactory.Sources` | 已创建源快照 | `IReadOnlyCollection<TelemetrySource>` |
| `TelemetrySource.StartActivity(string name, ActivityKind kind = Internal, ActivityContext? parent = null, IEnumerable<ActivityLink>? links = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)` | 启动活动 | 无监听者时返回空句柄 |
| `TelemetrySource.Counter(string name, string? unit = null, string? description = null)` | 取或建计数器 | 指标关闭时返回回退实例 |
| `TelemetrySource.Histogram(string name, string? unit = null, string? description = null)` | 取或建直方图 | 同上；同名元数据不一致抛异常 |
| `TelemetrySource.IsTraceEnabled` / `IsMetricEnabled` | 可用性判断 | 前者含 `HasListeners()` |
| `TelemetryActivity.SetTag(string key, object? value)` | 写标签（经脱敏） | `value` 为 `null` 时忽略 |
| `TelemetryActivity.SetOutcome(bool success, string? errorType = null)` | 写结果与状态 | 失败时置 `ActivityStatusCode.Error` |
| `TelemetryActivity.RecordException(Exception)` | 记录异常事件 | 写 `exception.type`、`exception.stacktrace` |
| `TelemetryLogScope.Begin(ILogger, ITelemetryContextProvider?)` | 日志 Scope | 无上下文时返回空句柄 |
| `ITelemetryEnricher.Enrich(Activity, TelemetryContext?)` | 自定义富化 | 内置 `TelemetryContextEnricher` 写租户标签 |
| `ITelemetryRedactor.Redact(string key, object? value)` | 自定义脱敏 | 内置实现命中键返回 `***` |
| `ITelemetryPropagator.Inject/Extract` | W3C 传播 | 载体为 `IDictionary<string, object>` |
| `TelemetryNames` | 活动名/标签键/指标名常量 | 如 `RepositoryExecute`、`TenantId`、`Outcome` |
| `TelemetryContext` | 只读上下文记录 | 8 个字段，含 `IsCrossTenant` |
| `TelemetryRedactionOptions` | 脱敏子选项 | `Enabled=true`，默认 6 个键 |

## 相关文档

- [教程](TUTORIAL.md)
- [`Euonia.Telemetry` README](../../../Source/Euonia.Telemetry/README.md) —— 安装、完整选项与行为边界
- [Otel 手册](../Euonia.Telemetry.Otel/API.md) —— 接 OpenTelemetry 与 OTLP 导出
- [Tenancy 手册](../Euonia.Tenancy/API.md) —— 租户标签来源
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` HTTP 中间件
- [根 README](../../../README.md)
