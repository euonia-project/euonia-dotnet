# Euonia.Bus.Http API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

Euonia 总线的 HTTP 远程传输：客户端用 HTTP POST 执行 `CallAsync`，服务端用 `MapBusEndpoint()` 接收消息、执行处理器并回传结果。本页给出两个 ASP.NET Core 服务的端到端接线；完整选项见 [项目 README](../../../Source/Euonia.Bus.Http/README.md)。

- **NuGet 包**：`Euonia.Bus.Http`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Http`
- **完整选项**：[Source/Euonia.Bus.Http/README.md](../../../Source/Euonia.Bus.Http/README.md)

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddHttpBus(name, configure)` | 注册 `HttpTransporter` 与键控 `ITransporter` | `name` 默认 `http` |
| `MapBusEndpoint()` | 映射服务端请求端点 | 路由取 `Route`，默认 `/bus/call` |
| `HttpBusOptions` | 端点、路由、序列化、超时、授权 | 见下表 |
| `HttpBusOptions.Endpoint` | 默认服务端地址 | 默认 `null` |
| `HttpBusOptions.Route` | 端点路由 | 默认 `/bus/call` |
| `HttpBusOptions.Endpoints` | 通道 → 端点路由表 | 精确键优先，其次通配符 |
| `HttpBusOptions.RequestTimeout` | 传输器级超时 | 默认 `null` |
| `HttpBusOptions.RequireAuthorization` | 端点是否要求授权 | 默认 `true` |
| `HttpBusOptions.SerializerProvider` | 序列化器名称 | 默认 `SystemTestJson` |
| `HttpBusOptions.MessageHandlerFactory` | 自定义 `HttpMessageHandler` | 默认 `null` |
| `RemoteReply<TResult>` / `RemoteError` / `RemoteReceiver` | 线路协议、错误还原与服务端接收 | 均定义于 `Euonia.Bus` |

> `HttpTransporter` 与 `HttpEndpointPattern` 是 `internal`，请通过 `AddHttpBus` 使用。

### 配置项

配置节 `Euonia:Bus:Http`；旧节 `HttpBus` 仍绑定，但新节优先（`Source/Euonia.Bus.Http/HttpBusOptions.cs:12,20`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `Enabled` | `bool` | `true`（预留，不控制注册） |
| `Name` | `string` | `http` |
| `Endpoint` | `string?` | `null` |
| `Route` | `string` | `/bus/call` |
| `SerializerProvider` | `string` | `SystemTestJson` |
| `RequestTimeout` | `TimeSpan?` | `null` |
| `RequireAuthorization` | `bool` | `true` |
| `Endpoints` | `Dictionary<string,string>` | 空 |

```bash
Euonia__Bus__Http__Endpoint=http://localhost:5100     # .NET 原生：双下划线表示层级
EUONIA_BUS_HTTP_ENDPOINT=http://localhost:5100        # 仓库 alias：大写单下划线
Euonia__Bus__Http__RequireAuthorization=false
EUONIA_BUS_HTTP_REQUIREAUTHORIZATION=false
```

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.Http/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.Grpc 用户手册](../Euonia.Bus.Grpc/API.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/API.md)
- [仓库根 README](../../../README.md)
