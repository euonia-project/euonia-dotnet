# Euonia.Bus.RabbitMq 用户手册

Euonia 总线的 RabbitMQ 传输：fanout 交换机做多播、队列做单播与请求-响应，内置持久连接、发布确认、优先级、死信与重试。本页用一个可本机复现的生产者/消费者场景说明接线；完整选项见 [项目 README](../../Source/Euonia.Bus.RabbitMq/README.md)。

- **NuGet 包**：`Euonia.Bus.RabbitMq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.RabbitMq`
- **完整选项**：[Source/Euonia.Bus.RabbitMq/README.md](../../Source/Euonia.Bus.RabbitMq/README.md)

## 教程

### 场景：订单事件跨进程投递 + 库存查询

订单服务发布 `OrderCreated`，库存服务订阅并处理；网关再通过 `GetStock` 请求-响应查询库存。示例把两个角色放在同一进程便于验证，生产环境拆成两个服务、共享同一份消息契约程序集。

### 前置条件

- 本机可运行 RabbitMQ（示例用 Docker）；
- .NET SDK 可构建 `net10.0`/`net11.0`；
- 两个角色使用相同的 `SubscriptionId` 与命名前缀。

### 1. 启动 broker 并安装

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.RabbitMq
```

### 2. 配置与接线

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "RabbitMq",
      "RabbitMq": {
        "Enabled": true,
        "Name": "RabbitMq",
        "Connection": "amqp://guest:guest@localhost:5672",
        "SubscriptionId": "stock-service",
        "QueueNamePrefix": "euonia",
        "ExchangeNamePrefix": "euonia",
        "MaxPriority": 5,
        "IsDeadLetterEnabled": true
      }
    }
  }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddOptions<RabbitMqBusOptions>().BindConfiguration("Euonia:Bus:RabbitMq");
builder.Services.AddRabbitMqBus("RabbitMq");
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .SetStrategy("RabbitMq", strategy =>
    {
        strategy.Add<DistributedMessageTransportStrategy>();
        strategy.Add(new AnnotationTransportStrategy(["RabbitMq"]));
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));

var host = builder.Build();
await host.StartAsync();
```

模块方式等价于 `[DependsOn(typeof(MessageBusModule), typeof(RabbitMqBusModule))]`，但模块只在 `Enabled=true` 且 `Connection` 非空时注册。

### 3. 消息与处理器

```csharp
using Nerosoft.Euonia.Bus;

[Multicast]
[Channel("orders")]
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

public sealed class OrderCreatedHandler : IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class GetStockHandler : IHandler<GetStock, StockResult>
{
    public Task<StockResult> HandleAsync(GetStock message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new StockResult { OrderId = message.OrderId, Available = 42 });
}
```

### 4. 发布、调用与验证

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;

using var scope = host.Services.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();

await bus.Publish(new OrderCreated { OrderId = 1001 }).ExecuteAsync();

var stock = await bus.Call<GetStock, StockResult>(new GetStock { OrderId = 1001 })
                     .WithTimeout(TimeSpan.FromSeconds(5))
                     .ExecuteAsync();

Console.WriteLine($"available={stock.Available}");
```

```bash
dotnet run
# 期望输出：available=42

# 观察队列与死信队列（管理界面 http://localhost:15672，guest/guest）
docker exec rabbitmq rabbitmqctl list_queues name messages consumers
```

配置前缀时队列名为 `{QueueNamePrefix}:{channel}@{SubscriptionId}`、交换机为 `{ExchangeNamePrefix}:{channel}`；未配置前缀时分别为 `{channel}@{SubscriptionId}` 与 `{channel}`。`GetStock` 的请求队列必须已有消费者，否则发送端会抛 `MessageDeliverException`。

### 常见失败点

- **`MessageDeliverException`（无消费者）**：消费端未启动，或两端 `SubscriptionId`/`QueueNamePrefix` 不一致。
- **模块不注册**：`Enabled` 未设或 `Connection` 为空；手动 `AddRabbitMqBus` 时仍要赋值 `Connection`。
- **订阅端收不到**：两端 `ExchangeNamePrefix` 不一致，或消息未被约定识别为多播。
- **优先级无效**：`MaxPriority=0`，或同名队列已用其他参数声明过，需删除重建。

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddRabbitMqBus(name, configure)` | 注册连接工厂、传输器与键控 `ITransporter` | `name` 必填 |
| `RabbitMqBusModule` | 模块入口，按 `Enabled`/`Connection` 注册 | `[DependsOn(typeof(RabbitMqBusModule))]` |
| `IPersistentConnection` | 持久连接契约 | `CreateChannelAsync`、`Dispose` |
| `RabbitMqBusOptions`（连接与命名） | 连接串、名称、订阅与前缀 | 见下表 |
| `RabbitMqBusOptions`（确认、重试与优先级） | 确认模式、持久化、重试、预取、死信、优先级 | 见下表 |

> `RabbitMqTransporter`、`RabbitMqRecipientRegistrar` 等实现类型是 `internal`，请通过 `AddRabbitMqBus` 与 `ITransporter` 使用。

### 配置项

配置节 `Euonia:Bus:RabbitMq`（`Source/Euonia.Bus.RabbitMq/Constants.cs:16`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `Enabled` | `bool` | `false` |
| `Name` | `string` | `RabbitMq` |
| `Connection` | `string?` | `null` |
| `ExchangeNamePrefix` / `QueueNamePrefix` | `string` | `""` |
| `RoutingKey` | `string` | `*` |
| `Persistent` | `bool` | `true` |
| `AutoAck` | `bool` | `false` |
| `Mandatory` | `bool` | `true` |
| `MaxFailureRetries` | `int` | `3` |
| `SubscriptionId` | `string?` | `null` |
| `PrefetchSize` / `PrefetchCount` | `int` | `0` / `1` |
| `IsDeadLetterEnabled` | `bool` | `true` |
| `MaxPriority` | `int` | `0` |
| `SerializerProvider` | `string` | `SystemTestJson` |

```bash
Euonia__Bus__RabbitMq__Connection=amqp://guest:guest@localhost:5672   # .NET 原生
EUONIA_BUS_RABBITMQ_CONNECTION=amqp://guest:guest@localhost:5672      # 仓库 alias
Euonia__Bus__RabbitMq__MaxPriority=5
EUONIA_BUS_RABBITMQ_MAXPRIORITY=5
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus.RabbitMq/README.md)
- [Euonia.Bus 用户手册](Euonia.Bus.md)
- [Euonia.Bus.InMemory 用户手册](Euonia.Bus.InMemory.md)
- [Euonia.Bus.HealthChecks 用户手册](Euonia.Bus.HealthChecks.md)
- [仓库根 README](../../README.md)
