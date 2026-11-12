# Euonia.Telemetry

Euonia 遥测内核：集中创建 `ActivitySource` / `Meter`、按组件开关、标签脱敏、上下文富化、W3C 传播与日志 Scope；**不依赖 OpenTelemetry SDK**。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Telemetry`
- **NuGet 包**：`Euonia.Telemetry`
- **依赖**：`Euonia.Core`（`AmbientExecutionContext` / `IExecutionContextAccessor`）与 `Microsoft.Extensions.*` 抽象包

## 安装

```bash
dotnet add package Euonia.Telemetry
```

需要把数据导出到 OTLP 采集端时，追加适配包 `Euonia.Telemetry.Otel`。

## 快速开始

```csharp
using Nerosoft.Euonia.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaTelemetry(options =>
{
    options.Enabled = true;
    options.ServiceName = "order-service";
    options.RecordTenantId = true;
});
```

`AddEuoniaTelemetry(this IServiceCollection services, Action<TelemetryOptions>? configure = null)` 只注册内核服务，不引入 OTel SDK：

| 注册项 | 生命周期 | 说明 |
|---|---|---|
| `ITelemetryRedactor` → `DefaultTelemetryRedactor` | 单例 | 按配置的键集合脱敏 |
| `ITelemetryPropagator` → `DefaultTelemetryPropagator` | 单例 | 基于 `DistributedContextPropagator.Current` |
| `TelemetrySourceRegistry` | 单例 | 记录已创建的源，供 OTel 适配层订阅 |
| `ITelemetryContextProvider` → `DefaultTelemetryContextProvider` | 单例 | 读取 `Activity.Current` 与执行上下文 |
| `ITelemetryEnricher` → `TelemetryContextEnricher` | 单例 | Activity 创建后补充租户与关联标签 |
| `ITelemetrySourceFactory` → `TelemetrySourceFactory` | 单例 | 按组件开关创建并缓存遥测源 |

`IConfiguration` 已注册时自动绑定 `Euonia:Telemetry` 节；`configure` 委托在绑定之后执行，因此优先级高于配置文件。

## 主要功能

- **组件级遥测源**：组件通过 `ITelemetrySourceFactory.Create("<源名>")` 获得 `TelemetrySource`；同名重复调用返回同一实例，`Sources` 返回已创建源的快照。
- **空操作语义**：无监听者或开关关闭时 `StartActivity` 返回空句柄，`Counter` / `Histogram` 返回回退实例，埋点代码无需分支。
- **标签脱敏**：写入 `Activity` 的标签（含事件参数与异常堆栈）统一经过 `ITelemetryRedactor`，命中键的值替换为 `***`。
- **上下文富化**：`euonia.correlation_id`、`euonia.request_trace_id`、`tenant.id`、`tenant.actor.id`、`tenant.cross`、`euonia.user.id` 只写 Trace，**不写指标**。
- **W3C 传播**：`ITelemetryPropagator.Inject` / `Extract`；非法 `traceparent` 按新根处理，不抛异常。
- **日志 Scope**：`TelemetryLogScope.Begin` 把 TraceId / SpanId / CorrelationId / TenantId / RequestTraceId 写入日志作用域。

## 核心类型

| 类型 | 作用 |
|------|------|
| `TelemetrySource` | `ActivitySource` + `Meter` 的持有者：`StartActivity`、`Counter`、`Histogram`、`IsTraceEnabled`、`IsMetricEnabled` |
| `TelemetryActivity` | 只读活动句柄：`SetTag`、`SetOutcome`、`AddEvent`、`RecordException`，`Dispose` 结束活动 |
| `ITelemetrySourceFactory` | `Create(sourceName, meterName = null)` 与 `Sources`；同名幂等 |
| `TelemetrySourceRegistry` | `Register` / `GetAll`，不持有源的所有权 |
| `ITelemetryRedactor` | `ShouldRedact` / `Redact`；默认实现按键名匹配 |
| `ITelemetryEnricher` | `Enrich(Activity, TelemetryContext?)`；默认实现为 `TelemetryContextEnricher` |
| `ITelemetryContextProvider` | `GetContext()` 返回 `TelemetryContext`；默认不生成新标识 |
| `TelemetryContext` | 只读记录：`TraceId` / `SpanId` / `CorrelationId` / `RequestTraceId` / `TenantId` / `ActorTenantId` / `UserId` / `IsCrossTenant` |
| `ITelemetryPropagator` | `Inject` / `Extract`，载体为 `IDictionary<string, object>` |
| `TelemetryLogScope` | `Begin(ILogger, ITelemetryContextProvider?)` 返回 `IDisposable` |
| `TelemetryNames` | Activity 名、标签键与指标名的统一常量 |
| `TelemetryOptions` / `TelemetryRedactionOptions` / `TelemetryComponentOptions` | 全局选项、脱敏选项、单组件开关 |

## 配置项

配置节常量：`TelemetryOptions.ConfigurationSection` = `Euonia:Telemetry`。

| 键 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | `bool` | `false` | 总开关；关闭时不创建 Activity，指标返回回退实例 |
| `TracesEnabled` | `bool` | `true` | 跟踪子开关，与 `Enabled` 同时生效 |
| `MetricsEnabled` | `bool` | `true` | 指标子开关，与 `Enabled` 同时生效 |
| `ServiceName` | `string?` | `null` | 服务名；写入 Activity 的 `service.name` 标签，OTel 侧同时写入 Resource |
| `ServiceVersion` | `string?` | `null` | 服务版本；由 OTel Resource 使用（`service.version`） |
| `Environment` | `string?` | `null` | 部署环境；由 OTel Resource 使用（`deployment.environment`） |
| `MessageTypeTag` | `string` | `ShortName` | 消息类型标签模式（`ShortName` / `FullName`）；见「注意事项」 |
| `RecordUserId` | `bool` | `false` | 是否把用户标识写入 Trace 标签 `euonia.user.id` |
| `RecordTenantId` | `bool` | `true` | 是否把租户标识写入 Trace 标签 |
| `Redaction` | `TelemetryRedactionOptions` | 见下 | 脱敏选项，支持热更新 |
| `Components` | `Dictionary<string, TelemetryComponentOptions>` | 空 | 按组件覆盖开关；键比较忽略大小写 |

`Redaction` 子项：

| 键 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Redaction:Enabled` | `bool` | `true` | 是否启用脱敏 |
| `Redaction:Keys` | `string[]` | 6 项（见下） | 需要脱敏的标签键，命中即整值替换 |

默认 `Keys`：`http.request.header.authorization`、`http.request.header.cookie`、`db.statement`、`messaging.message.payload`、`exception.message`、`exception.stacktrace`。

`Components` 的键可以是完整源名（`Nerosoft.Euonia.Repository`），也可以是去掉 `Nerosoft.Euonia.` / `Euonia.` 前缀的短名（`Repository`）；每个值有 `Enabled`、`TracesEnabled`、`MetricsEnabled` 三个开关，默认均为 `true`。

```json
{
  "Euonia": {
    "Telemetry": {
      "Enabled": true,
      "ServiceName": "order-service",
      "ServiceVersion": "2026.5.0",
      "Environment": "Production",
      "RecordTenantId": true,
      "RecordUserId": false,
      "Redaction": {
        "Enabled": true,
        "Keys": [ "db.statement", "http.request.header.authorization" ]
      },
      "Components": {
        "Repository": { "MetricsEnabled": false }
      }
    }
  }
}
```

```bash
# .NET 原生：双下划线表示层级
Euonia__Telemetry__Enabled=true
Euonia__Telemetry__ServiceName=order-service
Euonia__Telemetry__Components__Repository__MetricsEnabled=false

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_TELEMETRY_ENABLED=true
EUONIA_TELEMETRY_SERVICENAME=order-service
EUONIA_TELEMETRY_COMPONENTS_REPOSITORY_METRICSENABLED=false
```

## 使用示例

### 组件内埋点

```csharp
using System.Diagnostics;
using Nerosoft.Euonia.Telemetry;

public sealed class OrderRepository
{
    private readonly TelemetrySource _source;

    public OrderRepository(ITelemetrySourceFactory telemetry)
    {
        // 同名源全局唯一；组件名同时作为 ActivitySource / Meter 名称。
        _source = telemetry.Create("Nerosoft.Euonia.Repository");
    }

    public async Task<Order?> FindAsync(string id, CancellationToken cancellationToken)
    {
        using var activity = _source.StartActivity(TelemetryNames.RepositoryExecute);
        activity.SetTag("db.system", "postgresql");

        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var order = await LoadAsync(id, cancellationToken);
            activity.SetOutcome(true);
            return order;
        }
        catch (Exception exception)
        {
            activity.SetOutcome(false, exception.GetType().FullName);
            activity.RecordException(exception);
            throw;
        }
        finally
        {
            // 指标只记耗时与结果，不写租户/用户标识。
            _source.Histogram("euonia.repository.duration", "s")
                   .Record(Stopwatch.GetElapsedTime(startedAt).TotalSeconds);
        }
    }
}
```

### ASP.NET Core 管道

```csharp
// Startup.ConfigureServices
services.AddEuoniaTelemetry(options => options.Enabled = true);

// Startup.Configure：保留租户中间件之后再补 HTTP 活动标签（Euonia.Hosting）
app.UseTenancy();
app.UseEuoniaTelemetry();
app.UseAuthorization();
```

### 日志 Scope

```csharp
using Nerosoft.Euonia.Telemetry;

public sealed class OrderService(ILogger<OrderService> logger, ITelemetryContextProvider contextProvider)
{
    public void Handle(string orderId)
    {
        using var scope = TelemetryLogScope.Begin(logger, contextProvider);
        logger.LogInformation("Handling order {OrderId}.", orderId);
    }
}
```

## 注意事项 / 限制

- `AddEuoniaTelemetry` **不注册任何导出器**，也不读取 OTLP 端点；导出由 `Euonia.Telemetry.Otel` 或宿主自行配置。
- `IsTraceEnabled` 依赖 `ActivitySource.HasListeners()`：没有订阅者时不会创建 Activity，测试断言前必须先挂监听器。
- 同名指标以不同 `unit` / `description` 再次获取会抛 `InvalidOperationException`（显式失败，不静默复用）。
- 指标关闭或源已释放时，`Counter` / `Histogram` 返回共享回退实例（`Euonia.Telemetry.Disposed` 下的 `noop`），写入被丢弃。
- 脱敏是**键名**匹配（大小写不敏感），不解析值内容；键命中时整个值替换为 `***`。
- 组件内直接使用静态 `Meter` / `ActivitySource` 的埋点不受 `Components` 开关控制，只有经 `TelemetrySource` 创建的源受影响。
- 选项热更新只切换已创建源的跟踪 / 指标开关；`ServiceName` 等 Resource 属性在 OTel Provider 构建期固定。
- `TelemetryOptions.MessageTypeTag` 当前仅在选项中声明，本包内没有消费点（消息适配层可自行读取）；这是预留扩展点，不是缺失配置。

## 常见问题与故障排查

### 开了遥测但一条 Span 都没有

**现象**：`StartActivity` 返回的句柄 `IsRecording == false`，OTLP 端也没有任何数据。
**原因**：`TelemetryOptions.Enabled` 默认是 `false`；即使打开，`IsTraceEnabled` 还要求 `ActivitySource.HasListeners()` 为真。
**解决**：显式设置 `options.Enabled = true`，并让 Provider 订阅源名：OTel 用 `AddEuoniaSources()`，自建 Provider 用 `AddSource("Nerosoft.Euonia.Repository")`。

### appsettings 里写了 `Euonia:Telemetry` 却不生效

**原因**：`AddEuoniaTelemetry` 只在容器里**已经存在** `IConfiguration` 时才绑定配置节（`TelemetryServiceCollectionExtensions.cs:26-29`）；先注册遥测、后注册 `IConfiguration` 的顺序读不到配置。
**解决**：把 `AddEuoniaTelemetry` 放到宿主配置构建之后（`Host.CreateApplicationBuilder` 默认满足），或直接传 `configure` 委托——委托在绑定之后执行，优先级更高。

### 有 Trace 但没有指标

**原因**：`MetricsEnabled` 或 `Enabled` 为 `false` 时 `Counter` / `Histogram` 返回共享回退实例（`TelemetrySource.cs:162-167`），写入被丢弃；另一种情况是 `Create(sourceName, meterName)` 用了自定义 `meterName`，它不在 `EuoniaSources.Default` 静态目录里，因此没被订阅。
**解决**：核对两个开关；自定义 `meterName` 时先 `EuoniaSources.Add(meterName)`，或在 `AddEuoniaSources(filter:)` 之后手动 `AddMeter(meterName)`。

### 运行期抛 `InvalidOperationException: Metric '...' was already defined with different metadata`

**原因**：同名指标的 `unit` 或 `description` 不一致，`TelemetrySource` 显式失败而不是静默复用（`TelemetrySource.cs:242-245`）。
**解决**：把指标名、单位、描述收敛为常量（可放在自己的 `TelemetryNames` 同层类型里），禁止各组件各自拼字符串。

### 日志里没有 TraceId / SpanId

**原因**：OTel 的 `IncludeScopes` 只作用于 OTel 日志管道；自定义日志必须在代码里用 `TelemetryLogScope.Begin` 写入作用域（`TelemetryLogScope.cs:16-33`），Serilog 还需 `Enrich.FromLogContext()`。
**解决**：在处理入口 `using var scope = TelemetryLogScope.Begin(logger, contextProvider);`，并确认日志管线开启了 Scope 捕获。

## 最佳实践

- **在组合根一次性打开**：`options.Enabled = true`，再用 `Components` 单独关掉噪声组件，而不是在每个组件里写 `if (Enabled)`。
- **指标名常量化管理**：单位固定用 `s` / `By` / `{item}` 这类稳定写法，避免升级时触发同名元数据冲突。
- **脱敏靠配置而不是靠“不写标签”**：把密钥类键追加到 `Euonia:Telemetry:Redaction:Keys`，保留默认 6 个键。
- **每个组件只 `Create` 一次源**：源在工厂内按 `sourceName|meterName` 缓存，把 `TelemetrySource` 存为单例字段，不要每请求创建。
- **生产保持 `RecordUserId=false`**：用户标识走日志与审计，需要时再按合规要求打开。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；包版本 `2026.5.0`（`project.props:3`）。
- **关键依赖**：`Euonia.Core` 与 `Microsoft.Extensions.*` `11.0.0`（`Directory.Packages.props:5`），不依赖 OpenTelemetry。
- **兼容边界**：本包只做埋点内核，不导出数据；升级 OpenTelemetry 只影响 `Euonia.Telemetry.Otel`，配置文件与标签键保持稳定。
- **已知边界**：Resource 类属性（`ServiceName` 等）在 OTel Provider 构建期固定，热更新只切换跟踪/指标开关。

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— `AmbientExecutionContext` / 执行上下文
- [`Euonia.Telemetry.Otel`](../Euonia.Telemetry.Otel/README.md) —— OpenTelemetry 适配与 OTLP 导出
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` HTTP 中间件
- [`Euonia.Security`](../Euonia.Security/README.md) —— 授权决策埋点
- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计写入埋点
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 租户标签来源
- [`Euonia.Osba`](../Euonia.Osba/README.md) —— 业务对象生命周期埋点

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
