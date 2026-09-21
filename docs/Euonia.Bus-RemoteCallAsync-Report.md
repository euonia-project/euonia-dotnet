# Euonia.Bus 远程调用扩展报告（HTTP / gRPC）

## 概述
为使 Euonia.Bus 的请求-响应调用（`CallAsync`）支持**跨进程/跨服务**的远程调用，本次新增了基于
**HTTP** 与 **gRPC** 两种传输的远程调用能力：客户端经 `ITransporter` 发起调用，服务端以接收器
（receiver）处理消息并回传结果。单次调用链路为——

```
IBus.CallAsync
  → MessageBus → ITransporter（HttpTransporter / GrpcTransporter）  [客户端]
  → 序列化 RoutedMessage 信封 → 传输（HTTP POST / gRPC unary Call）
  → 端点（MapBusEndpoint / MapGrpcBusService） → RemoteReceiver → IHandlerContext
  → IHandler / RegisterChannel 委托处理 → RemoteReply 响应
  → 反序列化并还原（成功结果 / 原始异常类型）
```

验证：`Euonia.slnx` 与 `Euonia.Test.slnx` 均 **0 错误 0 警告**；
`Euonia.Bus.Tests` **67/67**、`Euonia.Bus.InMemory.Tests` **10/10**、
`Euonia.Bus.Http.Tests` **10/10**（新增）、`Euonia.Bus.Grpc.Tests` **7/7**（新增）全部通过。

---

## 一、共享远程协议（`Source/Euonia.Bus/Remote/`）

HTTP 与 gRPC 两条链路共用同一套线上协议与接收逻辑，新增三个类型：

- **`RemoteError`**：`{ Type, Message, StackTrace }`。`Create(Exception)` 生成，
  `ToException()` 在客户端还原异常——优先用 `Type.GetType` + AppDomain 程序集扫描解析原类型
  （含 `(string message)` 构造器），解析失败则回退为 `MessageDeliverException`。
- **`RemoteReply<TResult>`**：`{ IsSuccess, Result, Error }`，成功/失败两套构造。
- **`RemoteReceiver`**：静态 `ReceiveAsync(IMessageSerializer, IHandlerContext, string payload, CancellationToken)`。
  流程：解析负载取 `typeName`/`TypeName` → `Type.GetType` 解析消息类型 → `DeserializeEnvelope`
  反序列化信封 → `new MessageContext(envelope)` → 订阅 `Responded`/`Failed`/`Completed` 事件 →
  `IHandlerContext.HandleAsync(channel, payload, context, ct)` → 成功后 `context.Response(result)`、
  异常时 `context.Failure(exception)`、最终 `context.Complete(null)`。
  调用方取消（`OperationCanceledException`）原样上抛；其余任何异常均转为失败响应，
  不会让远端端点崩溃。

---

## 二、HTTP 传输（`Source/Euonia.Bus.Http`）

新增项目 `Euonia.Bus.Http`（`FrameworkReference: Microsoft.AspNetCore.App`，仅引用 `Euonia.Bus`）。

| 类型 | 说明 |
| --- | --- |
| `HttpBusOptions` | `Name`（默认 `"http"`）、`Endpoint`、`Route`（默认 `/bus/call`）、`SerializerProvider`（默认 `"SystemTestJson"`）、`RequestTimeout`、`MessageHandlerFactory` |
| `HttpTransporter`（internal, `IDisposable`） | `CallAsync`：HTTP POST 序列化信封，携带 `x-*` 消息头，解析 `RemoteReply<TResponse>`；失败抛原始异常；`Send/Publish` → `NotSupportedException` |
| `ServiceCollectionExtensions.AddHttpBus(name, configure)` | 注册 Options + `HttpTransporter` 单例 + keyed `ITransporter(name)`（带去重保护） |
| `BusEndpointExtensions.MapBusEndpoint(this IEndpointRouteBuilder)` | `MapPost` 路由（读自 `HttpBusOptions.Route`），解析 keyed 序列化器 + `IHandlerContext`，调用 `RemoteReceiver` 并回写 JSON |

`HttpTransporter` 使用 `TryAddWithoutValidation` 透传 `MessageHeaders`（Channel/MessageType/
CorrelationId/ConversationId/RequestTraceId/MessageId/Authorization）。

---

## 三、gRPC 传输（`Source/Euonia.Grpc` + `Source/Euonia.Bus.Grpc`）

### 服务定义（`Euonia.Grpc/protos/nerosoft/message.proto`，新增）
```proto
syntax = "proto3";
package nerorsoft.bus;

service ReplierService {
  rpc Call(google.protobuf.GrpcRequest) returns (google.protobuf.GrpcResponse);
}
```
- `GrpcRequest`/`GrpcResponse` 含 `RequestId`、`Data` 与 `map<string, string> Property`；
  生成命名空间 `Nerorsoft.Bus`。
- `Euonia.Grpc.csproj` 的 Protobuf 项由 `GrpcServices="None"` 改为
  `GrpcServices="Server,Client"`（同时产出 Base 服务端基类与客户端）。
- `Directory.Packages.props` 新增 `Grpc.Net.Client`（`$(GrpcAspNetCoreVersion)` = 2.83.0）。已验证：
  protobuf 3.36 将 `google.protobuf.StringValue` 映射为原生 `string`，`Data` 直接承载 JSON 文本。

### 传输与端点（`Euonia.Bus.Grpc`，新增项目）
| 类型 | 说明 |
| --- | --- |
| `GrpcBusOptions` | `Name`（默认 `"grpc"`）、`Endpoint`、`SerializerProvider`（默认 `"SystemTestJson"`） |
| `GrpcTransporter`（internal, `IDisposable`） | `GrpcChannel.ForAddress(Endpoint)` + `ReplierServiceClient`；`Data = serializer.Serialize(message)`；属性写入 `RequestId` 与 `MessageHeaders`（空串跳过，兼容无上下文调用）；`Send/Publish` → `NotSupportedException` |
| `RemoteMessageService : ReplierService.ReplierServiceBase` | `Call` 覆写：空 `Data` → `RpcException(InvalidArgument)`；否则 `RemoteReceiver.ReceiveAsync` 并回传 `GrpcResponse { RequestId, Data = reply }` |
| `ServiceCollectionExtensions` | `AddGrpcBus(name, configure)` 客户端注册；`AddGrpcBusServer()` 注册 `RemoteMessageService` 单例（读自 `IHandlerContext`/keyed 序列化器） |
| `GrpcEndpointExtensions.MapGrpcBusService(this IEndpointRouteBuilder)` | `MapGrpcService<RemoteMessageService>()` |

> `RemoteMessageService` 借用 gRPC 的服务器 `IHttpContextAccessor.HttpContext.RequestServices`
> 解析 keyed 序列化器与 `IHandlerContext`。客户端经 `AddGrpcBus` 时既注册 keyed `ITransporter`，
> 也直接注册 `GrpcTransporter`（internal 单例），供协议级测试复用。

---

## 四、测试

### `Tests/Euonia.Bus.Http.Tests`（10 例，全部通过）
- 桩服务器端 `FakeBusServerHandler`/`FailureBusServerHandler` + `StubHandlerContext`
  模拟远端（返回结果 / 抛 `InvalidOperationException` / 延迟），`HttpTestFactory` 组装传输器；
- 协议用例：结果 42 往返、远端异常还原为原始类型、`x-*` 消息头断言、非 2xx → `MessageDeliverException`、
  `Send/Publish` → `NotSupportedException`、`typeName` 缺失 → 失败响应、取消穿透；
- 端到端用例：真实 `WebApplication` + `MapBusEndpoint` + 完整 `IBus` 调用栈，
  验证成功路径与处理异常上抛（宿主启动前需先解析 `IHandlerContext`，
  `DefaultHandlerContext` 构造时才订阅信道注册事件，注册委托才会生效）。

### `Tests/Euonia.Bus.Grpc.Tests`（7 例，全部通过）
- `GrpcServerHarness`：`WebApplication` + `UseKestrel(Listen(Loopback, 0, Http2))`（明文 h2c，
  端口 0 运行时绑定）+ `AddGrpc`/`AddGrpcBusServer`/`MapGrpcBusService` + 信道注册；
- 用例：传输器往返、异常还原、`IBus` 端到端（成功 + 抛错）、空 `Data` → `InvalidArgument`、
  `Send/Publish` → `NotSupportedException`。

> 关键经验：Kestrel 明文 gRPC 必须显式启用 `HttpProtocols.Http2`（`UseUrls` 默认 HTTP/1.1，
> 否则客户端收到 `HTTP_1_1_REQUIRED`）。

---

## 五、增强（后续追加）

在基础版本之上继续完善与增强，全部通过回归：

1. **消除处理器注册时序陷阱**：`MapBusEndpoint()` / `MapGrpcBusService()` 在映射时即通过
   `endpoints.ServiceProvider` 提前构造 `IHandlerContext`——`DefaultHandlerContext` 仅在构造时订阅
   `ChannelRegistered` 事件，此前用户在（模块初始化等）应用启动阶段注册的渠道会被静默遗漏
   （运行时报“No handler registered”）。现在**先映射端点、后注册渠道**的常规顺序（模块初始化）
   必定生效，无需手动先解析 `IHandlerContext`。
   （`Source/Euonia.Bus.Http/BusEndpointExtensions.cs`、`Source/Euonia.Bus.Grpc/GrpcEndpointExtensions.cs`）
2. **注册自足（少样板代码）**：
   - `AddEuoniaBus()` 现在同时以 `TryAddKeyedSingleton` 注册内置键控序列化器
     （`SystemTestJson` / `NewtonsoftJson`），`MessageBusModule` 中的重复注册已移除；
   - `AddHttpBus` / `AddGrpcBus` / `AddGrpcBusServer` 内部先调用 `AddEuoniaBus()`（`TryAdd*`，
     重复调用安全），用户**无需**再显式调用 `AddEuoniaBus()` 或注册序列化器。
   - 测试同步精简：协议/端到端宿主依赖自我注册，并新增 `AddHttpBus_SelfRegistersCoreServices`、
     `AddGrpcBus_SelfRegistersCoreServices`、`AddGrpcBusServer_SelfRegistersCoreServices` 3 个事实用例。
   - 说明：`IBus`（客户端）解析仍依赖框架宿主提供的请求上下文访问器（
     `DefaultRequestContextAccessor` / `DelegateRequestContextAccessor` / `IServiceAccessor` / `IRequestContextAccessor`），
     与其余 Euonia 传输一致，不在传输注册内隐式提供。

---

## 六、验证结果

| 项目 | 结果 |
| --- | --- |
| `dotnet build Euonia.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误 0 警告 |
| `Euonia.Bus.Tests` | 67/67（回归） |
| `Euonia.Bus.InMemory.Tests` | 10/10（回归） |
| `Euonia.Bus.RabbitMq.Tests` | 10/10（回归） |
| `Euonia.Bus.Http.Tests` | 11/11（新增 3：自我注册） |
| `Euonia.Bus.Grpc.Tests` | 9/9（新增 4：自我注册） |
| 新增项目 | `Euonia.Bus.Http`、`Euonia.Bus.Grpc`（同时纳入 `Euonia.slnx` 与 `Euonia.Test.slnx`） |