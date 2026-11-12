# Euonia.Endpoint.Grpc

基于 `protobuf-net.Grpc` 的 code-first gRPC 运行时，把 `Euonia.Endpoint` 标记的接口方法暴露为 gRPC 服务。

## 目标框架与元信息

- **目标框架**：`net10.0`、`net11.0`（依赖 ASP.NET Core 共享框架）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Grpc`（`AddDynamicGrpcEndpoint` 位于 `Microsoft.Extensions.DependencyInjection`，`MapDynamicGrpcEndpoint` 位于 `Microsoft.AspNetCore.Builder`）
- **NuGet 包**：`Euonia.Endpoint.Grpc`
- **主要依赖**：`protobuf-net` 3.4.40、`protobuf-net.Grpc` / `protobuf-net.Grpc.AspNetCore` / `protobuf-net.Grpc.Reflection` 1.3.14、`Grpc.AspNetCore.Server.Reflection`

## 工作方式

构建期由 `Euonia.Endpoint.SourceGenerator` 生成：

- `[Service]` 契约接口；
- 请求/响应包装类型；
- 调用应用服务的适配器；
- `AddGenerated` / `MapGenerated` 注册代码；
- 程序集级 `[assembly: GrpcGeneratedMapping(...)]` 标记。

运行时由 `MapDynamicGrpcEndpoint` 发现标记、注册适配器并映射 gRPC 服务。

## 安装

```bash
dotnet add package Euonia.Endpoint
dotnet add package Euonia.Endpoint.Grpc
dotnet add package Euonia.Endpoint.SourceGenerator
```

## 快速开始

注册与映射的最小路径：

```csharp
builder.Services.AddDynamicGrpcEndpoint(builder.Configuration);
```

配置：

```json
{
  "Euonia": {
    "Endpoint": {
      "Grpc": {
        "EnableExposeApplicationServiceAsGrpc": true,
        "RequireAuthorization": true,
        "EnableReflection": false,
        "ScanAssemblies": [ "MyApp" ],
        "ScanEntryAssembly": false
      }
    }
  }
}
```

回调形式：

```csharp
builder.Services.AddDynamicGrpcEndpoint(options =>
{
    options.EnableExposeApplicationServiceAsGrpc = true;
    options.EnableReflection = true;
});
```

显式程序集：

```csharp
builder.Services.AddDynamicGrpcEndpoint(
    options => options.EnableExposeApplicationServiceAsGrpc = true,
    typeof(IOrderService).Assembly);
```

映射：

```csharp
var app = builder.Build();

app.MapDynamicGrpcEndpoint();

app.Run();
```

也可以：

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapDynamicGrpcEndpoint();
});
```

## 核心类型

| 类型 | 作用 |
|---|---|
| `GrpcExposureOptions` | 配置节 `Euonia:Endpoint:Grpc` 的选项（见 [配置项](#配置项)） |
| `GrpcMetadataRegistry` | 方法级授权元数据：`Build(assemblies)` / `GetServiceMetadata` / `GetMethodMetadata` |
| `GrpcMethodAuthorizationInterceptor` | 内置拦截器，在调用前执行方法级授权 |
| `GrpcMethodAuthorizationDescriptor` | 方法级授权描述符：`MethodPath` / `ServiceType` / `MethodName` |
| `GrpcGeneratedMappingAttribute` | 程序集级标记，指向生成的 `GrpcMappings` 类型 |
| `EndpointProtoExporter` | `Export(assembly, outputDirectory)` 导出 `.proto` |
| `EndpointProtoModel` | protobuf 模型配置：`Configure()`，注册时自动调用 |
| `EndpointRouteBuilderExtensions` / `ServiceCollectionExtensions` | `MapDynamicGrpcEndpoint()` / `AddDynamicGrpcEndpoint(...)` |

## 配置项

选项类型：`GrpcExposureOptions`，配置节：`Euonia:Endpoint:Grpc`（`GrpcExposureOptions.SectionName`）。

| 选项 | 默认值 | 说明 |
|---|---|---|
| `EnableExposeApplicationServiceAsGrpc` | `false` | 总开关 |
| `RequireAuthorization` | `true` | 拦截器对无显式授权特性的方法要求已认证用户 |
| `EnableReflection` | `false` | 是否映射 gRPC reflection 服务 |
| `ThrowOnUnregisteredService` | `true` | 生成契约对应的应用服务未注册时抛异常；`false` 时跳过 |
| `ScanAssemblies` | 空 | 要扫描的程序集名称；配置后不再回退入口程序集 |
| `ScanEntryAssembly` | `true` | 未配置 `ScanAssemblies` 时是否扫描入口程序集 |

环境变量（两种写法等价，原生格式优先）：

```bash
Euonia__Endpoint__Grpc__EnableExposeApplicationServiceAsGrpc=true     # .NET 原生：双下划线表示层级
EUONIA_ENDPOINT_GRPC_ENABLEEXPOSEAPPLICATIONSERVICEASGRPC=true        # 仓库 alias：大写单下划线（Euonia.Modularity）

Euonia__Endpoint__Grpc__EnableReflection=true
EUONIA_ENDPOINT_GRPC_ENABLEREFLECTION=true
```

alias 格式的每一段下划线都对应一个层级，段内不拆词；含 `__` 的变量只按原生格式处理。

## 特性

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExposeGrpcAttribute : Attribute
{
    public string Prefix { get; set; }
    public string Route { get; set; }
    public GrpcMethod Method { get; set; }
}
```

| 属性 | 说明 |
|---|---|
| `Prefix` | gRPC 服务全名；为空时由接口名推导 |
| `Route` | gRPC 方法名；为空时由方法名去掉 `Async` 后推导 |
| `Method` | `Unary` / `ServerStreaming` / `ClientStreaming` / `DuplexStreaming` |

`Prefix` / `Route` 都为空时：

```csharp
public interface IOrderService
{
    [ExposeGrpc]
    Task<OrderDto> GetAsync(GetOrderRequest request);
}
```

推导结果：

- 服务名：`Order`（去掉 `I` 和 `Service` 后缀）
- 方法名：`Get`（去掉 `Async`）
- gRPC 全名：`Order/Get`

## DTO 与 DataMember 规则

gRPC 请求/响应 DTO 使用 protobuf-net 的 `[DataContract]` / `[DataMember]` 契约：

```csharp
using System.Runtime.Serialization;

[DataContract]
public sealed class GetOrderRequest
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;
}

[DataContract]
public sealed class OrderDto
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public decimal Amount { get; set; }
}
```

规则：

- DTO 类型标 `[DataContract]`；
- 需要传输的成员标 `[DataMember(Order = n)]`；
- `Order` 从 1 开始，同一类型内唯一；
- `Order` 是 protobuf 字段号，发布后不能修改、复用或重排；
- 新增字段使用新的、更大的 `Order`；
- 删除字段时保留字段号，不要复用；
- 不需要传输的成员标 `[IgnoreDataMember]`；
- 嵌套类型也必须标 `[DataContract]` / `[DataMember]`；
- 建议使用公共 get/set 属性与公共无参构造函数；
- 集合建议使用 `List<T>` / `T[]` 等具体类型；
- 枚举显式赋值并保持数值稳定；
- 多态/继承需要显式 `[ProtoInclude]`。

兼容性：

- 新增可选字段兼容；
- 新增 `IsRequired = true` 字段可能破坏旧客户端；
- 修改字段号、类型或名称可能破坏旧客户端；
- 已发布字段号只能保留。

> HTTP-only DTO 不需要 `DataContract` / `DataMember`；这些标注只用于 gRPC/protobuf-net。

## 四种方法类型

### Unary

```csharp
[ExposeGrpc(Prefix = "sample.OrderService", Route = "Get", Method = GrpcMethod.Unary)]
Task<OrderDto> GetAsync(GetOrderRequest request, CancellationToken cancellationToken = default);
```

### Server streaming

```csharp
[ExposeGrpc(Prefix = "sample.OrderService", Route = "Watch", Method = GrpcMethod.ServerStreaming)]
IAsyncEnumerable<OrderDto> WatchAsync(WatchRequest request, CancellationToken cancellationToken = default);
```

### Client streaming

```csharp
[ExposeGrpc(Prefix = "sample.OrderService", Route = "Upload", Method = GrpcMethod.ClientStreaming)]
Task<UploadReply> UploadAsync(IAsyncEnumerable<OrderDto> request, CancellationToken cancellationToken = default);
```

### Duplex streaming

```csharp
[ExposeGrpc(Prefix = "sample.OrderService", Route = "Chat", Method = GrpcMethod.DuplexStreaming)]
IAsyncEnumerable<OrderDto> ChatAsync(IAsyncEnumerable<OrderDto> request, CancellationToken cancellationToken = default);
```

生成器会校验 `GrpcMethod` 与签名是否一致：

- 不一致：报告 `EUONIA001`，跳过该方法；
- 同一契约内重复 `Route`：跳过重复；
- 多个契约推导出相同服务名：报告 `EUONIA002` 并跳过重复契约。

## 认证

默认 `RequireAuthorization = true`，由 `GrpcMethodAuthorizationInterceptor` 在调用前执行。

- 无显式授权特性：要求已认证用户；
- `[Authorize]`：按策略/角色执行；
- `[AllowAnonymous]`：跳过授权；
- 多实现时只使用接口元数据，避免任一实现误放开。

```csharp
[Authorize(Roles = "admin")]
public sealed class OrderService : IOrderService
{
    // ...
}
```

接口/实现方法上的 `[Authorize]` / `[AllowAnonymous]` 由内置 `GrpcMethodAuthorizationInterceptor` 在调用前检查：根据生成的 gRPC 方法路径找到应用服务方法，使用 `IAuthorizationService` 执行策略/角色校验，失败返回 `PermissionDenied`。

## 未注册服务校验

`MapDynamicGrpcEndpoint` 会检查生成契约对应的应用服务是否已注册：

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
```

未注册且 `ThrowOnUnregisteredService = true`（默认）时，启动阶段抛出 `InvalidOperationException`；设为 `false` 时跳过该服务。

## Proto 导出

```csharp
var files = EndpointProtoExporter.Export(
    typeof(Program).Assembly,
    "obj/EuoniaEndpoint/Protos");
```

- 扫描程序集上的 `GrpcGeneratedMappingAttribute`；
- 读取生成类的 `ServiceContracts`；
- 使用 `protobuf-net.Grpc.Reflection.SchemaGenerator` 生成 schema；
- 文件名使用契约类型全名，例如 `Nerosoft.Euonia.Endpoint.Generated.IOrderServiceGrpcContract0.proto`。

导出的 proto 可用于其它语言客户端，但当前类型映射仍沿用 protobuf-net 内建 BCL schema：

- `Guid` / `DateTime` / `TimeSpan` / `DateOnly` / `TimeOnly` 尚未映射为自定义 wire 类型；
- 需要标准 `google.protobuf.Timestamp`、`Duration`、`string Guid` 等映射时，需要后续生成 wire-DTO 层。

## 扫描范围与插件

默认只扫描入口程序集。插件或 Application 程序集需要显式加入：

```csharp
builder.Services.AddDynamicGrpcEndpoint(
    options =>
    {
        options.EnableExposeApplicationServiceAsGrpc = true;
        options.ScanAssemblies = new[] { "MyApp.Application", "MyPlugin" };
        options.ScanEntryAssembly = false;
    });
```

注意：Source Generator 只处理**当前编译**中的接口。gRPC **没有运行时兜底**；插件程序集如果包含 `[ExposeGrpc]` 接口，插件项目本身必须引用 `Euonia.Endpoint.SourceGenerator`。未生成映射的程序集会在 `MapDynamicGrpcEndpoint` 记录 Warning。

## 注意事项 / 限制

- 未注册服务默认在映射阶段失败，适合快速暴露配置错误；
- 共享契约类型尚未接入自动 wire-DTO 映射；
- 生成器不处理带 `CallContext` 参数的方法（会跳过）；
- 生成器只处理接口方法，不扫描实现类方法。

其它边界：

- `EnableExposeApplicationServiceAsGrpc` 默认 `false`：关闭时既不执行生成的 `AddGenerated` 注册，也不映射服务，且不会报错。
- gRPC **没有运行时兜底**：插件或独立程序集里的 `[ExposeGrpc]` 接口必须由该程序集自己引用 `Euonia.Endpoint.SourceGenerator`，否则 `MapDynamicGrpcEndpoint` 只会记录 Warning。
- `RequireAuthorization` 默认 `true`，由 `GrpcMethodAuthorizationInterceptor` 执行；未配置认证方案时调用会返回 `PermissionDenied`。
- `Prefix` / `Route` 推导出的服务名在同一程序集内必须唯一，重复时报告 `EUONIA002` 并跳过后续契约。
- 导出的 proto 使用 protobuf-net 内建 BCL schema，`Guid` / `DateTime` / `TimeSpan` 等还不是标准 `google.protobuf.*` wire 类型。

## 完整示例

```csharp
using System.Runtime.Serialization;
using Nerosoft.Euonia.Endpoint;

[DataContract]
public sealed class GetOrderRequest
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;
}

[DataContract]
public sealed class OrderDto
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;
}

public interface IOrderService
{
    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Get", Method = GrpcMethod.Unary)]
    Task<OrderDto> GetAsync(GetOrderRequest request, CancellationToken cancellationToken = default);
}

public sealed class OrderService : IOrderService
{
    public Task<OrderDto> GetAsync(GetOrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = request.Id });
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDynamicGrpcEndpoint(builder.Configuration);

var app = builder.Build();
app.MapDynamicGrpcEndpoint();
app.Run();
```

## 相关包

- [`Euonia.Endpoint`](../Euonia.Endpoint/README.md) —— 暴露特性与共享契约
- [`Euonia.Endpoint.Http`](../Euonia.Endpoint.Http/README.md) —— HTTP 侧运行时
- [`Euonia.Endpoint.SourceGenerator`](../Euonia.Endpoint.SourceGenerator/README.md) —— 生成 gRPC 契约、适配器与注册标记
- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础类型与扩展方法

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。

## 常见问题与故障排查

### 启动抛 `gRPC service 'X' is not registered.`

**原因**：生成的 `AddGenerated` 在 `AddDynamicGrpcEndpoint` 里执行，发现契约对应的应用服务没注册（`Source/Euonia.Endpoint.SourceGenerator/GrpcGenerator.cs:287`）。
**解决**：补 `builder.Services.AddScoped<IOrderService, OrderService>()`；确认注册的是**接口**而不是实现类；确实要跳过时设 `ThrowOnUnregisteredService = false`。

### 端点缺失，日志只有「contains [ExposeGrpc] methods but no generated gRPC mapping」

**原因**：gRPC 没有运行时兜底，该程序集没生成映射（`Source/Euonia.Endpoint.Grpc/EndpointRouteBuilderExtensions.cs:68`）。
**解决**：在声明接口的项目里以 analyzer 形式引用 `Euonia.Endpoint.SourceGenerator`；插件程序集必须自己引用，宿主引用不生效。

### `grpcurl list` 返回空

**原因**：`EnableReflection` 默认 `false`，`MapGrpcReflectionService()` 不会被调用（`EndpointRouteBuilderExtensions.cs:74`）。
**解决**：开发/测试环境设 `options.EnableReflection = true`（或 `Euonia:Endpoint:Grpc:EnableReflection=true`）；生产环境按需开启并限制访问。

### 调用返回 `PermissionDenied`

**原因**：`RequireAuthorization` 默认 `true`，`GrpcMethodAuthorizationInterceptor` 对无显式授权特性的方法要求已认证用户。
**解决**：配置认证方案并在客户端带上凭据；确需匿名的方法标 `[AllowAnonymous]`，或按需关闭 `RequireAuthorization`。

### 构建只有 `EUONIA001` 警告且方法没暴露

**原因**：`GrpcMethod` 与实际签名不匹配（例如标了 `ServerStreaming` 却返回 `Task<T>`），生成器跳过该方法（`GrpcGenerator.cs:129`）。
**解决**：按「四种方法类型」表对齐签名；警告不阻断构建，容易被忽略，建议纳入 CI 检查。

## 最佳实践

- **显式写 `Prefix`**：服务全名是 wire 契约，靠接口名推导会在重命名接口时静默改变 gRPC 路径。
- **Reflection 只在非生产开启**：`EnableReflection = true` 会暴露服务描述，生产环境默认关闭并配合鉴权。
- **把 proto 导出纳入 CI**：`EndpointProtoExporter.Export(assembly, "obj/EuoniaEndpoint/Protos")` 生成快照并做 diff，能提前发现字段号破坏性变更。
- **生产保留 `ThrowOnUnregisteredService = true`**：让漏注册在启动阶段失败，而不是首个请求 404。
- **契约接口与实现分离**：接口放 Application 程序集（引用生成器），实现类由 DI 注册，避免生成器把实现细节写进契约。

## 版本与兼容

| 项 | 值 |
|---|---|
| 目标框架 | `net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`） |
| 包版本 | `2026.5.0`（仓库根 `project.props`） |
| 关键依赖 | `protobuf-net` 3.4.40、`protobuf-net.Grpc` / `protobuf-net.Grpc.AspNetCore` / `protobuf-net.Grpc.Reflection` 1.3.14、`Grpc.AspNetCore.Server.Reflection` |

- **wire 兼容由 `DataMember(Order)` 决定**：字段号是跨语言契约，升级本包不会改变序列化格式。
- **共享契约类型尚未接入自动 wire-DTO 映射**：`Guid` / `DateTime` / `TimeSpan` 仍用 protobuf-net 内建 BCL schema，升级到标准 `google.protobuf.*` 映射会按破坏性变更处理。
- **生成器与运行时需配套升级**：生成代码依赖 `GrpcMappings` / `GrpcMetadataRegistry` / `GrpcMethodAuthorizationDescriptor` 的签名，两者版本不一致可能出现编译或运行期错误。
