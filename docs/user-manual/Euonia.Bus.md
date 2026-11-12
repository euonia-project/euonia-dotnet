# Euonia.Bus 用户手册

Euonia 消息总线的运行时引擎：消息分类、传输策略路由、处理器调用、管道、发件箱/收件箱与遥测。本页是端到端教程与 API 速查；安装与完整选项见 [项目 README](../../Source/Euonia.Bus/README.md)。

- **NuGet 包**：`Euonia.Bus`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`
- **完整选项**：[Source/Euonia.Bus/README.md](../../Source/Euonia.Bus/README.md)

## 教程

### 场景：订单事件的发布、单播与查询

订单服务需要在创建订单后**发布** `OrderCreated`、向库存**发送** `ReserveStock`、并提供 `GetOrderStatus` **请求-响应**查询；同时开启 Outbox/Inbox/死信。教程先用 InMemory 传输跑通，随后可替换为 RabbitMQ。

### 前置条件

- .NET SDK 可构建 `net10.0`/`net11.0`；控制台或 ASP.NET Core 宿主项目；无需外部 broker。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.InMemory
```

### 2. 接线：引擎、传输器、可靠性与配置器

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddInMemoryBus("InMemory");
builder.Services.AddInMemoryOutbox();
builder.Services.AddInMemoryInbox();
builder.Services.AddInMemoryDeadLetters();
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);

builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();      // IUnicast / IMulticast / IRequest<>
        convention.Add<AnnotationMessageConvention>();   // [Unicast] / [Multicast] / [Request]
    })
    .SetStrategy("InMemory", strategy =>
    {
        strategy.Add<LocalMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));

var host = builder.Build();
await host.StartAsync();
```

要点：`AddEuoniaBus` 不读配置节，需显式 `BindConfiguration("Euonia:Bus")` 或用 `MessageBusModule`；`AddConfiguratorBuilder` 的委托在 `host.StartAsync()` 时由 `ServiceActivator` 执行；`RegisterChannel` 决定处理器注册到哪个通道。

### 3. 定义消息与处理器

```csharp
using Nerosoft.Euonia.Bus;

[Multicast]
[Channel("orders")]
public sealed class OrderCreated
{
    public long OrderId { get; init; }
}

[Unicast]
public sealed class ReserveStock
{
    public long OrderId { get; init; }
    public int Quantity { get; init; }
}

[Request(typeof(OrderStatus))]
public sealed class GetOrderStatus
{
    public long OrderId { get; init; }
}

public sealed class OrderStatus
{
    public long OrderId { get; init; }
    public string State { get; init; } = null!;
}

public sealed class OrderCreatedHandler : IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class ReserveStockHandler : IHandler<ReserveStock>
{
    public Task HandleAsync(ReserveStock message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class GetOrderStatusHandler : IHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> HandleAsync(GetOrderStatus message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderStatus { OrderId = message.OrderId, State = "Shipped" });
}
```

### 4. 发布、单播、调用与验证

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;

using var scope = host.Services.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();

await bus.Publish(new OrderCreated { OrderId = 1001 }).WithChannel("orders").ExecuteAsync();
await bus.Send(new ReserveStock { OrderId = 1001, Quantity = 2 }).ExecuteAsync();

var status = await bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = 1001 })
                        .WithTimeout(TimeSpan.FromSeconds(5))
                        .ExecuteAsync();

var deadLetters = scope.ServiceProvider.GetRequiredService<IDeadLetterService>();
var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

Console.WriteLine($"status={status.State}");
Console.WriteLine($"deadLetters={deadLetters.GetAll().Count}");
Console.WriteLine($"inboxFailures={inboxStore.GetFailedMessages().Count}");
```

```bash
dotnet run
# 期望输出：
# status=Shipped
# deadLetters=0
# inboxFailures=0
```

### 常见失败点

- **调用挂起**：宿主未 `StartAsync`，配置器委托未执行；或通道未通过 `RegisterChannel` 注册。
- **`MessageTypeException`**：使用特性但未注册 `AnnotationMessageConvention`；或没有策略匹配且未设 `DefaultTransporter`。
- **`MessagePersistentException`**：开启 Outbox 但没有 `IOutboxStore`，检查 `AddInMemoryOutbox()`。
- **Inbox 不去重**：消息级 `UseInbox` 只是发送侧标记，真正开关是 `Inbox:Enabled` + 已注册 `IInboxStore`。

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `IBus.Publish<T>` | 多播发布 | `T : class` |
| `IBus.Send<T>` / `Send<T, TResult>` | 单播发送 | 无结果用 `Unit` |
| `IBus.Call<T, TResult>` | 请求-响应 | `WithTimeout` 仅对 Call 生效 |
| `PublishBuilder<T>` / `SendBuilder<T, TResult>` / `CallBuilder<T, TResult>` | 流式构建器 | `WithChannel`、`WithPriority`、`WithCorrelationId`、`WithTimeout` |
| `IConfigurator` | 约定、策略、通道注册 | `SetConvention`、`SetStrategy(string, ...)`、`RegisterChannel(...)`、`SetChannelResolver` |
| `MessageBusOptions` / `OutboxOptions` / `InboxOptions` | 总线、发件箱、收件箱选项 | `DefaultTransporter` 默认空；`Outbox/Inbox.Enabled=false` |
| `MessageBusModule` | 模块入口，绑定 `Euonia:Bus` | `[DependsOn(typeof(MessageBusModule))]` |
| `IDeadLetterService` | 死信查询、重放、丢弃 | `GetAll()`、`ReplayAsync(messageId)`、`Discard(messageId)` |
| `AddEuoniaBus()` | 注册引擎服务 | 不读配置节 |
| `AddConfiguratorBuilder(Action<IConfigurator>)` | 注册启动时执行的配置委托 | 仅第一次生效 |
| `AddMessageHandler(...)` | 把处理器注册进 DI | `ServiceLifetime` + 程序集/类型 |
| `AddInMemoryOutbox/Inbox/DeadLetters()` | 注册内存存储 | 死信同时注册 `IDeadLetterService` |

### 配置项

配置节 `Euonia:Bus`（`Source/Euonia.Bus/Constants.cs:11`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `DefaultTransporter` | `string` | `""` |
| `AutoLoadAssemblies` | `string[]?` | `null` |
| `Outbox:Enabled` / `Inbox:Enabled` | `bool` | `false` |
| `Outbox:PollingInterval` / `Inbox:PollingInterval` | `TimeSpan` | `00:01:00` |
| `Outbox:MaxRetryAttempts` / `Inbox:MaxRetryAttempts` | `int` | `0`（不限） |
| `Outbox:RetentionPeriod` / `Inbox:RetentionPeriod` | `TimeSpan` | `1.00:00:00` |
| `Outbox:PendingReclaimGracePeriod` | `TimeSpan` | `00:05:00` |

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "InMemory",
      "Outbox": { "Enabled": true, "PollingInterval": "00:00:30" },
      "Inbox": { "Enabled": true, "MaxRetryAttempts": 5 }
    }
  }
}
```

```bash
Euonia__Bus__DefaultTransporter=InMemory      # .NET 原生：双下划线表示层级
EUONIA_BUS_DEFAULTTRANSPORTER=InMemory        # 仓库 alias：大写单下划线（需 Euonia.Modularity 配置构建）
Euonia__Bus__Outbox__Enabled=true
EUONIA_BUS_OUTBOX_ENABLED=true
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus/README.md)
- [Euonia.Bus.Abstract 用户手册](Euonia.Bus.Abstract.md)
- [Euonia.Bus.InMemory 用户手册](Euonia.Bus.InMemory.md)
- [Euonia.Bus.RabbitMq 用户手册](Euonia.Bus.RabbitMq.md)
- [Euonia.Bus.HealthChecks 用户手册](Euonia.Bus.HealthChecks.md)
- [仓库根 README](../../README.md)
