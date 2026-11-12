# Euonia.Bus.Grpc

Euonia 总线的 gRPC 远程传输与集成：客户端无需生成桩代码即可进行泛化一元调用，服务端通过 `RemoteMessageService` 接收并执行处理器；同时提供 gRPC 拦截器、健康检查与自动服务发现。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Grpc`
- **NuGet 包**：`Euonia.Bus.Grpc`

## 安装

```bash
dotnet add package Euonia.Bus.Grpc
```

## 主要功能

- **泛化调用**：运行时构造 `Method<GrpcRequest, GrpcResponse>`，服务名/方法名由配置决定，无需生成客户端桩。
- **服务端接收**：`RemoteMessageService`（继承生成的 `ReplierService.ReplierServiceBase`）校验负载并交给 `IHandlerContext` / `RemoteReceiver`。
- **通道路由**：`GrpcBusOptions.Endpoints` 支持按通道精确匹配或通配符匹配不同服务地址。
- **共享线路协议**：与 HTTP 传输共用 `RemoteReply<TResult>`。
- **集成能力**：异常映射拦截器、请求追踪拦截器、健康检查与 `MapGrpcServices()` 自动发现。

## 快速开始

客户端：

```csharp
using Nerosoft.Euonia.Bus;

services.AddGrpcBus("grpc", options =>
{
    options.Endpoint = "https://localhost:5001";
});

services.Configure<MessageBusOptions>(options =>
{
    options.DefaultTransporter = "grpc";
});
```

按通道路由到不同服务：

```csharp
services.AddGrpcBus("grpc", options =>
{
    options.Endpoint = "https://gateway.local";
    options.Endpoints["orders-*"] = "https://orders.local";
    options.Endpoints["payments"] = "https://payments.local";
});
```

服务端：

```csharp
services.AddGrpc();
services.AddGrpcBusServer();

var app = builder.Build();
app.MapGrpcBusService();
```

> 明文 HTTP/2 环境需要为 Kestrel 配置 `HttpProtocols.Http2`；TLS 环境使用 `https://` 端点即可。

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Enabled` | `true` | 预留开关；`AddGrpcBus` 不读取该值，注册与否取决于是否调用该方法 |
| `Name` | `grpc` | 传输器名称 |
| `Endpoint` | `string.Empty` | 默认服务端地址 |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称 |
| `ServiceName` | `nerorsoft.bus.ReplierService` | 泛化调用的服务全名 |
| `MethodName` | `Call` | 一元方法名 |
| `Endpoints` | 空 | 通道 → 端点路由表 |
| `RequireAuthorization` | `true` | 服务端端点是否要求授权 |

## 配置绑定与环境变量

`AddGrpcBus`（客户端）与 `AddGrpcBusServer`（服务端）都会**自动绑定**配置节 `Euonia:Bus:Grpc`，
无需手工 `Bind`。该绑定以**最低优先级**注册为选项默认值：

- 宿主的 `services.Configure<GrpcBusOptions>(...)` 无论写在 `AddGrpcBus`/`AddGrpcBusServer` 之前还是之后，都会覆盖配置节的值；
- `AddGrpcBus(..., configure)` 的 `configure` 形参同样优先，因此客户端与服务端在同一进程共存时互不覆盖；
- 环境变量的优先级高于配置文件，例如：

```bash
Euonia__Bus__Grpc__Endpoint=https://env-grpc.example      # .NET 原生：双下划线表示层级
EUONIA_BUS_GRPC_ENDPOINT=https://env-grpc.example          # 仓库 alias：大写单下划线
Euonia__Bus__Grpc__RequireAuthorization=false
EUONIA_BUS_GRPC_REQUIREAUTHORIZATION=false
```

```jsonc
// appsettings.json
{
  "Euonia": {
    "Bus": {
      "Grpc": {
        "Enabled": true,
        "Endpoint": "https://localhost:5001",
        "ServiceName": "nerorsoft.bus.ReplierService",
        "MethodName": "Call",
        "RequireAuthorization": true
      }
    }
  }
}
```

> 配置节不存在时保持类型默认值；没有任何 `IConfiguration` 注册时自动绑定为 no-op（不报错）。

## 核心类型

| 类型 | 作用 |
|------|------|
| `AddGrpcBus(name, configure)` | 客户端注册键控 `ITransporter` |
| `AddGrpcBusServer()` | 服务端注册 `RemoteMessageService` |
| `AddGrpcService(...)` | 注册 gRPC、异常映射/追踪拦截器与反射 |
| `MapGrpcBusService()` | 映射 gRPC 总线服务端点 |
| `GrpcBusOptions` | 客户端与服务端配置 |
| `RemoteMessageService` | 服务端消息接收与回复 |
| `ExceptionHandlingInterceptor` | 将 .NET 异常映射为 gRPC 状态码 |
| `RequestTraceInterceptor` | 传播 `x-request-trace-id` |
| `MapGrpcServices()` / `UseGrpcEndpoints()` | 自动发现并映射入口程序集中的 gRPC 服务 |

## 使用示例

服务端：注册 gRPC、总线服务端与拦截器，再映射端点。

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddGrpcService();      // AddGrpc + 异常映射/追踪拦截器 + 反射
builder.Services.AddGrpcBusServer();    // RemoteMessageService
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention => convention.Add<DefaultMessageConvention>())
    .RegisterChannel(typeof(Program).Assembly));

var app = builder.Build();
app.MapGrpcBusService();                // 映射 nerorsoft.bus.ReplierService
app.Run();
```

需要自动发现入口程序集中的其它 gRPC 服务时，改用 `UseGrpcEndpoints()`：

```csharp
app.UseGrpcEndpoints(endpoints =>
{
    endpoints.MapGet("/healthz", () => "ok");
});
```

客户端：按通道路由到不同服务，并设置超时。

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;

services.AddGrpcBus("grpc", options =>
{
    options.Endpoint = "https://gateway.local";
    options.Endpoints["orders"] = "https://orders.local";
    options.Endpoints["payments"] = "https://payments.local";
});

services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
```

```csharp
using Nerosoft.Euonia.Bus;

// [Channel] 同时决定服务端处理器注册的通道与客户端路由键
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

public sealed class OrderClient(IBus bus)
{
    public Task<OrderStatus> GetStatusAsync(long orderId, CancellationToken cancellationToken)
        => bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = orderId })
              .WithTimeout(TimeSpan.FromSeconds(5))
              .ExecuteAsync(cancellationToken);
}
```

> 自定义 proto 服务时，需要同步设置 `ServiceName`（服务全名，不含方法）与 `MethodName`（一元方法名），默认值分别对应 `nerorsoft.bus.ReplierService` 与 `Call`。

## 注意事项 / 限制

1. **只支持请求-响应**：`GrpcTransporter` 的 `PublishAsync` / `SendAsync` 返回失败任务（`NotSupportedException`），只能走 `CallAsync`。
2. **服务端必须完成三步注册**：`AddGrpcService()`（或 `AddGrpc()`）、`AddGrpcBusServer()`、`MapGrpcBusService()`；缺任意一步都会得到 404/未实现。
3. **`Endpoint` 必填**：即使只用 `Endpoints` 做通道路由，`GrpcTransporter` 构造时仍要求默认 `Endpoint` 非空，否则抛 `InvalidOperationException`（`Source/Euonia.Bus.Grpc/GrpcTransporter.cs:51-54`）。
4. **明文 HTTP/2 需要显式配置**：Kestrel 需启用 `HttpProtocols.Http2`；TLS 环境直接用 `https://` 端点即可，客户端端点协议必须与服务端一致。
5. **授权默认开启**：`RequireAuthorization` 默认 `true`，服务端会调用 `RequireAuthorization()`；客户端需在信封中带 `Authorization`，否则请求被拒绝。
6. **端点解析顺序固定**：精确通道名 → 通配符（按声明顺序，首个匹配）→ `Endpoint` 默认端点；连接按端点缓存复用（`Source/Euonia.Bus.Grpc/GrpcBusOptions.cs:45-63`）。
7. **`Enabled` 不控制注册**：是否注册传输器取决于是否调用 `AddGrpcBus`，配置节里的 `Enabled` 目前不被读取。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.Http`](../Euonia.Bus.Http/README.md) —— 共享远程回复协议的 HTTP 传输
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
