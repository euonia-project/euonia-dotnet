# Euonia.Bus.Grpc

Euonia 总线的 gRPC 远程传输与集成：客户端无需生成桩代码即可进行泛化一元调用，服务端通过 `RemoteMessageService` 接收并执行处理器；同时提供 gRPC 拦截器、健康检查与自动服务发现。

gRPC remote transport and integration for Euonia Bus: generic unary client invocation without generated stubs, a server-side `RemoteMessageService` that executes handlers, plus gRPC interceptors, health checks and automatic service discovery.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Grpc`
- **NuGet 包**：`Euonia.Bus.Grpc`

## 安装

```bash
dotnet add package Euonia.Bus.Grpc
```

## 主要功能

- **泛化调用**：运行时构造 `Method<GrpcRequest, GrpcResponse>`，服务名/方法名由配置决定，无需生成客户端桩。
- **服务端接收**：`RemoteMessageService`（`ReplierService` 基类）校验负载并交给 `RemoteReceiver`。
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
| `Enabled` | `true` | 是否启用 |
| `Name` | `grpc` | 传输器名称 |
| `Endpoint` | `string.Empty` | 默认服务端地址 |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称 |
| `ServiceName` | `nerorsoft.bus.ReplierService` | 泛化调用的服务全名 |
| `MethodName` | `Call` | 一元方法名 |
| `Endpoints` | 空 | 通道 → 端点路由表 |
| `RequireAuthorization` | `true` | 服务端端点是否要求授权 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `AddGrpcBus(name, configure)` | 客户端注册键控 `ITransporter` |
| `AddGrpcBusServer()` | 服务端注册 `RemoteMessageService` |
| `MapGrpcBusService()` | 映射 gRPC 总线服务端点 |
| `GrpcBusOptions` | 客户端与服务端配置 |
| `RemoteMessageService` | 服务端消息接收与回复 |
| `ExceptionHandlingInterceptor` | 将 .NET 异常映射为 gRPC 状态码 |
| `RequestTraceInterceptor` | 传播 `x-request-trace-id` |
| `MapGrpcServices()` / `UseGrpcEndpoints()` | 自动发现并映射入口程序集中的 gRPC 服务 |

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.Http`](../Euonia.Bus.Http/README.md) —— 共享远程回复协议的 HTTP 传输
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
