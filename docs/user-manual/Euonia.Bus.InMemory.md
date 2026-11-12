# Euonia.Bus.InMemory 用户手册

Euonia 总线的进程内传输：基于容器级强/弱引用信使实现多播、单播与请求-响应，不需要任何外部基础设施。本页演示如何在单进程/单测中可靠地等待异步投递；安装与完整选项见 [项目 README](../../Source/Euonia.Bus.InMemory/README.md)。

- **NuGet 包**：`Euonia.Bus.InMemory`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.InMemory`
- **完整选项**：[Source/Euonia.Bus.InMemory/README.md](../../Source/Euonia.Bus.InMemory/README.md)

## 教程

### 场景：单进程内发布订单通知并查询库存

一个后台任务发布 `OrderCreated`，同一进程的处理器收到后写入内存队列；随后调用 `GetStock` 请求-响应获取库存。教程重点演示**发布是异步的**，必须用信号等待，而不是发布后立即断言。

### 前置条件

- .NET SDK 可构建 `net10.0`/`net11.0`；
- 一个控制台项目（测试项目同理）；
- 不需要 broker。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.InMemory
```

### 2. 接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddInMemoryBus("InMemory", options =>
{
    options.MultipleSubscriberInstance = false;
});
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
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

`AddInMemoryBus(string name, Action<InMemoryBusOptions>?)` 的 `name` 必填，并作为键控 `ITransporter` 的键与 `SetStrategy` 的匹配名。

### 3. 消息、处理器与等待信号

```csharp
using Nerosoft.Euonia.Bus;

[Multicast]
public sealed class OrderCreated
{
    public long OrderId { get; init; }
}

[Request(typeof(StockResult))]
public sealed class GetStock
{
    public long OrderId { get; init; }
}

public sealed class StockResult
{
    public long OrderId { get; init; }
    public int Available { get; init; }
}

public static class Signal
{
    public static readonly TaskCompletionSource<long> Published = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class OrderCreatedHandler : IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        Signal.Published.TrySetResult(message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed class GetStockHandler : IHandler<GetStock, StockResult>
{
    public Task<StockResult> HandleAsync(GetStock message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new StockResult { OrderId = message.OrderId, Available = 42 });
}
```

### 4. 发布、调用与等待

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;

using var scope = host.Services.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();

await bus.Publish(new OrderCreated { OrderId = 1001 }).ExecuteAsync();

var publishedId = await Signal.Published.Task.WaitAsync(TimeSpan.FromSeconds(2));
Console.WriteLine($"published={publishedId}");

var stock = await bus.Call<GetStock, StockResult>(new GetStock { OrderId = 1001 })
                     .WithTimeout(TimeSpan.FromSeconds(2))
                     .ExecuteAsync();
Console.WriteLine($"available={stock.Available}");

await host.StopAsync();
```

### 5. 运行与验证

```bash
dotnet run
# 期望输出：
# published=1001
# available=42
```

### 常见失败点

- **`PublishAsync` 返回后断言失败**：多播走接收者的后台串行泵，必须用 `TaskCompletionSource`/轮询等待。
- **`CallAsync` 一直挂起**：通道没有接收者，通常是漏了 `RegisterChannel` 或约定不识别消息；务必传取消令牌或 `WithTimeout`。
- **处理器执行两次**：同一进程同时启用多个传输器且策略都命中同一通道；用 `EvaluateIncoming`/`EvaluateOutgoing` 限定唯一传输器。
- **单例注入报错**：`InMemoryTransporter` 是 `Scoped`，不要注入单例服务。

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddInMemoryBus(name, configure)` | 注册传输器、接收者与键控 `ITransporter` | `name` 必填，默认名 `InMemory` |
| `InMemoryBusModule` | 模块入口，绑定 `Euonia:Bus:InMemory` | `[DependsOn(typeof(InMemoryBusModule))]` |
| `InMemoryBusOptions` | 传输名称与接收者行为 | `Name=InMemory`、`MultipleSubscriberInstance=false` |
| `InMemoryTransporter` | 公开的 `ITransporter` 实现 | `Scoped` 生命周期 |
| `InMemoryMessengerProvider` | 容器级强弱引用信使 | 每个容器一对实例 |
| `InMemoryRecipientRegistrar` | 按约定创建/注册接收者 | 单例，停机时注销 |
| `InMemoryConsumer` / `InMemorySubscriber` / `InMemoryExecutor` | 单播、多播、请求接收者 | 由注册器创建 |
| `ITransporter` | 传输契约：`PublishAsync` / `SendAsync` / `CallAsync` | `Delivered` 事件 |
| `IMessenger` | 信使契约 | `WeakReferenceMessenger` / `StrongReferenceMessenger` |
| `RequestMessage<T>` / `AsyncRequestMessage<T>` | 请求消息辅助类型 | 用于自定义接收者 |
| `MessageHandler<TRecipient, TMessage>` | 处理器委托 | `(recipient, message) => ...` |
| `ValueChangedMessage<T>` | 值变更消息载体 | 仅承载 `Value` |

### 配置项

配置节 `Euonia:Bus:InMemory`（`Source/Euonia.Bus.InMemory/Constants.cs:16`）：

| 键 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Name` | `string` | `InMemory` | 传输器名称 |
| `LazyInitialize` | `bool` | `true` | 预留属性，当前实现不读取 |
| `MaxConcurrentCalls` | `int` | `1` | 预留属性，当前实现不读取 |
| `MultipleSubscriberInstance` | `bool` | `false` | `true` 时每个注册项创建独立接收者 |

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "InMemory",
      "InMemory": { "Name": "InMemory", "MultipleSubscriberInstance": false }
    }
  }
}
```

```bash
Euonia__Bus__InMemory__Name=InMemory            # .NET 原生：双下划线表示层级
EUONIA_BUS_INMEMORY_NAME=InMemory               # 仓库 alias：大写单下划线
Euonia__Bus__InMemory__MultipleSubscriberInstance=true
EUONIA_BUS_INMEMORY_MULTIPLESUBSCRIBERINSTANCE=true
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus.InMemory/README.md)
- [Euonia.Bus 用户手册](Euonia.Bus.md)
- [Euonia.Bus.Abstract 用户手册](Euonia.Bus.Abstract.md)
- [Euonia.Bus.RabbitMq 用户手册](Euonia.Bus.RabbitMq.md)
- [仓库根 README](../../README.md)
