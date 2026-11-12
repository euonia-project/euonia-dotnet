# Euonia.Bus.RabbitMq

Euonia 总线的 RabbitMQ 传输：以 fanout 交换机实现多播发布、以队列实现单播与请求-响应，内置持久连接、发布确认、优先级队列、死信与 Polly 重试。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.RabbitMq`
- **NuGet 包**：`Euonia.Bus.RabbitMq`
- **依赖**：`Euonia.Bus.Abstract`、`Euonia.Core`、`Euonia.Modularity`、`RabbitMQ.Client`、`Polly`

## 安装

```bash
dotnet add package Euonia.Bus.RabbitMq
```

## 快速开始

### 模块化装配

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.RabbitMq;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MessageBusModule), typeof(RabbitMqBusModule))]
public sealed class AppModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConfiguratorBuilder(config => config
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
            .RegisterChannel(typeof(AppModule).Assembly));
    }
}
```

`RabbitMqBusModule` 只在 `Euonia:Bus:RabbitMq:Enabled=true` 且 `Connection` 非空时注册传输器（`Source/Euonia.Bus.RabbitMq/RabbitMqBusModule.cs:18-28`），因此配置文件至少要给出这两项。

### 手动 DI 装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.RabbitMq;

services.AddEuoniaBus();
services.AddRabbitMqBus("RabbitMq", options =>
{
    options.Connection = "amqp://guest:guest@localhost:5672";
    options.SubscriptionId = "orders-service";
    options.MaxFailureRetries = 3;
});
services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .SetStrategy("RabbitMq", strategy =>
    {
        strategy.Add<DistributedMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));
```

`AddRabbitMqBus` 的签名是 `AddRabbitMqBus(string name, Action<RabbitMqBusOptions>? configureOptions = null)`（`Source/Euonia.Bus.RabbitMq/ServiceCollectionExtensions.cs:28`），`name` 同时作为键控 `ITransporter` 的键与策略匹配名。

## 主要功能

- **多播发布**：声明 fanout 交换机并把消息发布到 `{exchange}@{RoutingKey}`，启用 publisher confirmations（`RabbitMqTransporter.cs:59-83`）。
- **单播与请求-响应**：发送到队列 `{prefix}:{channel}@{SubscriptionId}`，回复使用独占、自动删除的临时队列，按 `CorrelationId` 匹配（`RabbitMqTransporter.cs:103-167`）。
- **命名规则集中**：交换机、队列、订阅标识、优先级的推导都在 `RabbitMqDelivery`，发布端与消费端共用同一份规则（`RabbitMqDelivery.cs:40-76`）。
- **死信**：启用后为每个队列声明 `{queue}.dlx` / `{queue}.dlq` 并设置 `x-dead-letter-exchange`（`RabbitMqRecipient.cs:419-437`）。
- **优先级队列**：`MaxPriority > 0` 时队列附加 `x-max-priority`，消息优先级收敛到 `[0, min(MaxPriority, 9)]`（`RabbitMqDelivery.cs:90-117`）。
- **重试**：连接类异常（`SocketException`、`TimeoutException`、`BrokerUnreachableException`）按 `MaxFailureRetries` 重试，固定间隔 3 秒。

## 核心类型

| 类型 | 作用 |
|------|------|
| `RabbitMqBusModule` | 模块入口：读配置节并按开关注册传输器 |
| `RabbitMqBusOptions` | 连接、命名、确认、优先级与死信选项 |
| `IPersistentConnection` / `DefaultPersistentConnection` | 持久连接与自动恢复 |
| `RabbitMqConsumer` / `RabbitMqSubscriber` / `RabbitMqExecutor` | 单播、多播、请求接收者 |
| `RabbitMqRecipientRegistrar` | 按约定创建并启动接收者，停机时释放 |
| `RabbitMqDelivery` | 交换机/队列/订阅标识/优先级的纯计算规则（`internal`） |
| `AddRabbitMqBus(name, configure)` | 注册传输器、连接工厂与键控 `ITransporter` |

> `RabbitMqTransporter`、`RabbitMqRecipientRegistrar` 等实现类型是 `internal`，请通过 `AddRabbitMqBus` 与 `ITransporter` 使用，不要直接引用。

## 配置项

`RabbitMqBusOptions` 绑定配置节 `Euonia:Bus:RabbitMq`（`Source/Euonia.Bus.RabbitMq/Constants.cs:16`）。

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `Enabled` | `bool` | `false` | 模块是否注册传输器；手动调用 `AddRabbitMqBus` 时不读取 |
| `Name` | `string` | `RabbitMq` | 传输器名称，用于策略匹配与键控注册 |
| `Connection` | `string?` | `null` | AMQP URI，例如 `amqp://user:password@host:5672` |
| `ExchangeNamePrefix` | `string` | `""` | 非空时交换机名为 `{prefix}:{channel}` |
| `QueueNamePrefix` | `string` | `""` | 非空时队列名为 `{prefix}:{channel}@{subscription}` |
| `RoutingKey` | `string` | `*` | 订阅绑定的路由键；发布时拼成 `{exchange}@{RoutingKey}` |
| `Persistent` | `bool` | `true` | 消息是否持久化 |
| `AutoAck` | `bool` | `false` | 是否自动确认；`false` 时处理成功后才确认 |
| `Mandatory` | `bool` | `true` | 不可路由消息是否退回发布端 |
| `MaxFailureRetries` | `int` | `3` | 连接类异常的重试次数 |
| `SubscriptionId` | `string?` | `null` | 订阅标识；为空时回退入口程序集简单名称，再回退通道名 |
| `PrefetchSize` | `int` | `0` | 预取字节数上限，`0` 表示不限制 |
| `PrefetchCount` | `int` | `1` | 确认前可投递的消息条数 |
| `IsDeadLetterEnabled` | `bool` | `true` | 是否为队列声明死信交换机与死信队列 |
| `MaxPriority` | `int` | `0` | 队列最大优先级；`0` 表示不启用优先级 |
| `SerializerProvider` | `string` | `SystemTestJson` | 序列化器名称；可设为 `NewtonsoftJson` |

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "RabbitMq",
      "RabbitMq": {
        "Enabled": true,
        "Name": "RabbitMq",
        "Connection": "amqp://guest:guest@localhost:5672",
        "SubscriptionId": "orders-service",
        "ExchangeNamePrefix": "euonia",
        "QueueNamePrefix": "euonia",
        "MaxPriority": 5,
        "IsDeadLetterEnabled": true
      }
    }
  }
}
```

```bash
Euonia__Bus__RabbitMq__Connection=amqp://guest:guest@localhost:5672   # .NET 原生
EUONIA_BUS_RABBITMQ_CONNECTION=amqp://guest:guest@localhost:5672      # 仓库 alias
Euonia__Bus__RabbitMq__MaxPriority=5
EUONIA_BUS_RABBITMQ_MAXPRIORITY=5
```

## 使用示例

```csharp
using Nerosoft.Euonia.Bus;

[Multicast]
[Channel("orders")]
public sealed class OrderCreated
{
    public long OrderId { get; init; }
}

[Unicast]
public sealed class CancelOrder
{
    public long OrderId { get; init; }
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

public sealed class GetOrderStatusHandler : IHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> HandleAsync(GetOrderStatus message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderStatus { OrderId = message.OrderId, State = "Shipped" });
}
```

```csharp
public sealed class OrderService(IBus bus)
{
    public Task PublishAsync(long orderId, CancellationToken cancellationToken)
        => bus.Publish(new OrderCreated { OrderId = orderId })
              .ExecuteAsync(cancellationToken);

    public Task CancelAsync(long orderId, CancellationToken cancellationToken)
        => bus.Send(new CancelOrder { OrderId = orderId })
              .ExecuteAsync(cancellationToken);

    public Task<OrderStatus> QueryAsync(long orderId, CancellationToken cancellationToken)
        => bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = orderId })
              .ExecuteAsync(cancellationToken);
}
```

`WithPriority` 仅在 `MaxPriority > 0` 时写入消息属性；`WithQueue` 会覆盖按通道推导出的队列名，但消费端仍需在同一队列上监听，否则消息无人消费。

## 注意事项 / 限制

1. **队列与交换机命名必须两端一致**：发布端与消费端共用 `RabbitMqDelivery`，但 `SubscriptionId`、`QueueNamePrefix`、`ExchangeNamePrefix` 必须部署一致，否则发布端检查队列存在性会失败。
2. **`AutoAck=false` 依赖死信配置**：处理失败时消息不确认，若未启用 `IsDeadLetterEnabled`，失败消息会留在队列中反复投递。
3. **优先级需要队列声明支持**：`MaxPriority` 只在队列以 `x-max-priority` 首次声明时生效；同名队列已用其他参数创建时该声明会被 broker 忽略。
4. **模块开关不作用于手动注册**：`Enabled` 只由 `RabbitMqBusModule` 读取；直接调用 `AddRabbitMqBus` 时即使 `Enabled=false` 也会注册传输器。
5. **发布是异步确认**：`PublishAsync` 使用 publisher confirmations，但不可路由消息只记录警告日志（`OnBasicReturnAsync`），不会抛异常。
6. **单播/请求依赖队列消费**：目标队列没有消费者时，请求会一直等待；`CallAsync` 建议设置 `WithTimeout`，`SendAsync` 请传入取消令牌（`SendOptions.Timeout` 不参与发送路径）。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎与约定/策略配置
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约与信封
- [`Euonia.Bus.InMemory`](../Euonia.Bus.InMemory/README.md) —— 无 broker 的进程内替代
- [`Euonia.Bus.ActiveMq`](../Euonia.Bus.ActiveMq/README.md) —— ActiveMQ 传输
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
