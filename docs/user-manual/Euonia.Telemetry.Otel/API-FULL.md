# Euonia.Telemetry.Otel 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Telemetry.Otel |
| 公共类型 | 3 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Telemetry.Otel

### Nerosoft.Euonia.Telemetry.Otel.EuoniaOpenTelemetryExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaOpenTelemetry(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Telemetry.TelemetryOptions> configureTelemetry = null, Action<OpenTelemetry.OpenTelemetryBuilder> configure = null, Microsoft.Extensions.Configuration.IConfiguration configuration = null, System.Boolean configurePropagator = false)
- static OpenTelemetry.Trace.TracerProviderBuilder AddEuoniaSources(OpenTelemetry.Trace.TracerProviderBuilder builder, Nerosoft.Euonia.Telemetry.TelemetrySourceRegistry registry = null, Func<System.String, System.Boolean> filter = null)
- static OpenTelemetry.Metrics.MeterProviderBuilder AddEuoniaSources(OpenTelemetry.Metrics.MeterProviderBuilder builder, Nerosoft.Euonia.Telemetry.TelemetrySourceRegistry registry = null, Func<System.String, System.Boolean> filter = null)
- static void UseEuoniaPropagator()

### Nerosoft.Euonia.Telemetry.Otel.EuoniaSources

**类型**：static class

- static IReadOnlyList<System.String> Default { get; }
- static void Add(System.String sourceName)

### Nerosoft.Euonia.Telemetry.Otel.EuoniaTelemetryOpenTelemetryBuilderExtensions

**类型**：static class

- static OpenTelemetry.Trace.TracerProviderBuilder AddEuoniaOtlpExporter(OpenTelemetry.Trace.TracerProviderBuilder builder)
