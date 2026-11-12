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
        options.ServiceVersion = "2026.4.7";
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
      "ServiceVersion": "2026.4.7",
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
