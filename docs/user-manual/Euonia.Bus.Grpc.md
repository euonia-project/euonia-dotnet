# Euonia.Bus.Grpc 用户手册

Euonia 总线的 gRPC 远程传输：客户端无需生成桩代码即可做泛化一元调用，服务端由 `RemoteMessageService` 接收并执行处理器；同时提供拦截器、健康检查与自动服务发现。本页给出服务端/客户端端到端示例；完整选项见 [项目 README](../../Source/Euonia.Bus.Grpc/README.md)。

- **NuGet 包**：`Euonia.Bus.Grpc`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Grpc`
- **完整选项**：[Source/Euonia.Bus.Grpc/README.md](../../Source/Euonia.Bus.Grpc/README.md)

## 教程

### 场景：网关通过 gRPC 调用订单服务

订单服务注册 gRPC、总线服务端与处理器，映射 `nerorsoft.bus.ReplierService`；网关用 `AddGrpcBus` 按通道 `orders` 调用 `GetOrderStatus`，返回强类型结果。

### 前置条件

- .NET SDK 可构建 `net10.0`/`net11.0`；
- 两个 ASP.NET Core 项目（Orders、Gateway）共享消息契约；
- 本地用明文 h2c 时，Kestrel 必须启用 HTTP/2。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.Grpc
```

### 2. 订单服务：gRPC + 总线服务端

```csharp
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5101, listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddEuoniaBus();
builder.Services.AddGrpcService();      // AddGrpc + 异常映射/追踪拦截器 + 反射
builder.Services.AddGrpcBusServer();    // RemoteMessageService
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .RegisterChannel(typeof(Program).Assembly));

var app = builder.Build();
app.MapGrpcBusService();                // 映射 nerorsoft.bus.ReplierService
app.Run();
```

本地演示需在订单服务设置 `Euonia:Bus:Grpc:RequireAuthorization=false`；生产保持默认 `true`。

### 3. 共享消息契约与处理器

```csharp
using Nerosoft.Euonia.Bus;

[Channel("orders")]
public sealed class GetOrderStatus : IRequest<OrderStatus>
{
    public long OrderId { get; init; }
}

public sealed class OrderStatus
{
    public long OrderId { get; init; }
    public string State { get; init; } = null!;
}

public sealed class GetOrderStatusHandler : IHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> HandleAsync(GetOrderStatus message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderStatus { OrderId = message.OrderId, State = "Shipped" });
}
```

### 4. 网关：泛化调用

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "grpc",
      "Grpc": {
        "Endpoint": "http://localhost:5101",
        "ServiceName": "nerorsoft.bus.ReplierService",
        "MethodName": "Call",
        "RequireAuthorization": false
      }
    }
  }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddGrpcBus("grpc");

var app = builder.Build();

app.MapGet("/orders/{id:long}/status", async (long id, IBus bus, CancellationToken cancellationToken) =>
{
    var status = await bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = id })
                          .WithTimeout(TimeSpan.FromSeconds(5))
                          .ExecuteAsync(cancellationToken);

    return Results.Ok(status);
});

app.Run();
```

### 5. 运行与验证

```bash
# 终端 1：订单服务（h2c，http://localhost:5101）
dotnet run --project Orders

# 终端 2：网关
dotnet run --project Gateway

# 终端 3
curl http://localhost:5200/orders/42/status
# 期望：{"orderId":42,"state":"Shipped"}
```

### 常见失败点

- **`Unimplemented`/404**：服务端缺 `AddGrpcBusServer()` 或 `MapGrpcBusService()`；或 `ServiceName`/`MethodName` 不匹配。
- **HTTP/2 协议错误**：Kestrel 未启用 `HttpProtocols.Http2`，或客户端 `http://` 与服务端 TLS 不一致。
- **`InvalidOperationException`（Endpoint 未配置）**：即使只用 `Endpoints` 路由，默认 `Endpoint` 也必须非空。
- **401/403**：`RequireAuthorization` 默认 `true`；本地演示在两端关闭。

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddGrpcBus(name, configure)` | 客户端注册键控 `ITransporter` | `name` 默认 `grpc` |
| `AddGrpcBusServer()` | 注册 `RemoteMessageService` | 需配合 `MapGrpcBusService()` |
| `AddGrpcService(configure)` | `AddGrpc` + 拦截器 + 反射 | `GrpcServiceOptions` |
| `MapGrpcBusService()` | 映射 gRPC 总线服务端点 | 按 `RequireAuthorization` 授权 |
| `UseGrpcEndpoints(configure)` | 映射入口程序集 gRPC 服务 + 根提示 | 可选端点配置委托 |
| `MapGrpcServices(useHealthCheck)` | 自动发现入口程序集 gRPC 服务 | 默认启用健康检查 |
| `UseGrpcHealthCheck()` | 映射 gRPC 健康检查 | `MapGrpcService<HealthService>` |
| `GrpcBusOptions` | 端点、服务名、方法名、授权 | 见下表 |
| `RemoteMessageService` | 服务端接收与回复 | 继承 `ReplierService.ReplierServiceBase` |
| `RequestTraceInterceptor` | 传播 `x-request-trace-id` | 命名空间 `Nerosoft.Euonia.Grpc` |
| `IExceptionHandler` | 异常到 gRPC 状态码的映射契约 | 命名空间 `Nerosoft.Euonia.Grpc` |
| `HealthService` | gRPC 健康检查实现 | 命名空间 `Nerosoft.Euonia.Grpc` |

> `GrpcTransporter`、`GrpcEndpointPattern` 是 `internal`，请通过 `AddGrpcBus` 使用。

### 配置项

配置节 `Euonia:Bus:Grpc`（`Source/Euonia.Bus.Grpc/GrpcBusOptions.cs:12`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `Enabled` | `bool` | `true`（预留，不控制注册） |
| `Name` | `string` | `grpc` |
| `Endpoint` | `string` | `string.Empty`（必填） |
| `SerializerProvider` | `string` | `SystemTestJson` |
| `ServiceName` | `string` | `nerorsoft.bus.ReplierService` |
| `MethodName` | `string` | `Call` |
| `Endpoints` | `Dictionary<string,string>` | 空 |
| `RequireAuthorization` | `bool` | `true` |

```bash
Euonia__Bus__Grpc__Endpoint=http://localhost:5101      # .NET 原生：双下划线表示层级
EUONIA_BUS_GRPC_ENDPOINT=http://localhost:5101          # 仓库 alias：大写单下划线
Euonia__Bus__Grpc__RequireAuthorization=false
EUONIA_BUS_GRPC_REQUIREAUTHORIZATION=false
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus.Grpc/README.md)
- [Euonia.Bus 用户手册](Euonia.Bus.md)
- [Euonia.Bus.Http 用户手册](Euonia.Bus.Http.md)
- [Euonia.Bus.HealthChecks 用户手册](Euonia.Bus.HealthChecks.md)
- [仓库根 README](../../README.md)
