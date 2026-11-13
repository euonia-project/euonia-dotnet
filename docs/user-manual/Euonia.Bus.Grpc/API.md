# Euonia.Bus.Grpc API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 总线的 gRPC 远程传输：客户端无需生成桩代码即可做泛化一元调用，服务端由 `RemoteMessageService` 接收并执行处理器；同时提供拦截器、健康检查与自动服务发现。本页给出服务端/客户端端到端示例；完整选项见 [项目 README](../../../Source/Euonia.Bus.Grpc/README.md)。

- **NuGet 包**：`Euonia.Bus.Grpc`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Grpc`
- **完整选项**：[Source/Euonia.Bus.Grpc/README.md](../../../Source/Euonia.Bus.Grpc/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.Grpc/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.Http 用户手册](../Euonia.Bus.Http/API.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/API.md)
- [仓库根 README](../../../README.md)
