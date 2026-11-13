# Euonia.Bus 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

Euonia 消息总线的运行时引擎：消息分类、传输策略路由、处理器调用、管道、发件箱/收件箱与遥测。本页是端到端教程；安装与完整选项见 [项目 README](../../../Source/Euonia.Bus/README.md)。

- **NuGet 包**：`Euonia.Bus`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`
- **完整选项**：[Source/Euonia.Bus/README.md](../../../Source/Euonia.Bus/README.md)

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

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Bus/README.md)
- [Euonia.Bus.Abstract 用户手册](../Euonia.Bus.Abstract/TUTORIAL.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/TUTORIAL.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/TUTORIAL.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/TUTORIAL.md)
- [仓库根 README](../../../README.md)
