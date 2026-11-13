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
using Microsoft.Extensions.DependencyInjection.Extensions;
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

// 示例省略了 ASP.NET Core 认证配置，因此显式关闭端点授权。
// 生产环境必须保留默认值 true，并为端点配置认证/授权中间件。
// 自动绑定（配置节/环境变量）是最低优先级的默认值，因此显式配置写在 AddHttpBus 之前或之后都生效。
builder.Services.Configure<HttpBusOptions>(options => options.RequireAuthorization = false);

// 一条调用即登记核心总线 + keyed ITransporter("http") + HttpTransporter（重复调用安全），
// 并自动绑定 HttpBus（旧节）与 Euonia:Bus:Http（新节）两个配置节。
builder.Services.AddHttpBus("http");

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

消息类型只要声明在服务端进程能引用到的程序集里即可（两端通常共享一个 `Contracts` 程序集）：

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
using Microsoft.Extensions.DependencyInjection.Extensions;
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

`AddHttpBus` 会**自动绑定**配置节（无需手工 `Bind`），按以下顺序生效：

1. 旧节 `HttpBus`（兼容既有部署）；
2. 新节 `Euonia:Bus:Http`（推荐，覆盖旧节的同名键）；
3. 显式配置：`AddHttpBus` 的 `configure` 形参，以及宿主的 `services.Configure<HttpBusOptions>(...)`。

自动绑定以**最低优先级**注册（`IConfigureOptions<HttpBusOptions>` 插入服务集合最前面），
因此无论 `services.Configure<HttpBusOptions>(...)` 写在 `AddHttpBus` **之前还是之后**，都会覆盖配置节与环境变量的值。

```jsonc
// appsettings.json（新节；也继续支持 "HttpBus": { ... } 旧节）
{
  "Euonia": {
    "Bus": {
      "Http": {
        "Enabled": true,
        "Name": "http",
        "Endpoint": "http://localhost:5080",
        "Route": "/bus/call",
        "SerializerProvider": "SystemTestJson",
        "RequestTimeout": "00:00:10"
      }
    }
  }
}
```

```csharp
builder.Services.AddHttpBus("http");
// 环境变量优先于 appsettings，例如：Euonia__Bus__Http__Endpoint=https://env.example
```

显式覆盖（以下两种写法二选一；均写在 `AddHttpBus` 之前或之后均可）：

```csharp
// 方式一（推荐）：直接使用 configure 形参
builder.Services.AddHttpBus("http", options => options.RequireAuthorization = false);

// 方式二：使用 services.Configure —— 自动绑定优先级最低，因此依然生效
builder.Services.Configure<HttpBusOptions>(options => options.RequireAuthorization = false);
```

### 2.2 `HttpBusOptions` 选项表

| 成员 | 默认值 | 说明 |
|---|---|---|
| `Enabled` | `true` | 功能开关 |
| `Name` | `"http"` | 传输器名（键控 `ITransporter` 的键） |
| `Endpoint` | 空 | **默认**服务端 BaseAddress；与 `Route` 拼接为完整地址。未命中映射与规则的消息走这里 |
| `Endpoints` | 空 | 通道 → 端点路由表（§3.3）：键为精确通道名或通配符模式（含 `*`/`?` 自动按模式匹配），值可含模板占位符 |
| `Route` | `"/bus/call"` | 远程调用路由 |
| `SerializerProvider` | `"SystemTestJson"` | 键控 `IMessageSerializer` 名（内置 `SystemTestJson` / `NewtonsoftJson`） |
| `RequestTimeout` | `null` | 请求超时（`HttpClient.Timeout`），`null` 不限制 |
| `RequireAuthorization` | `true` | 端点是否要求授权；生产环境保持 `true` 并配置认证中间件 |
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
x-channel: count              MessageHeaders.Channel
x-correlation-id: corr-001
x-message-id / x-message-type / x-request-trace-id / x-conversation-id / x-authorization(选填)
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

### 3.3 多端点路由（微服务多服务地址）

微服务体系里每个服务有自己的地址：`HttpBusOptions.Endpoint` 只是**默认端点**。
路由目标统一在 `Endpoints` 里配置——键为精确通道名或通配符模式（含 `*`/`?` 即自动按模式匹配），
值为 BaseAddress，路由（`Route`）保持一致：

```csharp
services.AddHttpBus("http", options =>
{
    options.Endpoint = "http://orders:8080";            // 默认端点（Endpoints 未命中走这里）

    options.Endpoints["billing.CreateInvoice"] = "http://billing:8080";  // 精确通道名
    options.Endpoints["billing.*"]  = "http://billing:8080";             // 通配符：* 不跨段
    options.Endpoints["order.**"]   = "http://orders:8080";              // 通配符：** 跨段
    options.Endpoints["*.ship"]     = "http://{1}:8080";                 // {1} = 第 1 个通配符段
});

// 调用方只关心通道，不感知服务地址
var bill = await bus.CallAsync<CreateInvoice, Invoice>(new CreateInvoice { OrderId = 1 },
    new CallOptions { Channel = "billing.CreateInvoice" }, ct);   // → POST http://billing:8080/bus/call
```

**解析顺序**（`TryResolveEndpoint`）：`Endpoints` 精确键（忽略大小写）→ 通配符键（按声明顺序首个匹配）→ `Endpoint` 默认端点。

**通配符语法**（键含 `*`/`?` 即触发，Glob 风格，通道全名锚定匹配、忽略大小写）：

| 通配符 | 语义 | 示例 |
|---|---|---|
| `*` | 匹配任意字符序列，**不跨段**（不含 `.`） | `billing.*` 命中 `billing.CreateInvoice`，不命中 `billing.a.b` |
| `?` | 匹配单个字符，不跨段 | `coun?` 命中 `count`，不命中 `cou.t` |
| `**` | 匹配任意字符序列，**可跨段**（含 `.`） | `order.**` 命中 `order.payment.capture` |

**端点值模板占位符**：`{channel}` 整个通道名；`{1}`…`{9}` 第 N 个通配符段（按 `*`/`**`/`?` 出现顺序）的匹配值；均经 `Uri.EscapeDataString` 转义。

- 目标服务必须同样部署了 `MapBusEndpoint`（同一套总线协议）；换端点只换“打到哪台服务”；
- 「BaseAddress + Route」的解析结果按端点缓存，同一端点多次调用不重复拼接；
- 未命中任何键的通道走默认端点，单服务场景零配置；
- 命中的端点不可达 → `HttpRequestException` / 非 2xx → `MessageDeliverException`，
  **不会静默回落默认端点**（避免消息发错服务）；
- 路由表是**部署期配置**（启动代码或 appsettings 绑定 `Endpoints`），不随单条消息变化——
  需要按消息动态选目标时，应注册多个 keyed 传输器并用传输策略路由。

**相关测试**（`Euonia.Bus.Http.Tests`）：
- `CallAsync_WithChannelEndpointMapping_PostsToMappedBaseAddress`：精确键路由；
- `CallAsync_WithWildcardEndpointKey_PostsToPatternDerivedBaseAddress` / `CallAsync_WithSegmentTemplate_RendersBaseAddressFromChannel`：
  通配符键与段模板路由；
- `CallAsync_ExactKeyOverridesWildcard_UnmatchedFallsBackToDefault`：精确键 > 通配符键 > 默认的优先级；
- `EndpointPattern_TryResolve_RendersTemplatePlaceholders` / `EndpointPattern_IsAnchoredAndSingleSegment`：
  模板渲染与全名锚定；
- `CallAsync_WithUnmappedChannel_UsesDefaultEndpoint` / `CallAsync_AlternatingChannels_ProduceDistinctUris`：
  默认端点回落与地址缓存。

---

## 4. 端到端

`WebApplication` 服务端 + 独立 `ServiceProvider` 客户端的最小可运行组合
（与 `HttpEndToEndTests.CallAsync_OverHttpEndpoint_ReturnsResult` 同构）：

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Modularity;

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
9. 多端点路由（§3.3）只改 BaseAddress，不改 `Route`：所有目标服务必须用同一 `Route`
   部署 `MapBusEndpoint`；未命中 `Endpoints` 任何键的通道回落默认端点，
   命中的端点不可达则直接失败，不会回落。

## 下一步

- gRPC 版（含**泛化调用**：运行时指定服务名/方法名、不依赖生成桩）见
  [Euonia.Bus.Grpc/SAMPLE.md](../Euonia.Bus.Grpc/SAMPLE.md)。