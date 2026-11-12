# Euonia.Endpoint.Grpc 用户手册

基于 `protobuf-net.Grpc` 的 code-first gRPC 运行时：把 `Euonia.Endpoint` 标记的接口方法暴露为 gRPC 服务，支持四种方法类型、方法级授权与 proto 导出。本页是端到端教程与 API 速查；完整规则见 [项目 README](../../Source/Euonia.Endpoint.Grpc/README.md)。

- **NuGet 包**：`Euonia.Endpoint.Grpc`
- **目标框架**：`net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Grpc`（`AddDynamicGrpcEndpoint` 在 `Microsoft.Extensions.DependencyInjection`，`MapDynamicGrpcEndpoint` 在 `Microsoft.AspNetCore.Builder`）
- **关键依赖**：`protobuf-net` 3.4.40、`protobuf-net.Grpc` / `.AspNetCore` / `.Reflection` 1.3.14、`Grpc.AspNetCore.Server.Reflection`
- **完整参考**：[Source/Euonia.Endpoint.Grpc/README.md](../../Source/Euonia.Endpoint.Grpc/README.md)

## 教程

### 场景：订单查询（Unary）与订单变更流（Server streaming）

内部服务要通过 gRPC 查询单个订单，并订阅订单变更流。服务用 code-first 接口声明，宿主开启 reflection 便于 `grpcurl` 调试，最后导出 `.proto` 给其他语言客户端。

### 前置条件

.NET 10 SDK；ASP.NET Core 项目；本地安装 `grpcurl`（可选）；gRPC 需要 HTTP/2。

### 1. 安装

```bash
dotnet new web -n OrderGrpc && cd OrderGrpc
dotnet add package Euonia.Endpoint.Grpc
dotnet add package Euonia.Endpoint.SourceGenerator
```

### 2. 声明契约与 DTO

```csharp
using System.Runtime.Serialization;
using Nerosoft.Euonia.Endpoint;

public interface IOrderService
{
    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Get", Method = GrpcMethod.Unary)]
    Task<OrderDto> GetAsync(GetOrderRequest request, CancellationToken cancellationToken = default);

    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Watch", Method = GrpcMethod.ServerStreaming)]
    IAsyncEnumerable<OrderDto> WatchAsync(GetOrderRequest request, CancellationToken cancellationToken = default);
}

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

### 3. 实现与宿主接线

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public sealed class OrderService : IOrderService
{
    public Task<OrderDto> GetAsync(GetOrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = request.Id, Amount = 99.5m });

    public async IAsyncEnumerable<OrderDto> WatchAsync(GetOrderRequest request, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(100, cancellationToken);
            yield return new OrderDto { Id = $"{request.Id}-{i}" };
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000, listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDynamicGrpcEndpoint(options =>
{
    options.EnableExposeApplicationServiceAsGrpc = true;
    options.EnableReflection = true;                 // 仅开发/测试环境
    options.ScanAssemblies = new[] { "OrderGrpc" };
    options.ScanEntryAssembly = false;
});

var app = builder.Build();
app.MapDynamicGrpcEndpoint();
app.Run();
```

### 4. 配置与环境变量

```json
{
  "Euonia": {
    "Endpoint": {
      "Grpc": {
        "EnableExposeApplicationServiceAsGrpc": true,
        "EnableReflection": true,
        "ScanAssemblies": [ "OrderGrpc" ],
        "ScanEntryAssembly": false
      }
    }
  }
}
```

```bash
Euonia__Endpoint__Grpc__EnableExposeApplicationServiceAsGrpc=true     # .NET 原生
EUONIA_ENDPOINT_GRPC_ENABLEEXPOSEAPPLICATIONSERVICEASGRPC=true        # 仓库 alias
Euonia__Endpoint__Grpc__EnableReflection=true
EUONIA_ENDPOINT_GRPC_ENABLEREFLECTION=true
```

### 5. 运行与验证

```bash
dotnet run

# reflection 生效时可列出服务
grpcurl -plaintext localhost:5000 list

# Unary
grpcurl -plaintext -d '{"id":"A-1"}' localhost:5000 sample.OrderService/Get

# Server streaming
grpcurl -plaintext -d '{"id":"A-1"}' localhost:5000 sample.OrderService/Watch
```

导出 proto 给其他语言客户端：

```csharp
var files = EndpointProtoExporter.Export(typeof(Program).Assembly, "obj/EuoniaEndpoint/Protos");
```

### 常见失败点

- `gRPC service 'X' is not registered.`：启动时生成注册发现服务未注册；补 `AddScoped<IOrderService, OrderService>()`。
- 端点缺失且日志只有 `contains [ExposeGrpc] methods but no generated gRPC mapping`：该程序集没引用 `Euonia.Endpoint.SourceGenerator`（gRPC 无运行时兜底）。
- `grpcurl list` 为空：`EnableReflection` 默认 `false`。
- `PermissionDenied`：`RequireAuthorization` 默认 `true`，需认证或对方法标 `[AllowAnonymous]`。
- 构建只有 `EUONIA001`：`GrpcMethod` 与签名不匹配（如标 `ServerStreaming` 却返回 `Task<T>`），该方法被跳过。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddDynamicGrpcEndpoint(IConfiguration, params Assembly[])` | 绑定 `Euonia:Endpoint:Grpc` 并注册运行时与拦截器 | 开关打开时执行生成的 `AddGenerated` |
| `AddDynamicGrpcEndpoint(Action<GrpcExposureOptions>, params Assembly[])` | 回调注册 | 显式程序集优先 |
| `MapDynamicGrpcEndpoint()` | 映射生成的 gRPC 服务 | `EnableReflection` 时映射 reflection |
| `GrpcExposureOptions.EnableExposeApplicationServiceAsGrpc` | 总开关 | `false` |
| `GrpcExposureOptions.RequireAuthorization` | 方法级授权要求 | `true` |
| `GrpcExposureOptions.EnableReflection` | 是否映射 gRPC reflection | `false` |
| `GrpcExposureOptions.ThrowOnUnregisteredService` | 未注册服务时抛异常 | `true`（生成代码读取） |
| `GrpcExposureOptions.ScanAssemblies` / `ScanEntryAssembly` | 扫描范围 | 空数组 / `true` |
| `GrpcMetadataRegistry` | 方法级授权元数据：`Build` / `GetServiceMetadata` / `GetMethodMetadata` | 由注册流程构建 |
| `GrpcMethodAuthorizationInterceptor` | 内置拦截器，执行方法级授权 | 失败返回 `PermissionDenied` |
| `GrpcMethodAuthorizationDescriptor` | 描述符：`MethodPath` / `ServiceType` / `MethodName` | 生成器写入 |
| `EndpointProtoExporter.Export(Assembly, string)` | 导出 `.proto` | 返回文件路径列表 |
| `GrpcGeneratedMappingAttribute` | 程序集级标记，指向 `GrpcMappings` | 生成器写入 |

## 相关文档

- [项目 README](../../Source/Euonia.Endpoint.Grpc/README.md) —— 四种方法类型、DTO 规则、认证与限制
- [Euonia.Endpoint 用户手册](Euonia.Endpoint.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.SourceGenerator 用户手册](Euonia.Endpoint.SourceGenerator.md) —— 生成契约与适配器
- [Euonia.Endpoint.Http 用户手册](Euonia.Endpoint.Http.md) —— HTTP 侧运行时
- [仓库根 README](../../README.md)
