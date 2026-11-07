# Euonia.Bus.Http

Euonia 总线的 HTTP 远程传输：客户端通过 HTTP POST 执行请求-响应调用（`CallAsync`），服务端端点接收消息、执行处理器并回传结果。与 gRPC 传输共用 `RemoteReply<TResult>` 线路协议。

HTTP remote transport for Euonia Bus: client-side `CallAsync` over HTTP POST and a server endpoint that executes handlers and returns results, sharing the `RemoteReply<TResult>` wire protocol with the gRPC transport.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Http`
- **NuGet 包**：`Euonia.Bus.Http`

## 安装

```bash
dotnet add package Euonia.Bus.Http
```

## 主要功能

- **请求-响应**：客户端 `CallAsync` → HTTP POST，服务端 `MapBusEndpoint()` 处理并回传。
- **仅支持远程调用**：`Send` / `Publish` 会抛出 `NotSupportedException`，避免误用语义。
- **错误还原**：`RemoteReply<T>` 与 `RemoteError` 在客户端还原原始异常类型，失败时回退为 `MessageDeliverException`。
- **请求头透传**：`x-*` 消息头随请求传递。
- **授权与超时**：`RequireAuthorization`、`RequestTimeout` 与自定义 `MessageHandlerFactory`。

## 快速开始

客户端：

```csharp
using Nerosoft.Euonia.Bus;

services.AddHttpBus("http", options =>
{
    options.Endpoint = "https://service.example.com";
    options.RequestTimeout = TimeSpan.FromSeconds(30);
});

services.Configure<MessageBusOptions>(options =>
{
    options.DefaultTransporter = "http";
});
```

服务端（ASP.NET Core）：

```csharp
services.AddEuoniaBus();

var app = builder.Build();
app.MapBusEndpoint();   // POST /bus/call
```

调用远程处理器：

```csharp
var status = await bus.CallAsync<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = 42 });
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Enabled` | `true` | 是否启用 |
| `Name` | `http` | 传输器名称 |
| `Endpoint` | `null` | 服务端基础地址 |
| `Route` | `/bus/call` | 服务端端点路由 |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称 |
| `RequestTimeout` | `null` | 请求超时 |
| `RequireAuthorization` | `true` | 服务端端点是否要求授权 |
| `MessageHandlerFactory` | `null` | 自定义 `HttpMessageHandler` 工厂 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `AddHttpBus(name, configure)` | 注册 HTTP 传输器与键控 `ITransporter` |
| `MapBusEndpoint()` | 映射服务端请求端点 |
| `HttpBusOptions` | 传输配置 |
| `HttpTransporter` | `ITransporter` 实现（仅调用） |
| `RemoteReply<TResult>` / `RemoteError` | 线路协议与错误还原（定义于 `Euonia.Bus`） |
| `RemoteReceiver` | 服务端接收、执行与回复（定义于 `Euonia.Bus`） |

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— 共享协议的 gRPC 传输

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
