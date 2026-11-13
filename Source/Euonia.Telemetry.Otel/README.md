# Euonia.Telemetry.Otel

Euonia 遥测的 OpenTelemetry 适配层：把内核的遥测源接入 OTel Provider，负责显式源目录、Resource、日志 Scope 与传播器桥接。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Telemetry.Otel`
- **NuGet 包**：`Euonia.Telemetry.Otel`
- **依赖**：`Euonia.Telemetry` + `OpenTelemetry`、`OpenTelemetry.Extensions.Hosting`、`OpenTelemetry.Exporter.OpenTelemetryProtocol`

## 安装

```bash
dotnet add package Euonia.Telemetry.Otel
```

本包只强依赖 OTel SDK / Hosting / OTLP 导出器；`OpenTelemetry.Instrumentation.*`（AspNetCore、Http、Runtime 等）由宿主按需引用。

## 快速开始

```csharp
using Nerosoft.Euonia.Telemetry.Otel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaOpenTelemetry(
    configureTelemetry: options =>
    {
        options.Enabled = true;
        options.ServiceName = "order-service";
        options.ServiceVersion = "2026.5.0";
        options.Environment = builder.Environment.EnvironmentName;
    },
    configure: otel => otel.AddOtlpExporter());
```

签名为：

```csharp
public static IServiceCollection AddEuoniaOpenTelemetry(
    this IServiceCollection services,
    Action<TelemetryOptions>? configureTelemetry = null,
    Action<OpenTelemetryBuilder>? configure = null,
    IConfiguration? configuration = null,
    bool configurePropagator = false)
```

一次调用完成的事：

1. 读取 `Euonia:Telemetry` 配置节与 `configureTelemetry` 委托，形成**启动期快照**；
2. 注册（或复用）`TelemetrySourceRegistry`，并调用 `AddEuoniaTelemetry()` 注册内核服务；
3. `AddOpenTelemetry().ConfigureResource(...).WithLogging()`，按启动期开关决定是否 `WithTracing` / `WithMetrics`；
4. 打开日志的 `IncludeScopes` 与 `IncludeFormattedMessage`；
5. `configurePropagator` 为 `true` 时调用 `UseEuoniaPropagator()`；
6. 执行宿主的 `configure` 委托（添加导出器与 instrumentation）。

## 主要功能

- **显式源目录**：`AddEuoniaSources` 同时订阅 `EuoniaSources.Default` 静态目录与（可选的）`TelemetrySourceRegistry` 中已创建的源，不扫描程序集。
- **Resource 快照**：`service.name`、`service.version`、`deployment.environment`、`euonia.version` 在注册期写入。
- **OTLP 导出**：`AddEuoniaOtlpExporter` 是 `AddOtlpExporter()` 的可读性包装，仅用于表达式上下文。
- **传播器桥接**：`UseEuoniaPropagator` 设置进程级 TraceContext + Baggage 组合传播器。
- **幂等注册**：重复调用不重复构建 Provider / 导出器，但第二次的选项与构建器配置仍会生效。

## 核心类型

| 类型 | 作用 |
|------|------|
| `EuoniaOpenTelemetryExtensions` | `AddEuoniaOpenTelemetry`、两个 `AddEuoniaSources` 重载、`UseEuoniaPropagator` |
| `EuoniaTelemetryOpenTelemetryBuilderExtensions` | `AddEuoniaOtlpExporter(this TracerProviderBuilder)` |
| `EuoniaSources` | `Default` 静态源目录快照与 `Add(string)` 扩展入口 |

`EuoniaSources.Default` 当前包含 16 个名称：`Nerosoft.Euonia.Bus`、`Nerosoft.Euonia.Bus.Http`、`Nerosoft.Euonia.Bus.RabbitMq`、`Nerosoft.Euonia.Bus.ActiveMq`、`Nerosoft.Euonia.Bus.Grpc`、`Euonia.Osba`、`Nerosoft.Euonia.Auditing`、`Nerosoft.Euonia.Tenancy`、`Nerosoft.Euonia.Repository`、`Nerosoft.Euonia.Uow`、`Nerosoft.Euonia.Caching`、`Nerosoft.Euonia.Security`、`Nerosoft.Euonia.Quartz`、`Nerosoft.Euonia.Concurrency`、`Nerosoft.Euonia.Hosting`、`Nerosoft.Euonia.Application`。

## 配置项

本包读取的仍是内核的 `Euonia:Telemetry` 节（`TelemetryOptions.ConfigurationSection`），但**只在注册期读取一次**：

| 键 | 类型 | 默认值 | 在本包中的作用 |
|------|------|--------|----------------|
| `Enabled` | `bool` | `false` | 决定是否构建 Tracing / Metrics 管道；`false` 时不订阅任何 Euonia 源 |
| `TracesEnabled` | `bool` | `true` | 启动期是否 `WithTracing` |
| `MetricsEnabled` | `bool` | `true` | 启动期是否 `WithMetrics` |
| `ServiceName` | `string?` | `null` | Resource `service.name` |
| `ServiceVersion` | `string?` | `null` | Resource `service.version` |
| `Environment` | `string?` | `null` | Resource `deployment.environment` |
| `Redaction` / `Components` | 对象 | 见内核文档 | 注册后合并进 DI 侧选项，不参与 Resource |

注册期读取配置的前提：`IConfiguration` 以**实例**方式注册，或通过 `configuration` 参数显式传入。

```json
{
  "Euonia": {
    "Telemetry": {
      "Enabled": true,
      "ServiceName": "order-service",
      "ServiceVersion": "2026.5.0",
      "Environment": "Production"
    }
  }
}
```

```bash
# .NET 原生：双下划线表示层级
Euonia__Telemetry__Enabled=true
Euonia__Telemetry__ServiceName=order-service
Euonia__Telemetry__Environment=Production

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_TELEMETRY_ENABLED=true
EUONIA_TELEMETRY_SERVICENAME=order-service
EUONIA_TELEMETRY_ENVIRONMENT=Production
```

## 使用示例

### 接入 OTLP 并追加宿主的 instrumentation

```csharp
using Nerosoft.Euonia.Telemetry.Otel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

builder.Services.AddEuoniaOpenTelemetry(
    configureTelemetry: options =>
    {
        options.Enabled = true;
        options.ServiceName = "order-service";
    },
    configure: otel =>
    {
        otel.WithTracing(tracing => tracing
                 .AddAspNetCoreInstrumentation()   // 需宿主自行引用
                 .AddHttpClientInstrumentation())
             .WithMetrics(metrics => metrics
                 .AddAspNetCoreInstrumentation()
                 .AddRuntimeInstrumentation());

        otel.AddOtlpExporter();
    },
    configuration: builder.Configuration);
```

### 手工接管 TracerProvider / MeterProvider

已有自定义 Provider 构建代码时，只借用源目录与导出器包装：

```csharp
using Nerosoft.Euonia.Telemetry.Otel;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddEuoniaSources()             // 订阅 Euonia 默认源
           .AddEuoniaOtlpExporter());      // 等价于 AddOtlpExporter()
```

需要导出本包未知的自定义源时，**先**声明再构建 Provider：

```csharp
EuoniaSources.Add("MyCompany.Billing");
```

### 关闭传播器覆盖（默认行为）

宿主已自行配置 `TextMapPropagator` 时保持默认即可；确需 Euonia 覆盖时：

```csharp
builder.Services.AddEuoniaTelemetry(options => options.Enabled = true);
EuoniaOpenTelemetryExtensions.UseEuoniaPropagator(); // 进程级全局设置，最后一次调用生效
```

## 注意事项 / 限制

- **源订阅在 Provider 构建时固定**：构建后再 `EuoniaSources.Add` 或再创建 `TelemetrySource` 都不会被导出；自定义源必须在 `AddEuoniaOpenTelemetry` 之前声明。
- `AddEuoniaSources` 的 `registry` 参数只包含**构建 Provider 之前**已创建的源；延迟创建的组件源需要静态目录或提前创建。
- `UseEuoniaPropagator` 是进程级副作用；`configurePropagator` 默认为 `false`，宿主已有传播器配置时不要打开。
- `IConfiguration` 以工厂方式注册（`services.AddSingleton<IConfiguration>(sp => ...)`）时注册期读不到配置，必须通过 `configuration` 参数传入。
- Resource 属性在注册期固定，运行期修改 `ServiceName` / `Environment` 只影响 Activity 标签，不影响已构建的 Resource。
- `AddEuoniaOtlpExporter` 只扩展 `TracerProviderBuilder`；指标与日志导出请直接使用 OTel 原生的 `AddOtlpExporter()`。
- 本包不替宿主引入任何 instrumentation 包，缺失时 `AddAspNetCoreInstrumentation` 等扩展方法无法解析。

## 常见问题与故障排查

### OTLP 端点收不到任何数据

**原因**：`AddEuoniaOpenTelemetry` 只在注册期开关为真时才构建对应管道（`EuoniaOpenTelemetryExtensions.cs:117-125`），且 `configure` 委托里没有添加导出器时也没有出口。
**解决**：确认 `options.Enabled` 与 `TracesEnabled` / `MetricsEnabled` 为真，并在 `configure` 里调用 `otel.AddOtlpExporter()`；导出目标读 OTel 自己的 `OTEL_EXPORTER_OTLP_ENDPOINT` 等环境变量。

### 自己写的源（自定义 `sourceName`）不出现在后端

**原因**：OTel 的源订阅在 Provider 构建时固定，构建之后再创建或登记的源不会被导出（`EuoniaSources.cs:8-11`、`EuoniaOpenTelemetryExtensions.cs:152-155`）。
**解决**：在 `AddEuoniaOpenTelemetry` **之前**调用 `EuoniaSources.Add("MyCompany.Billing")`；或把该源名加入静态目录后重新构建 Provider。

### 改了 `ServiceName` / `Environment`，后端 Resource 没变

**原因**：Resource 在注册期读取配置与委托后一次性写入（`EuoniaOpenTelemetryExtensions.cs:61-72,223-245`），运行期热更新只影响埋点开关。
**解决**：Resource 变更必须重启进程；部署时用环境变量 `Euonia__Telemetry__ServiceName` 注入，避免改配置文件后忘记重启。

### 下游服务收不到 `traceparent`

**原因**：`configurePropagator` 默认 `false`，本包不会覆盖宿主的全局传播器（`EuoniaOpenTelemetryExtensions.cs:37,134-138`）。
**解决**：宿主没配传播器时显式传 `configurePropagator: true`，或在宿主侧自行 `Sdk.SetDefaultTextMapPropagator(...)`；调用 `UseEuoniaPropagator()` 的位置必须早于任何出站调用。

### 配置文件里的 `Euonia:Telemetry` 没有被读到

**原因**：注册期只能从**以实例方式**注册的 `IConfiguration` 读取（`EuoniaOpenTelemetryExtensions.cs:66-67`）；工厂注册（`AddSingleton<IConfiguration>(sp => ...)`）读不到。
**解决**：调用时显式传 `configuration: builder.Configuration`。

## 最佳实践

- **显式传 `configuration`**：`AddEuoniaOpenTelemetry(..., configuration: builder.Configuration)`，不要依赖容器探测。
- **一进程一次注册**：重复调用虽幂等，但把额外导出器/采样器集中在唯一一次 `configure` 里更好排查。
- **用 `filter` 收窄订阅**：`otel.WithTracing(t => t.AddEuoniaSources(filter: name => name != "Nerosoft.Euonia.Hosting"))` 可排除噪声源，降低后端压力。
- **采样交给宿主**：本包不设置 Sampler，采样率用 `otel.WithTracing(t => t.SetSampler(new TraceIdRatioBasedSampler(0.1)))` 在 `configure` 中声明。
- **传播器只在一处设置**：要么 `configurePropagator: true`，要么宿主自配，二者不要同时出现。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；包版本 `2026.5.0`（`project.props:3`）。
- **关键依赖**：OpenTelemetry `1.21.0`（`Directory.Packages.props:9,31-33`），同时引用 Hosting 与 OTLP 导出器；`Euonia.Telemetry` 与 `Euonia.Core` 同版本。
- **升级注意**：升级 OpenTelemetry 时同步升级宿主的 `OpenTelemetry.Instrumentation.*`，它们与 SDK 主版本需一致。
- **已知边界**：只包装 tracer 侧的 OTLP 导出器；指标与日志导出用 OTel 原生 API；源订阅在 Provider 构建期固定。

## 相关包

- [`Euonia.Telemetry`](../Euonia.Telemetry/README.md) —— 遥测内核（源、脱敏、富化、传播）
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— `UseEuoniaTelemetry` HTTP 中间件
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 租户标签来源
- [`Euonia.Security`](../Euonia.Security/README.md) —— 授权埋点
- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计埋点
- [`Euonia.Osba`](../Euonia.Osba/README.md) —— 业务对象埋点
- [`Euonia.Core`](../Euonia.Core/README.md) —— 执行上下文

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
