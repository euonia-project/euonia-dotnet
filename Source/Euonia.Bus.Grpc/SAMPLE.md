# Euonia.Bus.Grpc 全景示例

> 一份「gRPC 远程调用」完整示例：`ReplierService.Call` 一元方法承载信封、
> `RemoteReceiver` 按通道派发；客户端**不再依赖 Grpc.Tools 生成的桩**——运行时用
> `GrpcMethodFactory` 构造方法描述符，经 `CallInvoker` 发起调用，服务名/方法名可由
> `GrpcBusOptions` 动态指定（泛化调用）。同时覆盖并入的 gRPC 工具链
> （拦截器、反射、健康检查、`AddGrpcService`/`MapGrpcServices`）。

**示例代码与仓库内 `Euonia.Bus.Grpc.Tests`（11 例全部通过）同构，API 按源码逐一核对。**

---

## 0. 场景

同 [Euonia.Bus.Http/SAMPLE.md](../Euonia.Bus.Http/SAMPLE.md) 的«订单服务»双进程：

| 进程 | 角色 | 说明 |
|---|---|---|
| `Ordering.Server` | 服务端 | Kestrel HTTP/2，映射 `MapGrpcBusService`，注册 `count` 处理器 |
| `Ordering.Client` | 客户端 | `AddGrpcBus("grpc")` 指定 `Endpoint`，`bus.CallAsync` 远程取数 |

与 HTTP 版差异：明文通道必须 `Http2` 协议、契约来自 `protos`、客户端经泛化调用（动态方法名）。

---

## 1. 线协议契约（protos）

合同在 `Euonia.Bus.Grpc/protos/nerosoft/` 下，一次 `Call` 携带完整的 `IMessageEnvelope`
（`Data` 内存 JSON 信封，`Property` 存关联 id / 追踪 id / 自定义元数据）：

```proto
// nerorsoft/bus/message.proto
syntax = "proto3";
import "nerosoft/request.proto";
import "nerosoft/response.proto";
package nerorsoft.bus;

service ReplierService
{
  rpc Call(google.protobuf.GrpcRequest) returns (google.protobuf.GrpcResponse);
}
```

```proto
// nerorsoft/request.proto
message GrpcRequest
{
  string RequestId = 1;
  google.protobuf.StringValue Data = 2;          // JSON 信封
  map<string, string> Property = 3;              // CorrelationId / RequestTraceId / 自定义键
  int32 Page = 4;
  int32 Size = 5;
}
```

```proto
// nerorsoft/response.proto
message GrpcResponse
{
  string RequestId = 1;
  google.protobuf.StringValue Data = 2;          // RemoteReply<TResult> JSON
  map<string, string> Property = 3;
}
```

- 顺带提供 `decimal.proto`（`DecimalValue`）与 `json.proto`（`JsonValue`），
  需要 `decimal` / 任意 JSON 承载时可直接复用。
- `Euonia.Bus.Grpc.csproj` 的 Protobuf 项为 **`GrpcServices="Server"`**：只产出消息类型与
  `ReplierServiceBase` **服务端**基类；**不生成客户端桩**（客户端走 §4 泛化调用）。
- 生成命名空间为 `Nerorsoft.Bus`（proto `package nerorsoft.bus`）。

---

## 2. 服务端装配

```csharp
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Grpc;             // 拦截器 / 健康检查 / MapGrpcServices 工具
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);

// Kestrel 明文 gRPC 必须显式启用 HTTP/2（UseUrls 默认 HTTP/1.1）
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Loopback, 5081, endpoint => endpoint.Protocols = HttpProtocols.Http2);
});

builder.Services.AddSingleton<DefaultRequestContextAccessor>();
builder.Services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
builder.Services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
builder.Services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
builder.Services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

// ① gRPC 工具链：AddGrpc + MaxReceiveMessageSize/DetailedErrors + 内置拦截器 + 反射
builder.Services.AddGrpcService();

// ② 注册远程消息服务端（RemoteMessageService : ReplierServiceBase）——只需这一行
builder.Services.AddGrpcBusServer();

var app = builder.Build();
var configurator = app.Services.GetRequiredService<IConfigurator>();
configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

// 端点映射（构造时提前绑定 IHandlerContext → 后续 RegisterChannel 注册必定生效）
app.MapGrpcBusService();

// 可选：扫描入口程序集的 gRPC 服务（含 HealthService）一并映射
// app.MapGrpcServices();

configurator.RegisterChannel<CountRequest, int>("count",
    (request, _) => Task.FromResult(request.Start + 1));

await app.RunAsync();
```

消息类型（两端共享的 Contracts 程序集）：

```csharp
public sealed class CountRequest : IRequest<int>
{
    public int Start { get; set; }
}
```

> `AddGrpcBusServer()` 内部同样先 `AddEuoniaBus()`（`TryAdd*`），并单例注册
> `RemoteMessageService`。`MapGrpcBusService` 前的“提前构造 `IHandlerContext`”与 HTTP 版
> `MapBusEndpoint` 同理——都是为了让“启动阶段注册通道”不漏处理（见易错点 3）。

### 2.1 并入的 gRPC 工具链（原 `Euonia.Grpc`）

| 工具 | 说明 | 用法 |
|---|---|---|
| `AddGrpcService(this IServiceCollection, Action<GrpcServiceOptions>)` | `AddGrpc` + `MaxReceiveMessageSize=null` + `EnableDetailedErrors` + `ExceptionHandlingInterceptor` + `RequestTraceInterceptor` + 反射 | §2 `builder.Services.AddGrpcService()` |
| `ExceptionHandlingInterceptor` | 把服务端未处理异常统一转 `RpcException`（可配合 `IExceptionHandler` 自定义映射） | `AddGrpcService` 已自动挂载 |
| `RequestTraceInterceptor` | 从元数据/上下文写入请求追踪 | 同上 |
| `HealthService : HealthServiceImpl` | 内置健康项 `HealthCheck` = `Serving`，未随 `MapGrpcBusService` 挂起，需自行映射 | `app.MapGrpcServices()` 会经 `UseGrpcHealthCheck` 显式映射它，或 `endpoints.MapGrpcService<HealthService>()` |
| `MapGrpcServices(this IEndpointRouteBuilder, bool useHealthCheck = true)` | 扫描入口程序集、自动映射所有 `GrpcServiceBase` 子类，默认带 gRPC 健康检查 | §2 `app.MapGrpcServices()` |
| `UseGrpcEndpoints(this IApplicationBuilder, Action<IEndpointRouteBuilder>)` | `UseEndpoints` 便捷封装 + 为无 gRPC 的根路径回写提示 | 传统中间件管线用 |
| `IExceptionHandler` | 自定义异常 → `RpcException` 映射（实现后需自己注册） | `services.AddSingleton<IExceptionHandler, MyHandler>()` |
| `GrpcExtensions`（`GetResult`/`SetResult`/`GetProperty`/`SetProperty`） | `GrpcResponse`/`GrpcRequest` 的 `Data`/`Property` 便捷读写 | proto 模型直用 |

---

## 3. 客户端装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
services.AddLogging();
services.AddOptions();
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

// 一条调用即登记核心总线 + keyed ITransporter("grpc") + GrpcTransporter
services.AddGrpcBus("grpc", options =>
{
    options.Endpoint = "http://127.0.0.1:5081";      // 明文 h2c 用 http://
    // options.ServiceName = "nerorsoft.bus.ReplierService";   // 泛化调用：服务全名（默认即是）
    // options.MethodName  = "Call";                            // 泛化调用：一元方法名（默认即是）
});

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

var configurator = provider.GetRequiredService<IConfigurator>();
configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
```

`GrpcBusOptions` 选项表：

| 成员 | 默认值 | 说明 |
|---|---|---|
| `Enabled` | `true` | 功能开关 |
| `Name` | `"grpc"` | 传输器名（键控 `ITransporter` 的键） |
| `Endpoint` | 空 | 服务端地址；明文 h2c 用 `http://`，TLS 用 `https://` |
| `SerializerProvider` | `"SystemTestJson"` | 键控 `IMessageSerializer` 名 |
| `ServiceName` | `"nerorsoft.bus.ReplierService"` | 泛化调用服务全名（§4） |
| `MethodName` | `"Call"` | 泛化调用一元方法名（§4） |

---

## 4. 泛化调用（客户端不依赖生成桩）

原始 `ReplierServiceClient` 桩不再生成与使用。`GrpcTransporter` 在构造时用
`GrpcMethodFactory.CreateUnary(serviceName, methodName)` 构建运行时
`Method<GrpcRequest, GrpcResponse>`（手写 protobuf `Marshaller`），并缓存
`CallInvoker`；每次调用 `AsyncUnaryCall(_method, ...)` 执行。

```csharp
// 默认即走泛化调用：方法描述符 = nerorsoft.bus.ReplierService/Call，等价于生成的桩
var result = await bus.CallAsync<CountRequest, int>(
    new CountRequest { Start = 41 },
    new CallOptions { Channel = "count", CorrelationId = "corr-001" },
    cancellationToken);
```

**动态指定服务名 / 方法名**——同一客户端可调用任意协议兼容的一元方法：

```csharp
services.AddGrpcBus("grpc", options =>
{
    options.Endpoint   = "http://127.0.0.1:5081";
    options.ServiceName = "my.service.PricingService";    // 换成你的服务
    options.MethodName  = "Quote";                         // 换成你的方法
});
```

方法名不存在时返回 `RpcException(StatusCode.Unimplemented)`（`CallAsync_UnknownMethod_ReturnsUnimplemented`
用例已覆盖），可用它做服务能力探测。

**相关测试**（`Euonia.Bus.Grpc.Tests`）：
- `CallAsync_UsesConfiguredMethodDescriptor`：显式指定 `ServiceName`/`MethodName` 往返成功（41→42），
  证明描述符由选项驱动；
- `CallAsync_UnknownMethod_ReturnsUnimplemented`：`MethodName="Nope"` → `Unimplemented`。

---

## 5. 端到端

与 `GrpcServerHarness` + 客户端用例同构（h2c、端口 0 运行期绑定）：

```csharp
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Modularity;

// ---- 服务端 ----
var builder = WebApplication.CreateBuilder();
builder.Logging.AddConsole();
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Loopback, 0, l => l.Protocols = HttpProtocols.Http2);
});
var s = builder.Services;
s.AddSingleton<DefaultRequestContextAccessor>();
s.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
s.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
s.Configure<MessageBusOptions>(o => o.DefaultTransporter = "grpc");
s.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
s.AddGrpc();
s.AddGrpcBusServer();

await using var app = builder.Build();
var c = app.Services.GetRequiredService<IConfigurator>();
c.SetConvention(x => x.Add<DefaultMessageConvention>());
app.MapGrpcBusService();                              // ← 先映射
c.RegisterChannel<CountRequest, int>("count", (req, _) => Task.FromResult(req.Start + 1));
await app.StartAsync();

var endpoint = app.Urls.First();                       // http://127.0.0.1:PORT

// ---- 客户端（独立容器，模拟跨进程）----
var services = new ServiceCollection();
services.AddLogging(); services.AddOptions();
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.Configure<MessageBusOptions>(o => o.DefaultTransporter = "grpc");
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
services.AddGrpcBus("grpc", o => o.Endpoint = endpoint);
var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

var result = await bus.CallAsync<CountRequest, int>(
    new CountRequest { Start = 41 },
    new CallOptions { Channel = "count" },
    CancellationToken.None);

Console.WriteLine(result);                            // 42
await app.StopAsync();
```

---

## 6. 易错点清单

1. **明文 gRPC 必须 `HttpProtocols.Http2`**：`UseUrls`/默认 Kestrel 走 HTTP/1.1，
   客户端会收到 `HTTP_1_1_REQUIRED`（仓库踩过）。
2. 客户端 `Endpoint` 明文用 `http://`、TLS 用 `https://`，与服务端协议必须一致。
3. **先 `MapGrpcBusService()` 再 `RegisterChannel()`**：映射即构造 `IHandlerContext`；
   反向顺序会“运行时 No handler registered”。
4. `Channel` 三处对齐：服务端 `RegisterChannel` 名、`[Channel]`/类型推导名、`CallOptions.Channel`。
5. **`CallOptions` 命名冲突**：`Nerosoft.Euonia.Bus` 自带调度选项类 `CallOptions`（`Dispatching`），
   与 `Grpc.Core.CallOptions` 同名。凡在 `Nerosoft.Euonia.Bus.Grpc(.Tests)` 命名空间内写
   `Grpc.Core.CallOptions`，必须用 **`global::Grpc.Core.CallOptions`**——外层命名空间的
   `Grpc` 与总线的 `CallOptions` 都会遮蔽相对限定，未限定 `CallOptions` 也会解析到总线版本。
   （`GrpcTransporter.cs` 与测试即如此处理。）
6. Protobuf 项为 `GrpcServices="Server"`：程序集**不含** `ReplierServiceClient`，旧代码里的
   new `ReplierServiceClient(channel)` 编译不过，请改用 §4 泛化调用。
7. 用泛化调用做服务能力探测时注意：`MethodName` 错 / 服务不存在 → `RpcException(Unimplemented)`，
   不是静默失败。
8. 内置传输只支持 `CallAsync`；`Send` / `Publish` → `NotSupportedException`。
9. `AddGrpcBusServer()` 只注册 `RemoteMessageService`；`AddGrpc()` / `AddGrpcService()` 必须
   由宿主自行调用，`MapGrpcBusService` 不帮你开 gRPC。
10. `SerializerProvider` 必须是已注册键控序列化器（内置 `SystemTestJson` / `NewtonsoftJson`），
    配错抛 `InvalidOperationException`。

## 下一步

- 消息总线核心模式、远程契约与易错点见 [Euonia.Bus/SAMPLE.md](../Euonia.Bus/SAMPLE.md)；
- HTTP 版（POST `/bus/call`）见 [Euonia.Bus.Http/SAMPLE.md](../Euonia.Bus.Http/SAMPLE.md)。