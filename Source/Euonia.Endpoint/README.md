# Euonia.Endpoint

把应用服务接口方法同时暴露为 HTTP Minimal API 与 gRPC 服务的契约包。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Endpoint`
- **NuGet 包**：`Euonia.Endpoint`

`Euonia.Endpoint` 只包含特性和共享契约类型，不依赖 ASP.NET Core 或具体传输实现。传输运行时与构建期生成器分别由以下包提供：

| 包 | 作用 |
|---|---|
| `Euonia.Endpoint` | 暴露特性、HTTP 方法枚举、共享 `DataContract` 类型 |
| `Euonia.Endpoint.Http` | Minimal API 运行时、参数绑定、认证元数据、扫描与兜底映射 |
| `Euonia.Endpoint.Grpc` | protobuf-net.Grpc 运行时、生成契约注册、proto 导出 |
| `Euonia.Endpoint.SourceGenerator` | 构建期生成 HTTP lambda、gRPC 契约/适配器与注册标记 |

## 安装

```bash
dotnet add package Euonia.Endpoint
dotnet add package Euonia.Endpoint.Http
dotnet add package Euonia.Endpoint.Grpc
dotnet add package Euonia.Endpoint.SourceGenerator
```

运行时包目标框架为 `net10.0` / `net11.0`；SourceGenerator 为 `netstandard2.0` + `net10.0`（仅 `netstandard2.0` 进入 NuGet analyzer）。

## 快速开始

```csharp
using System.Runtime.Serialization;
using Nerosoft.Euonia.Endpoint;

public interface IOrderService
{
    [ExposeWebApi(Prefix = "api/orders", Route = "{id}", Method = new[] { "GET" })]
    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Get")]
    Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default);
}

[DataContract]
public sealed class OrderDto
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;
}
```

宿主注册：

```csharp
builder.Services.AddDynamicHttpEndpoint(builder.Configuration);
builder.Services.AddDynamicGrpcEndpoint(builder.Configuration);

var app = builder.Build();

app.MapDynamicHttpEndpoint();
app.MapDynamicGrpcEndpoint();
```

配置：

```json
{
  "Euonia": {
    "Endpoint": {
      "Http": {
        "EnableExposeApplicationServiceAsWebApi": true
      },
      "Grpc": {
        "EnableExposeApplicationServiceAsGrpc": true
      }
    }
  }
}
```

配置节由运行时包绑定（`Euonia:Endpoint:Http` / `Euonia:Endpoint:Grpc`），环境变量两种写法：

```bash
Euonia__Endpoint__Http__EnableExposeApplicationServiceAsWebApi=true   # .NET 原生：双下划线表示层级
EUONIA_ENDPOINT_HTTP_ENABLEEXPOSEAPPLICATIONSERVICEASWEBAPI=true      # 仓库 alias：大写单下划线（Euonia.Modularity）
```

## 暴露特性

### ExposeWebApiAttribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExposeWebApiAttribute : Attribute
{
    public string Prefix { get; set; }
    public string Route { get; set; }
    public string[] Method { get; set; } // 默认 ["POST"]
}
```

| 属性 | 说明 |
|---|---|
| `Prefix` | 路由前缀；为空时使用 `HttpExposureOptions.DefaultRoutePrefix` + 服务名 |
| `Route` | 路由模板；为空时使用方法名转换出的 kebab-case |
| `Method` | HTTP 方法数组；支持 GET / POST / PUT / DELETE / PATCH / HEAD / OPTIONS / TRACE / CONNECT，默认 `POST` |

### ExposeGrpcAttribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExposeGrpcAttribute : Attribute
{
    public string Prefix { get; set; }
    public string Route { get; set; }
    public GrpcMethod Method { get; set; } // 默认 Unary
}
```

| 属性 | 说明 |
|---|---|
| `Prefix` | gRPC 服务全名；为空时由服务接口名推导 |
| `Route` | gRPC 方法名；为空时由方法名去掉 `Async` 后推导 |
| `Method` | `Unary` / `ServerStreaming` / `ClientStreaming` / `DuplexStreaming` |

## HTTP 方法签名约定

生成器优先处理以下形式，无法处理的签名由运行时兜底扫描处理：

```csharp
Task DoAsync();                                  // 204
Task<T> GetAsync();                              // 200 + JSON
ValueTask<T> GetAsync();                         // 200 + JSON
Task<IResult> HandleAsync();                     // IResult 透传
Task<T> GetAsync(string id, int page);           // route/query
Task<T> CreateAsync(CreateRequest request);      // JSON body
```

参数来源：

- 路由模板中的同名参数 → route；
- 基础类型、枚举、`Guid`、`DateTime`、`DateOnly`、`TimeOnly`、`TimeSpan`、`Uri` 及其集合 → query；
- 复杂类型或 `byte[]` → JSON body；
- `CancellationToken` → `HttpContext.RequestAborted`；
- `HttpContext` / `HttpRequest` / `HttpResponse` / `ClaimsPrincipal` / `Stream` / `PipeReader` → 当前请求对象。

不支持：文件上传、`IAsyncEnumerable` 返回、多个 body 参数、`ref/out/in` 参数、`params`、泛型方法、静态方法。

## gRPC 流式签名

| `GrpcMethod` | 方法签名 |
|---|---|
| `Unary` | `Task<T> Method(Request, CancellationToken)` |
| `ServerStreaming` | `IAsyncEnumerable<T> Method(Request, CancellationToken)` |
| `ClientStreaming` | `Task<T> Method(IAsyncEnumerable<Request>, CancellationToken)` |
| `DuplexStreaming` | `IAsyncEnumerable<T> Method(IAsyncEnumerable<Request>, CancellationToken)` |

`GrpcMethod` 与实际签名不一致时生成器报告 `EUONIA001` 并跳过该方法。

## 共享契约类型

| 类型 | 用途 |
|---|---|
| `EndpointEmpty` | 无返回值或空请求 |
| `DecimalValue` | `decimal` 的 proto 表示，9 位小数，越界抛 `ArgumentOutOfRangeException` |
| `GuidValue` | `Guid` 的 D 格式字符串 |
| `TimestampValue` | `DateTime` 的 Timestamp 表示，支持 1970 年之前的时间 |
| `DurationValue` | `TimeSpan` 的 Duration 表示 |
| `DateOnlyValue` | `DateOnly` |
| `TimeOnlyValue` | `TimeOnly` |

当前 gRPC 类型映射仍使用 protobuf-net 内建 BCL schema；上述类型保留给后续 wire-DTO 映射，暂未自动替换内建类型。

## gRPC DTO 的 DataContract / DataMember 规则

只有 **gRPC wire DTO** 需要 protobuf-net 的契约标注；HTTP 端点使用 `System.Text.Json`，不需要这些特性。

```csharp
using System.Runtime.Serialization;

[DataContract]
public sealed class OrderDto
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string CustomerId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public decimal Amount { get; set; }
}
```

基本规则：

- DTO 类型标 `[DataContract]`；
- 每个需要传输的成员标 `[DataMember(Order = n)]`；
- `Order` 从 **1** 开始，同一类型内唯一；
- `Order` 决定 protobuf 字段号，**一旦发布就不能修改、复用或重排**；
- 新增字段时使用新的、更大的 `Order`；
- 删除字段时保留原字段号，不要给新字段复用；
- 不需要传输的成员使用 `[IgnoreDataMember]`；
- 嵌套类型也必须标 `[DataContract]` / `[DataMember]`；
- 建议使用公共 get/set 属性，并保留公共无参构造函数；
- 集合建议使用 `List<T>` / `T[]` 等具体类型，不要依赖未初始化的集合；
- 枚举成员显式赋值并保持数值稳定，不要依赖成员顺序；
- 多态/继承需要显式 `[ProtoInclude]`，未配置时不要依赖运行时多态。

兼容性：

- 新增可选字段是兼容的；
- 把字段改成 `IsRequired = true` 可能让旧客户端失败；
- 修改字段类型、字段号或名称可能破坏旧客户端；
- 已发布字段号只能保留，不能重新分配。

> `DataContract` / `DataMember` 只影响 gRPC/protobuf-net；HTTP-only DTO 可以保持普通 POCO。

## 行为说明

- 生成端点优先映射；运行时扫描覆盖插件程序集和生成器跳过的签名。
- HTTP 与 gRPC 的 `RequireAuthorization` 默认都是 `true`。
- HTTP 合并接口方法、实现类、实现方法上的 `[Authorize]` / `[AllowAnonymous]`；多实现时只用接口方法元数据，避免 `AllowAnonymous` 误放开。
- gRPC 合并接口/实现类上的 `[Authorize]` / `[AllowAnonymous]`；多实现时只用接口元数据。方法级特性由内置 `GrpcMethodAuthorizationInterceptor` 在调用前校验。
- 扫描范围默认只包含入口程序集；插件或 Application 程序集需要显式加入 `ScanAssemblies`。
- 生成器只处理当前编译中的接口；被引用程序集里的接口需要在那个程序集里也引用生成器。

## 注意事项 / 限制

- 本包**只有契约**：`ExposeWebApi` / `ExposeGrpc` 特性与共享 `DataContract` 类型；不引用 ASP.NET Core、不包含运行时映射逻辑，单独安装不会暴露任何端点。
- 特性必须标在**接口方法**上（`AttributeTargets.Method`）；标在实现类方法上不会被生成器或运行时扫描识别。
- 生成器只处理**当前编译**中的接口；被引用程序集里的接口需要在那个程序集里也引用 `Euonia.Endpoint.SourceGenerator`。
- gRPC 侧**没有运行时兜底**：没有生成映射的程序集只会在映射阶段记录 Warning，方法不会被暴露。
- `DataMember(Order = n)` 是 protobuf 字段号，发布后不能修改、复用或重排；字段号从 1 开始且在同一类型内唯一。
- `RequireAuthorization` 在 HTTP 与 gRPC 的默认值都是 `true`，未配置认证时会拒绝匿名请求。

## 相关包

- [`Euonia.Endpoint.Http`](../Euonia.Endpoint.Http/README.md) —— Minimal API 运行时、参数绑定、认证元数据
- [`Euonia.Endpoint.Grpc`](../Euonia.Endpoint.Grpc/README.md) —— protobuf-net.Grpc 运行时、生成契约注册、proto 导出
- [`Euonia.Endpoint.SourceGenerator`](../Euonia.Endpoint.SourceGenerator/README.md) —— 构建期生成 HTTP lambda、gRPC 契约与注册标记
- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础类型与扩展方法

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。

## 常见问题与故障排查

### 装了包却没有任何端点

**原因**：`Euonia.Endpoint` 只有特性与共享契约，不含运行时；没有 `Euonia.Endpoint.Http` / `Euonia.Endpoint.Grpc` 时不会映射任何东西（本页开头「包职责」表）。
**解决**：按传输加运行时包，并调用 `AddDynamicHttpEndpoint` + `MapDynamicHttpEndpoint`（或 gRPC 对应方法）；确认总开关已打开。

### 特性标了但没生效

**原因**：特性必须标在**接口方法**上；标在实现类方法上不会被生成器或运行时扫描识别（`ExposeWebApiAttribute.cs:6` 的 `AttributeTargets.Method`）。
**解决**：把 `[ExposeWebApi]` / `[ExposeGrpc]` 移到接口方法；实现类只负责方法体。

### gRPC 端点缺失且日志只有 Warning

**原因**：gRPC 没有运行时兜底，程序集没生成映射时只记录「contains [ExposeGrpc] methods but no generated gRPC mapping」。
**解决**：在声明该接口的项目里引用 `Euonia.Endpoint.SourceGenerator`（analyzer 形式），重新构建后确认生成了 `GrpcMappings`。

### 请求返回 401 / `PermissionDenied`

**原因**：HTTP 与 gRPC 的 `RequireAuthorization` 默认都是 `true`，未配置认证方案时会拒绝请求。
**解决**：匿名端点显式标 `[AllowAnonymous]`；需要授权时配置认证方案，或用 `AddDynamicHttpEndpoint(options => options.RequireAuthorization = false)` 按需关闭。

### gRPC 客户端解析旧字段失败

**原因**：`DataMember(Order = n)` 就是 protobuf 字段号，改动、复用或重排会破坏旧客户端。
**解决**：已发布字段号只增不改；删除字段保留字段号；新增字段用更大的 `Order`。

## 最佳实践

- **接口集中放 Application 程序集**：接口所在程序集必须引用生成器，插件/多程序集场景把该程序集显式加入 `ScanAssemblies`。
- **字段号分段规划**：为每个 DTO 预留区间（如 1–50 业务字段、51+ 实验字段），避免多人并行开发时撞号。
- **HTTP-only DTO 不标 `DataContract`**：这些特性只服务 protobuf-net，加上去会让 DTO 被误认为 wire 契约。
- **先开开关再排障**：`EnableExposeApplicationServiceAsWebApi` / `EnableExposeApplicationServiceAsGrpc` 默认 `false`，排障第一步是确认配置已绑定。
- **契约先行**：先把接口方法签名与 `[DataMember]` 定下来再写实现，避免后期改签名导致生成器跳过后退回运行时路径。

## 版本与兼容

| 项 | 值 |
|---|---|
| 目标框架 | `net10.0`、`net11.0` |
| 包版本 | `2026.5.0`（仓库根 `project.props`） |
| 第三方依赖 | 无（不引用 ASP.NET Core，也不引用 protobuf-net） |

- **契约稳定优先**：`ExposeWebApi` / `ExposeGrpc` 的公开属性（`Prefix` / `Route` / `Method`）与 `GrpcMethod` 枚举值是兼容性边界，扩展只会新增成员。
- **共享契约类型仍在演进**：`EndpointEmpty` / `DecimalValue` / `TimestampValue` 等尚未接入自动 wire-DTO 映射，行为以当前源码为准（见「共享契约类型」）。
- 传输能力随运行时包升级，本包不单独引入 TFM 之外的依赖。
