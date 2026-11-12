# Euonia.Telemetry.Otel 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

把 Euonia 遥测源接入 OpenTelemetry：显式源目录、Resource 快照、日志 Scope 与传播器桥接，导出交给宿主选定的 exporter。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Telemetry.Otel` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Telemetry.Otel` |
| 依赖 | `Euonia.Telemetry` + OpenTelemetry SDK / Hosting / OTLP 导出器 |
| 完整参考 | [`Source/Euonia.Telemetry.Otel/README.md`](../../../Source/Euonia.Telemetry.Otel/README.md) |

## 教程

### 场景

给 `orders-api` 接上 OTel：先用一个**自写控制台 exporter**（只用 SDK，不装额外包）确认 Resource 与 Span 正常，再把同一段接线切换到 OTLP，并让出站请求带上 `traceparent`。

### 前置条件

- 已完成 [Telemetry 手册](../Euonia.Telemetry/TUTORIAL.md) 的接线，理解 `TelemetrySource` 与开关语义。
- 若要用 OTLP：本地有 Collector 或后端（如 Jaeger、Tempo），或先用控制台 exporter 验证。

### 1. 安装

```bash
dotnet add package Euonia.Telemetry.Otel
```

instrumentation 包（AspNetCore / HttpClient / Runtime）不在本包依赖里，需要时自行引用。

### 2. 接线：自写 exporter 验证

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Nerosoft.Euonia.Telemetry.Otel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaOpenTelemetry(
    configureTelemetry: options =>
    {
        options.Enabled = true;
        options.ServiceName = "orders-api";
        options.ServiceVersion = "1.0.0";
        options.Environment = builder.Environment.EnvironmentName;
    },
    configure: otel =>
    {
        otel.WithTracing(tracing => tracing
            .AddProcessor(new SimpleActivityExportProcessor(new ConsoleActivityExporter())));
    },
    configuration: builder.Configuration);   // 注册期快照必须能读到 IConfiguration

var app = builder.Build();
app.MapGet("/ping", () => Results.Ok("pong"));
app.Run();

public sealed class ConsoleActivityExporter : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            Console.WriteLine($"[otel] source={activity.Source.Name} name={activity.DisplayName} trace={activity.TraceId}");
        }

        return ExportResult.Success;
    }
}
```

```csharp
// 业务侧埋点：与不接 OTel 时完全一致，切换导出不影响业务代码。
using Nerosoft.Euonia.Telemetry;

public sealed class OrderReader
{
    private readonly TelemetrySource _source;

    public OrderReader(ITelemetrySourceFactory telemetry)
        => _source = telemetry.Create("Nerosoft.Euonia.Repository");

    public string Read(string id)
    {
        using var activity = _source.StartActivity(TelemetryNames.RepositoryExecute);
        activity.SetTag("db.system", "sqlite");
        activity.SetOutcome(true);
        return id;
    }
}
```

运行并触发：

```bash
dotnet run
curl -s http://localhost:5000/ping
```

预期输出形如：

```text
[otel] source=Nerosoft.Euonia.Repository name=euonia.repository.execute trace=4bf92f3577b34da6a3ce929d0e0e4736
```

说明两件事：源被订阅（`AddEuoniaSources` 生效），且 `ActivitySource` 名称就是组件源名。

### 3. 切到 OTLP 导出

把 `configure` 换成 OTLP，其它代码不动：

```csharp
builder.Services.AddEuoniaOpenTelemetry(
    configureTelemetry: options =>
    {
        options.Enabled = true;
        options.ServiceName = "orders-api";
        options.Environment = "Production";
    },
    configure: otel =>
    {
        otel.WithTracing(tracing => tracing
                 .AddAspNetCoreInstrumentation()      // 需宿主自行引用
                 .AddOtlpExporter())
             .WithMetrics(metrics => metrics
                 .AddEuoniaSources()
                 .AddOtlpExporter());
    },
    configuration: builder.Configuration,
    configurePropagator: true);   // 让出站请求带 traceparent
```

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```

后端应能看到 `service.name=orders-api` 的 Resource 与 `euonia.repository.execute` 的 Span；访问一次 `/ping` 应产生 HTTP Span 与其子 Span。

### 常见失败点

- **完全没有 span**：`configureTelemetry` 里没开 `Enabled`，注册期就不会建 Tracing 管道（`EuoniaOpenTelemetryExtensions.cs:117-125`）。
- **只有 HTTP span，没有业务 span**：自定义源不在 `EuoniaSources.Default` 且没在注册前 `EuoniaSources.Add(...)`；OTel 的源订阅在 Provider 构建时固定。
- **Resource 改了不生效**：`ServiceName` 等属性在注册期写入，必须重启进程。
- **下游没有 traceparent**：`configurePropagator` 默认 `false`，需要显式打开或由宿主配置全局传播器。
- **配置节读不到**：`IConfiguration` 以工厂方式注册时注册期不可读，必须传 `configuration: builder.Configuration`。

## 相关文档

- [API 参考](API.md)
- [`Euonia.Telemetry.Otel` README](../../../Source/Euonia.Telemetry.Otel/README.md) —— 完整说明与限制
- [Telemetry 手册](../Euonia.Telemetry/TUTORIAL.md) —— 埋点内核、脱敏与日志 Scope
- [Tenancy 手册](../Euonia.Tenancy/TUTORIAL.md) —— 租户标签来源
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` 与管道顺序
- [根 README](../../../README.md)
