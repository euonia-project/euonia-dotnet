# Euonia.Endpoint.Grpc

基于 `protobuf-net.Grpc` 的 code-first gRPC 运行时，把 `Euonia.Endpoint` 标记的接口方法暴露为 gRPC 服务。

## 目标框架

- `net9.0`
- `net10.0`

依赖 `protobuf-net.Grpc` 1.3.14。

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

## 注册

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

## 选项

| 选项 | 默认值 | 说明 |
|---|---|---|
| `EnableExposeApplicationServiceAsGrpc` | `false` | 总开关 |
| `RequireAuthorization` | `true` | 对每个 gRPC 服务端点调用 `.RequireAuthorization()` |
| `EnableReflection` | `false` | 是否映射 gRPC reflection 服务 |
| `ThrowOnUnregisteredService` | `true` | 生成契约对应的应用服务未注册时抛异常；`false` 时跳过 |
| `ScanAssemblies` | 空 | 要扫描的程序集名称；配置后不再回退入口程序集 |
| `ScanEntryAssembly` | `true` | 未配置 `ScanAssemblies` 时是否扫描入口程序集 |

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

默认 `RequireAuthorization = true`。

实现类上的 `[Authorize]` / `[AllowAnonymous]` 会合并到 gRPC 服务端点元数据；多实现时只使用接口元数据，避免任一实现误放开：

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

## 限制

- 方法级 gRPC 授权尚未实现；
- 未注册服务默认在映射阶段失败，适合快速暴露配置错误；
- 共享契约类型尚未接入自动 wire-DTO 映射；
- 生成器不处理带 `CallContext` 参数的方法（会跳过）；
- 生成器只处理接口方法，不扫描实现类方法。

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

## 相关文档

- [Euonia.Endpoint](../Euonia.Endpoint/README.md)
- [Euonia.Endpoint.Http](../Euonia.Endpoint.Http/README.md)
- [Euonia.Endpoint.SourceGenerator](../Euonia.Endpoint.SourceGenerator/README.md)
