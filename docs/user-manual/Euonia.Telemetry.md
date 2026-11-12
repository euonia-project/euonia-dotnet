# Euonia.Telemetry 用户手册

遥测内核：用统一的 `TelemetrySource` 同时产出 `Activity` 与指标，并把租户、关联标识写进 Trace 而不污染指标。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Telemetry` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Telemetry` |
| 依赖 | `Euonia.Core`（`AmbientExecutionContext`）与 `Microsoft.Extensions.*` 抽象包 |
| 完整参考 | [`Source/Euonia.Telemetry/README.md`](../../Source/Euonia.Telemetry/README.md) |

## 教程

### 场景

一个订单查询 API：给仓储埋一条 `euonia.repository.execute` 的 Span，带上租户与关联标识，并让业务日志自动带 `TraceId`。先用**进程内探针**确认 Span 真的产生，再换成 OTel 导出（见 [Otel 手册](Euonia.Telemetry.Otel.md)）。

前置条件：.NET SDK 支持 `net10.0` / `net11.0`；不需要 OTel SDK 或 Collector，验证阶段只用一个 `ActivityListener`。

### 1. 安装

```bash
dotnet add package Euonia.Telemetry
```

### 2. 接线与配置

`Program.cs`（minimal API，配置来自 `appsettings.json` 与环境变量）：

```csharp
using Nerosoft.Euonia.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// 自动绑定 Euonia:Telemetry；委托在绑定之后执行，可覆盖配置值。
builder.Services.AddEuoniaTelemetry(options =>
{
    options.Enabled = true;
    options.ServiceName = "orders-api";
});

builder.Services.AddScoped<OrderReader>();
builder.Services.AddHostedService<SpanProbeHostedService>();

var app = builder.Build();

app.MapGet("/orders/{id}", async (string id, OrderReader reader, ILogger<Program> logger,
                                  ITelemetryContextProvider contextProvider, CancellationToken cancellationToken) =>
{
    using var scope = TelemetryLogScope.Begin(logger, contextProvider);
    var order = await reader.ReadAsync(id, cancellationToken);
    return Results.Ok(new { order.Id, order.TenantId });
});

app.Run();
```

`appsettings.json` 中对应的配置节：

```json
{
  "Euonia": {
    "Telemetry": {
      "Enabled": true,
      "ServiceName": "orders-api",
      "TracesEnabled": true,
      "MetricsEnabled": true,
      "RecordTenantId": true,
      "RecordUserId": false
    }
  }
}
```

```bash
# .NET 原生：双下划线表示层级
Euonia__Telemetry__Enabled=true
Euonia__Telemetry__ServiceName=orders-api

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_TELEMETRY_ENABLED=true
EUONIA_TELEMETRY_SERVICENAME=orders-api
```

### 3. 组件内埋点

`OrderReader` 只依赖 `ITelemetrySourceFactory`；源名同时作为 `ActivitySource` 与 `Meter` 名称：

```csharp
using System.Diagnostics;
using Nerosoft.Euonia.Telemetry;

public sealed record Order(string Id, string TenantId);

public sealed class OrderReader
{
    private readonly TelemetrySource _source;

    public OrderReader(ITelemetrySourceFactory telemetry)
    {
        _source = telemetry.Create("Nerosoft.Euonia.Repository");
    }

    public async Task<Order> ReadAsync(string id, CancellationToken cancellationToken)
    {
        using var activity = _source.StartActivity(TelemetryNames.RepositoryExecute);
        activity.SetTag("db.system", "sqlite");

        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await Task.Delay(20, cancellationToken);   // 占位：真实查询
            activity.SetOutcome(true);
            return new Order(id, "t_acme");
        }
        catch (Exception exception)
        {
            activity.SetOutcome(false, exception.GetType().FullName);
            activity.RecordException(exception);
            throw;
        }
        finally
        {
            _source.Histogram("euonia.repository.duration", "s")
                   .Record(Stopwatch.GetElapsedTime(startedAt).TotalSeconds);
        }
    }
}
```

### 4. 验证：用探针确认 Span 真的产生

没有监听者时 `StartActivity` 返回空句柄，所以“看不到 Span”通常不是 bug。把下面这个探针注册为 `IHostedService`，Span 结束时打印到控制台：

```csharp
using System.Diagnostics;

public sealed class SpanProbeHostedService : IHostedService
{
    private ActivityListener? _listener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Repository",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                Console.WriteLine($"[span] {activity.DisplayName} trace={activity.TraceId} span={activity.SpanId}");
                foreach (var tag in activity.TagObjects)
                {
                    Console.WriteLine($"       {tag.Key} = {tag.Value}");
                }
            }
        };

        ActivitySource.AddActivityListener(_listener);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Dispose();
        return Task.CompletedTask;
    }
}
```

运行并触发一次请求：

```bash
dotnet run
# 另一个终端
curl -s http://localhost:5000/orders/42
```

预期看到一行 `[span] euonia.repository.execute trace=...`，其后是 `db.system = sqlite` 与 `euonia.outcome = success` 两个标签。

### 常见失败点

- **一条 `[span]` 都没有**：`Enabled` 默认为 `false`；探针的 `ShouldListenTo` 必须与 `Create` 的源名完全一致。
- **配置节没生效**：`AddEuoniaTelemetry` 只在容器中已存在 `IConfiguration` 时才绑定 `Euonia:Telemetry`（`TelemetryServiceCollectionExtensions.cs:26-29`）。
- **指标为空**：`MetricsEnabled=false` 时 `Counter` / `Histogram` 返回回退实例，写入被丢弃。
- **同名指标抛异常**：同名指标的 `unit` / `description` 不一致会抛 `InvalidOperationException`（`TelemetrySource.cs:242-245`）。

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

- [`Euonia.Telemetry` README](../../Source/Euonia.Telemetry/README.md) —— 安装、完整选项与行为边界
- [Otel 手册](Euonia.Telemetry.Otel.md) —— 接 OpenTelemetry 与 OTLP 导出
- [Tenancy 手册](Euonia.Tenancy.md) —— 租户标签来源
- [`Source/Euonia.Hosting/README.md`](../../Source/Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` HTTP 中间件
- [根 README](../../README.md)
