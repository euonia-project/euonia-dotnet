# Euonia.Bus 全景示例

> 一份从注册到三种消息模式（发布 / 发送 / 调用）再到跨进程远程调用的完整示例。
> 远程传输的具体实现见 [Euonia.Bus.Http/SAMPLE.md](../Euonia.Bus.Http/SAMPLE.md) 与
> [Euonia.Bus.Grpc/SAMPLE.md](../Euonia.Bus.Grpc/SAMPLE.md)。

**示例全部代码与仓库内已通过的测试保持一致，API 签名均按源码逐一核对，可直接放入项目使用。**

---

## 0. 场景

一个「订单」小系统，贯穿全文：

| 消息 | 约定 | 说明 |
|---|---|---|
| `CreateOrderRequest` | `IRequest<OrderResult>` | 创建订单（请求-响应，`CallAsync`） |
| `SyncOrderEvent` | `IMulticast` | 订单已同步（发布-订阅，`PublishAsync`） |
| `LockOrderCommand` | `IUnicast` | 锁定订单（点对点投递，`SendAsync`） |

需要的能力：

- 调用 `CallAsync` 拿到 `OrderResult` 结果；
- 发布 `SyncOrderEvent` 事件给所有订阅者；
- 点对点投递 `LockOrderCommand`，不期望响应；
- 同一套消息类型既能被**本地**处理，也能被**远端进程**处理（见 §6 与 HTTP/gRPC 示例）。

订单结果与数据载体：

```csharp
public sealed record OrderResult(string OrderId, DateTime CreatedAt, string Status);

public sealed record OrderData(string ProductId, int Quantity, decimal Amount);
```

---

## 1. 装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
services.AddLogging();

// 应用如果基于 Nerosoft.Euonia.Modularity 初始化，以下请求上下文访问器已由框架注册：
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();

// ① 注册消息总线核心（配置器、处理器上下文、IBus、分发器、启动激活后台服务、管道支持）
services.AddEuoniaBus();

// ② 可选：内存版发件箱 / 收件箱（仅开发参考；生产请替换为 Redis / 数据库实现）
// services.AddInMemoryOutbox();
// services.AddInMemoryInbox();

// ③ 注册具体处理器到 DI（供扫描注册的 IHandler<,> 通道在派发时按接口解析）
services.AddMessageHandler(ServiceLifetime.Transient, typeof(OrderApplication).Assembly);

// ④ 配置消息约定与通道注册（ConfiguratorBuilder 在应用启动时被 ServiceActivator 执行）
services.AddConfiguratorBuilder(configurator =>
{
    configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
    configurator.RegisterChannel(typeof(OrderApplication).Assembly);
});

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();
```

> `ConfiguratorBuilder` 委托的执行时机在**应用启动后**（`ServiceActivator.ExecuteAsync`），
> 因此「先注册通道、后发起调用」的顺序没有额外负担；端点映射时序见 §6 与 HTTP/gRPC 示例。

---

## 2. 消息与处理器

### 2.1 类定义方式（`IRequest<TResponse>` + `IHandler<TMessage, TResponse>`）

消息即普通的 POCO，实现 `IRequest<TResponse>` 即声明「期望响应」：

```csharp
using Nerosoft.Euonia.Bus;

[Channel("orders")]                       // 可选：显式指定通道名（默认按消息类型全名解析）
public sealed class CreateOrderRequest : IRequest<OrderResult>
{
    public string OrderId { get; set; }
    public OrderData Data { get; set; }
}
```

应用自己的存储（示意，真实用 EF/Repository）：

```csharp
public interface IOrderStore
{
    Task<OrderRecord> CreateAsync(string orderId, OrderData data, CancellationToken cancellationToken = default);
}

public sealed record OrderRecord(string Id, DateTime CreatedAt, string Status);

public sealed class MemoryOrderStore : IOrderStore
{
    public Task<OrderRecord> CreateAsync(string orderId, OrderData data, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderRecord(orderId, DateTime.UtcNow, "created"));
}
```

处理器实现 `IHandler<TMessage, TResponse>`，与消息一一对应：

```csharp
using Nerosoft.Euonia.Bus;

public sealed class CreateOrderHandler : IHandler<CreateOrderRequest, OrderResult>
{
    private readonly IOrderStore _store;

    public CreateOrderHandler(IOrderStore store)
    {
        _store = store;
    }

    public async Task<OrderResult> HandleAsync(
        CreateOrderRequest message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var order = await _store.CreateAsync(message.OrderId, message.Data, cancellationToken);
        return new OrderResult(order.Id, order.CreatedAt, order.Status);
    }
}
```

订阅 / 无响应消息用 `IHandler<TMessage>`（返回 `Task`，内部封为 `Unit`）代替：

```csharp
public sealed class SyncOrderEvent : IMulticast
{
    public string OrderId { get; set; }
}

public sealed class SyncOrderHandler : IHandler<SyncOrderEvent>
{
    public Task HandleAsync(SyncOrderEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        // ... 同步订单到搜索索引
        return Task.CompletedTask;
    }
}
```

### 2.2 Lambda 方式（无需单独类型）

```csharp
configurator.RegisterChannel<CreateOrderRequest, OrderResult>("orders",
    (request, _) => Task.FromResult(new OrderResult(request.OrderId, DateTime.UtcNow, "created")));

// 无返回值版本
configurator.RegisterChannel<SyncOrderEvent>("order-sync", (message, _) => Task.CompletedTask);
```

### 2.3 静态方法 + `[Subscribe]` 方式

扫描器同时支持在任意类上以特性标注订阅方法（参数为消息、可选 `IMessageContext`/`CancellationToken`，通道名取自特性）：

```csharp
public sealed class OrderWatcher
{
    [Subscribe("orders")]
    public async Task OnOrderCreated(CreateOrderRequest message, IMessageContext context)
    {
        // ...
    }
}
```

---

## 3. 三种消息模式

### 3.1 调用（请求-响应，`CallAsync`）——推荐远程传输

```csharp
// 直接调用：消息体 + 选项
var result = await bus.CallAsync<CreateOrderRequest, OrderResult>(
    new CreateOrderRequest { OrderId = "o-1", Data = new OrderData("sku-1", 2, 99.99m) },
    new CallOptions { Channel = "orders" },
    cancellationToken);

// IRequest<TResponse> 消息的专用重载（TResult 由接口推断）
var result2 = await bus.CallAsync(new CreateOrderRequest { OrderId = "o-2" }, cancellationToken: cancellationToken);

// 链式 Builder：可配置通道 / 超时 / 关联 id / 管道
var result3 = await bus.Call<CreateOrderRequest, OrderResult>(new CreateOrderRequest { OrderId = "o-3" })
                                      .WithChannel("orders")
                                      .WithTimeout(TimeSpan.FromSeconds(5))
                                      .WithCorrelationId("corr-3")
                                      .ExecuteAsync(cancellationToken);
```

### 3.2 发布（发布-订阅，`PublishAsync`）

```csharp
// 所有订阅了该通道的处理程序都会收到
await bus.PublishAsync(new SyncOrderEvent { OrderId = orderId }, cancellationToken);

// 或链式配置
await bus.Publish(new SyncOrderEvent { OrderId = orderId })
         .WithChannel("order-sync")
         .ExecuteAsync(cancellationToken);
```

### 3.3 发送（点对点，`SendAsync`）——投递给单个处理程序

```csharp
public sealed class LockOrderCommand : IUnicast
{
    public string OrderId { get; set; }
}

await bus.SendAsync(new LockOrderCommand { OrderId = orderId }, cancellationToken);
```

### 3.4 `CallOptions` 常用项

| 成员 | 说明 |
|---|---|
| `Channel` | 通道名。为空时由 `MessageChannelResolver` 按消息类型推导（`[Channel]` → 类型全名） |
| `Timeout` / `Delay` / `Priority` | 超时（毫秒，限时未完成抛 `TimeoutException`）/ 延迟 / 优先级 |
| `CorrelationId` | 关联标识，跨链路传播（HTTP 头 / gRPC 属性透传） |
| `RequestTraceId` | 请求追踪标识 |
| `MessageId` | 自定义消息 id（默认 `ObjectId` 顺序 GUID） |
| `MetadataSetter` | 自定义消息元数据委托 |
| `UseOutbox` | 单条消息级发件箱开关（`true` 强制 / `false` 跳过 / `null` 跟随全局） |

---

## 4. 约定与通道

- **约定**决定消息是单播（`IUnicast`）、多播（`IMulticast`）还是请求（`IRequest<TResponse>`）。
  通过 `SetConvention(x => x.Add<DefaultMessageConvention>())` 装配，策略分发据此判断“单播类型不得匹配多个传输”。
- **通道名解析优先级**（`MessageChannelResolver.ResolveChannel`）：
  1. `[Channel("name")]` 特性；
  2. 实现 `ITransportable` / 标了 `[Transportable]` → 消息类型全名；
  3. 普通类消息 → 类型全名。
- 调用时显式 `CallOptions.Channel` 的优先级最高，且是「本地处理器注册」与「远端处理器注册」都必须匹配的键。

---

## 5. 远程调用契约（核心包内）

`Euonia.Bus` 只定义**线协议契约**，具体端点由 `Euonia.Bus.Http`（POST `/bus/call`）与
`Euonia.Bus.Grpc`（`ReplierService.Call`）落地：

```
客户端进程                               服务端进程
IBus.CallAsync
   └─ StrategicDispatcher（按 DefaultTransporter / 策略选传输器）
        └─ keyed ITransporter（"http" / "grpc"）
             └─ 信封 JSON / gRPC 请求 ──► 端点（MapBusEndpoint / MapGrpcBusService）
                                              └─ RemoteReceiver.ReceiveAsync
                                                   └─ 反序列化 IMessageEnvelope
                                                        └─ 按 message.Channel 派发到处理器
                                                             └─ RemoteReply<TResult>.Success / Failure 回传
```

关键类型：

- `RemoteReceiver.ReceiveAsync(serializer, handler, payload, ct)`：服务端只此一个入口。
  反序列化失败或处理抛异常时**统一**回 `RemoteReply<TResult>.Failure(RemoteError.Create(ex))`；
  处理成功回 `Success(result)`。通道级处理结果经 `MessageContext.Responded/Failed/Completed` 事件汇聚。
- `RemoteReply<TResult>`：`IsSuccess` / `Result` / `Error`；`Success(...)` / `Failure(...)` 工厂。
- `RemoteError`：携带 `Type`（异常全名）、`Message`、`StackTrace`，客户端据此**还原为原始异常类型**（HTTP/gRPC 已实测 `InvalidOperationException("boom")` 跨进程还原）。
- 客户端经 `StrategyAssignedTypes` / `MessageBusOptions.DefaultTransporter` 选定传输器（键控 `ITransporter`）。

> **边界**：内置 HTTP/gRPC 传输器只支持请求-响应（`CallAsync`）；`SendAsync` / `PublishAsync`
> 通过它们会抛 `NotSupportedException`（远程 = 有去有回，即发即忘请用队列传输如 RabbitMQ）。

---

## 6. 端到端

把 §1–§5 串起来：同一套 `CreateOrderRequest` 消息在一个进程内自洽运行（处理器注册 + 调用）。
（此流程与 `Euonia.Bus.Http.Tests` 的端到端用例同构，仅省去远端进程。）

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<DefaultRequestContextAccessor>();
services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
services.AddSingleton<System.IServiceAccessor, ServiceAccessor>();
services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
services.AddEuoniaBus();
services.AddSingleton<IOrderStore, MemoryOrderStore>();
services.AddConfiguratorBuilder(configurator =>
{
    configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
    configurator.RegisterChannel<CreateOrderRequest, OrderResult>("orders",
        (request, _) => Task.FromResult(new OrderResult(request.OrderId, DateTime.UtcNow, "created")));
});

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

// 启动激活：io 里会执行 ConfiguratorBuilder → 通道注册在这里完成
// （真实托管环境由 ServiceActivator 后台服务完成）

var result = await bus.CallAsync<CreateOrderRequest, OrderResult>(
    new CreateOrderRequest { OrderId = "o-1", Data = new OrderData("sku-1", 2, 99.99m) },
    new CallOptions { Channel = "orders" },
    CancellationToken.None);

Console.WriteLine($"{result.OrderId} / {result.Status}");   // o-1 / created
```

> 跨进程的完整可运行示例（服务端 `MapBusEndpoint` / 客户端 `AddHttpBus`）见
> [Euonia.Bus.Http/SAMPLE.md](../Euonia.Bus.Http/SAMPLE.md)；
> gRPC 泛化调用版见 [Euonia.Bus.Grpc/SAMPLE.md](../Euonia.Bus.Grpc/SAMPLE.md)。

---

## 7. 易错点清单

1. `IBus` 解析依赖框架宿主的请求上下文访问器（`DefaultRequestContextAccessor` /
   `DelegateRequestContextAccessor` / `IServiceAccessor` / `IRequestContextAccessor`）。
   未基于 Modularity 的应用必须手工注册（§1 清单），否则 `IBus` 可能解析不出来。
2. `AddEuoniaBus()` 用 `TryAdd*` 注册，「重复调用安全」，可放心与 `AddHttpBus`/`AddGrpcBus` 叠加。
3. **扫描注册的 `IHandler<,>` 处理器必须同时注册到 DI**（`AddMessageHandler`），
   否则派发时 `DefaultHandlerContext` 按接口 `GetRequiredService` 会抛 `InvalidOperationException`。
4. 通道名是「本地 / 远端两侧注册」必须一致的键：`[Channel("orders")]`、λ 注册的 `"orders"`、
   调用方的 `CallOptions.Channel = "orders"` 三者必须相同。
5. `RegisterChannel<TMessage, TResult>(channel, handler)` 的终端签名是
   `Func<TMessage, IMessageContext, Task<TResult>>`——λ 两个参数（消息、上下文），第二个不要省略类型。
6. `CallAsync<TMessage, TResult>(... , CallOptions options, CancellationToken)` 等「选项重载」是接口默认实现，
   替换为「行为重载」时会把行为委托传给管道（`Action<IPipeline<...>>`），不要与「取消令牌」混淆。
7. `Timeout` 单位为**毫秒**；限时未完成抛 `TimeoutException`，适用于 `CallAsync` 系列。
8. HTTP/gRPC 传输只支持 `CallAsync`；对远端 `Send`/`Publish` 抛 `NotSupportedException`。
9. `MessageMetadata` / 请求头透传是**选填**的；但 `Authorization` 只会在非空时写入传输头。
10. 远程端处理器抛异常不丢失类型——`RemoteError` 在 `Euonia.Bus` 核心定义，客户端还原异常类型，
    但若服务端与客户端**引用了不同的程序集版本**，还原可能退化（保留 `Message`/`StackTrace`）。