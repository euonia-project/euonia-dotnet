# Euonia.Bus.Http 全景示例

> 一份「真正跑在两个进程上」的 HTTP 远程调用示例：服务端（`MapBusEndpoint`）接收信封、
> `RemoteReceiver` 还原消息并按通道派发；客户端经 keyed `ITransporter`（`"http"`）把
> `IBus.CallAsync` 投递到远端并还原强类型结果与异常。
> 线协议契约见 [Euonia.Bus/SAMPLE.md](../Euonia.Bus/SAMPLE.md) 的 §5。

**示例代码与仓库内 `Euonia.Bus.Http.Tests` 端到端用例同构、API 按源码逐一核对，可直接运行。**

---

## 0. 场景

«订单服务»拆成两个进程：

| 进程 | 角色 | 说明 |
|---|---|---|
| `Ordering.Server` | 服务端 | 托管 `MapBusEndpoint`（POST `/bus/call`），注册 `count`/`orders` 通道处理器 |
| `Ordering.Client` | 客户端 | `AddHttpBus("http")`，`bus.CallAsync<CountRequest, int>` 远程取数 |

同一个消息类型 / 处理器在**服务端**注册，客户端只声明消息类型本身（传输层不共享程序集即可）。

---

## 1. 服务端装配

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);

// IBus 依赖的请求上下文访问器（基于 Modularity 的应用由框架提供，这里手动补齐）
builder.Services.AddSingleton<DefaultRequestContextAccessor>();
builder.Services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
builder.Services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
builder.Services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

// 默认传输器：没分配策略的消息都走 http（供客户端侧路由；服务端用于 IBus 自举）
builder.Services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "http");

// 一条调用即登记核心总线 + keyed ITransporter("http") + HttpTransporter（重复调用安全）
builder.Services.AddHttpBus("http");

// 可配置化绑定 HttpBusOptions（与 services.Configure 等价）
// builder.Services.Configure<HttpBusOptions>(builder.Configuration.GetSection("HttpBus"));

var app = builder.Build();

// 端点映射会在启动前（映射时）提前构造 IHandlerContext —— 它仅在构造时订阅渠道注册事件，
// 所以“先映射、后注册”的常规顺序（模块初始化阶段）必定生效，处理器注册不会被遗漏。
app.MapBusEndpoint();

var configurator = app.Services.GetRequiredService<IConfigurator>();
configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

// 服务端注册处理器通道（λ 或扫描，见 core 示例 §2）
configurator.RegisterChannel<CountRequest, int>("count",
    (request, _) => Task.FromResult(request.Start + 1));
configurator.RegisterChannel<CreateOrderRequest, OrderResult>("orders",
    async (request, _) => await CreateOrderAsync(request));

await app.RunAsync();
```

消息类型只要声明在服务端进程能引用到的程序集里即可（两端 Simpler 共享 `Contracts` 程序集最常见）：

```csharp
public sealed class CountRequest : IRequest<int>
{
    public int Start { get; set; }
}
```

再补上第一节里 `orders` 通道用到的请求/结果（与 [Euonia.Bus/SAMPLE.md](../Euonia.Bus/SAMPLE.md) 同源）：

```csharp
public sealed class OrderResult(string OrderId, DateTime CreatedAt, string Status);

public sealed class CreateOrderRequest : IRequest<OrderResult>
{
    public string OrderId { get; set; }
}

static Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
    => Task.FromResult(new OrderResult(request.OrderId, DateTime.UtcNow, "created"));
```

---

## 2. 客户端装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
services.AddLogging();
services.AddOptions();
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

// 默认传输器 = http：CallAsync 时按消息类型选不到策略就落到 http 传输器
services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "http");

services.AddHttpBus("http", options =>
{
    options.Endpoint = "http://localhost:5080";   // 服务端地址；Route 默认 /bus/call
    // options.Route            = "/bus/call";
    // options.SerializerProvider = "SystemTestJson";   // 键控序列化器名
    // options.RequestTimeout   = TimeSpan.FromSeconds(10);
});

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

// 客户端也声明一遍约定（远端处理器按通道匹配，无需在客户端注册处理器）
var configurator = provider.GetRequiredService<IConfigurator>();
configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

// ... 发起远程调用（§3）
```

### 2.1 配置绑定

`HttpBusOptions` 可直接进配置（等价于上面代码里的 `configure`）：

```jsonc
// appsettings.json
{
  "HttpBus": {
    "Enabled": true,
    "Name": "http",
    "Endpoint": "http://localhost:5080",
    "Route": "/bus/call",
    "SerializerProvider": "SystemTestJson",
    "RequestTimeout": "00:00:10"
  }
}
```

```csharp
builder.Services.AddOptions<HttpBusOptions>().Bind(builder.Configuration.GetSection("HttpBus"));
builder.Services.AddHttpBus("http");
```

### 2.2 `HttpBusOptions` 选项表

| 成员 | 默认值 | 说明 |
|---|---|---|
| `Enabled` | `true` | 功能开关 |
| `Name` | `"http"` | 传输器名（键控 `ITransporter` 的键） |
| `Endpoint` | 空 | 服务端 BaseAddress；与 `Route` 拼接为完整地址 |
| `Route` | `"/bus/call"` | 远程调用路由 |
| `SerializerProvider` | `"SystemTestJson"` | 键控 `IMessageSerializer` 名（内置 `SystemTestJson` / `NewtonsoftJson`） |
| `RequestTimeout` | `null` | 请求超时（`HttpClient.Timeout`），`null` 不限制 |
| `MessageHandlerFactory` | `null` | `HttpMessageHandler` 工厂（测试用，如自定义 `HttpClientHandler`） |

---

## 3. 发起远程调用

```csharp
var result = await bus.CallAsync<CountRequest, int>(
    new CountRequest { Start = 41 },
    new CallOptions
    {
        Channel = "count",                    // ← 与服务端 RegisterChannel 的通道名一致
        CorrelationId = "corr-001",
        RequestTraceId = "trace-001",
    },
    cancellationToken);

Console.WriteLine(result);                    // 42
```

**发生了什么**（线协议）：

```
POST http://localhost:5080/bus/call
Content-Type: application/json
X-Bus-Channel: count            MessageHeaders.Channel
X-Bus-CorrelationId: corr-001
X-Bus-MessageId / Type / Trace / Conversation / Authorization
Body: 信封 JSON（含 typeName + payload + channel + correlationId …）

← 200 application/json
Body: { "isSuccess": true, "result": 42, "error": null }          // RemoteReply<int>
```

- 客户端：`HttpTransporter.CallAsync` 序列化 `IMessageEnvelope` POST 到端点，
  按 `RemoteReply<TResponse>` 反序列化；`IsSuccess==false` 时抛 `reply.Error.ToException()`。
- 服务端：`MapBusEndpoint` 读 Body → `RemoteReceiver.ReceiveAsync` 还原信封 → 按
  `message.Channel` 派发到「已注册处理器」，再回 `RemoteReply<TResult>.Success/Failure`。

### 3.1 处理器异常跨进程还原

服务端处理器抛什么，客户端 `CallAsync` 就还原成什么：

```csharp
// 服务端
configurator.RegisterChannel<CountRequest, int>("count", (_, _) => throw new InvalidOperationException("boom"));

// 客户端
try
{
    await bus.CallAsync<CountRequest, int>(new CountRequest { Start = 1 }, new CallOptions { Channel = "count" }, ct);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);   // boom（类型与 Message 均还原）
}
```

### 3.2 超时 / 非 2xx

- `HttpBusOptions.RequestTimeout` 超时抛 `TimeoutException`（来自 `HttpClient`）；
- 端点返回非 2xx（路由不对、500 等）→ `MessageDeliverException`。

---

## 4. 端到端

`WebApplication` 服务端 + 独立 `ServiceProvider` 客户端的最小可运行组合
（与 `HttpEndToEndTests.CallAsync_OverHttpEndpoint_ReturnsResult` 同构）：

```csharp
// ---- 服务端 ----
var builder = WebApplication.CreateBuilder();
builder.Logging.AddConsole();
builder.WebHost.UseUrls("http://127.0.0.1:0");   // 端口 0 → 运行期绑定
builder.Services.AddSingleton<DefaultRequestContextAccessor>();
builder.Services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
builder.Services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
builder.Services.Configure<MessageBusOptions>(o => o.DefaultTransporter = "http");
builder.Services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
builder.Services.AddHttpBus("http");

await using var app = builder.Build();
var c = app.Services.GetRequiredService<IConfigurator>();
c.SetConvention(x => x.Add<DefaultMessageConvention>());
app.MapBusEndpoint();                         // ← 先映射
c.RegisterChannel<CountRequest, int>("count", (req, _) => Task.FromResult(req.Start + 1));
await app.StartAsync();

var endpoint = app.Urls.First();              // http://127.0.0.1:PORT

// ---- 客户端（独立容器，模拟跨进程）----
var services = new ServiceCollection();
services.AddLogging(); services.AddOptions();
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.Configure<MessageBusOptions>(o => o.DefaultTransporter = "http");
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
services.AddHttpBus("http", o => o.Endpoint = endpoint);
var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

var result = await bus.CallAsync<CountRequest, int>(
    new CountRequest { Start = 41 },
    new CallOptions { Channel = "count", CorrelationId = "e2e" },
    CancellationToken.None);

Console.WriteLine(result);                    // 42

await app.StopAsync();
```

---

## 5. 易错点清单

1. `AddHttpBus(name, configure)` **内部已调用 `AddEuoniaBus()`**（`TryAdd*`），无需自己再调；
   但它不注册 `IBus` 依赖的请求上下文访问器——非 Modularity 应用必须手工补齐（§1）。
2. 客户端也必须 `SetConvention` + 能解析 `IConfigurator`；`MessageBusOptions.DefaultTransporter = "http"`
   是消息未被策略分配时落到 HTTP 传输器的先决条件。
3. **端点必须先映射再注册通道**（`MapBusEndpoint()` → `RegisterChannel(...)`）：
   `DefaultHandlerContext` 只在构造时订阅 `ChannelRegistered`，映射即构造；反向顺序会"运行时无处理器"。
4. `Channel` 三处对齐：`RegisterChannel` 的通道名、`[Channel]`/类型推导名、`CallOptions.Channel`。
5. 内置传输只支持 `CallAsync`；对远端 `Send` / `Publish` → `NotSupportedException`。
6. `SerializerProvider` 必须是已注册的键控序列化器（内置 `SystemTestJson` / `NewtonsoftJson`）；
   配错会在构造 `HttpTransporter` 时抛 `InvalidOperationException`。
7. `Endpoint` 为空时请求地址退化为 `Route`（相对路径），用于已注入 `BaseAddress` 的场景。
8. 空串请求头（如无上下文的 `CorrelationId`）会被跳过写头，不会抛错。

## 下一步

- gRPC 版（含**泛化调用**：运行时指定服务名/方法名、不依赖生成桩）见
  [Euonia.Bus.Grpc/SAMPLE.md](../Euonia.Bus.Grpc/SAMPLE.md)。