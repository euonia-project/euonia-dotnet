# Euonia.Bus.ActiveMq 用户手册

Euonia 总线的 Apache ActiveMQ 传输：主题（Topic）做发布/订阅、队列做点对点与请求-响应，基于 NMS 与持久连接，并用 Polly 处理连接级失败。本页给出本机可复现的端到端场景；完整选项见 [项目 README](../../Source/Euonia.Bus.ActiveMq/README.md)。

- **NuGet 包**：`Euonia.Bus.ActiveMq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.ActiveMq`
- **完整选项**：[Source/Euonia.Bus.ActiveMq/README.md](../../Source/Euonia.Bus.ActiveMq/README.md)

## 教程

### 场景：订单事件广播 + 库存查询

订单服务向主题发布 `OrderCreated`，同进程的两个处理器（审计、通知）都应收到；随后通过队列发送 `GetStock` 并等待回复。示例验证多播订阅与请求-响应两条路径。

### 前置条件

- 本机可运行 ActiveMQ Classic（示例用 Docker）；
- .NET SDK 可构建 `net10.0`/`net11.0`；
- 发送端与消费端使用相同的 `SubscriptionId`。

### 1. 启动 broker 并安装

```bash
docker run -d --name activemq -p 61616:61616 -p 8161:8161 apache/activemq-classic
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.ActiveMq
```

### 2. 配置与接线

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "ActiveMq",
      "ActiveMq": {
        "Enabled": true,
        "Name": "ActiveMq",
        "Connection": "activemq:tcp://localhost:61616",
        "SubscriptionId": "orders-service",
        "MaxFailureRetries": 3
      }
    }
  }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.ActiveMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddOptions<ActiveMqBusOptions>().BindConfiguration("Euonia:Bus:ActiveMq");
builder.Services.AddActiveMqBus("ActiveMq");
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .SetStrategy("ActiveMq", strategy =>
    {
        strategy.Add<DistributedMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));

var host = builder.Build();
await host.StartAsync();
```

模块方式等价于 `[DependsOn(typeof(MessageBusModule), typeof(ActiveMqBusModule))]`；模块只在 `Enabled=true` 且 `Connection` 非空时注册。

### 3. 消息与两个多播处理器

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

public sealed class AuditHandler : IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class NotifyHandler : IHandler<OrderCreated>
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

# 观察主题与队列（http://localhost:8161，admin/admin）
# Topics:  orders
# Queues:  GetStock 的通道名（默认类型全名）及其临时回复队列
```

消息的通道名决定主题/队列名（默认等于通道名）；`WithQueue` 会替换发送目标，但消费端仍按通道名监听。

### 常见失败点

- **模块不注册**：`Enabled` 未设或 `Connection` 为空；手动 `AddActiveMqBus` 时仍要赋值 `Connection`。
- **主题收不到**：`SubscriptionId` 不一致，或消费端未在对应通道启动。
- **请求-响应挂起**：目标队列无消费者，或账号无临时队列权限；调用侧务必设置超时/取消令牌。
- **重试没发生**：只有连接级异常才重试；业务异常需处理器自行补偿。

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddActiveMqBus(name, configure)` | 注册连接工厂、传输器与键控 `ITransporter` | `name` 必填 |
| `ActiveMqBusModule` | 模块入口，按 `Enabled`/`Connection` 注册 | `[DependsOn(typeof(ActiveMqBusModule))]` |
| `ActiveMqBusOptions` | 连接、重试、订阅与序列化 | 见下表 |
| `IPersistentConnection` | 持久连接契约 | `CreateSessionAsync`、`Dispose` |
| `ActiveMqBusOptions.Connection` | NMS 连接串 | 默认 `null`，必填 |
| `ActiveMqBusOptions.SubscriptionId` | 主题共享订阅名 | 回退入口程序集名 → `DefaultSubscription` |
| `ActiveMqBusOptions.MaxFailureRetries` | 连接级重试次数 | 默认 `3` |
| `ActiveMqBusOptions.Name` | 传输器名称 | 默认 `ActiveMq` |
| `ActiveMqBusOptions.Enabled` | 模块注册开关 | 默认 `false` |
| `ActiveMqBusOptions.SerializerProvider` | 序列化器名称 | 默认 `SystemTestJson` |

> `ActiveMqTransporter`、`ActiveMqRecipientRegistrar` 等实现类型是 `internal`，请通过 `AddActiveMqBus` 与 `ITransporter` 使用。

### 配置项

配置节 `Euonia:Bus:ActiveMq`（`Source/Euonia.Bus.ActiveMq/Constants.cs:16`）：

| 键 | 类型 | 默认值 |
|---|---|---|
| `Enabled` | `bool` | `false` |
| `Name` | `string` | `ActiveMq` |
| `Connection` | `string?` | `null` |
| `MaxFailureRetries` | `int` | `3` |
| `SubscriptionId` | `string?` | `null` |
| `SerializerProvider` | `string` | `SystemTestJson` |

```bash
Euonia__Bus__ActiveMq__Connection=activemq:tcp://localhost:61616   # .NET 原生
EUONIA_BUS_ACTIVEMQ_CONNECTION=activemq:tcp://localhost:61616        # 仓库 alias
Euonia__Bus__ActiveMq__Enabled=true
EUONIA_BUS_ACTIVEMQ_ENABLED=true
```

## 相关文档

- [项目 README](../../Source/Euonia.Bus.ActiveMq/README.md)
- [Euonia.Bus 用户手册](Euonia.Bus.md)
- [Euonia.Bus.RabbitMq 用户手册](Euonia.Bus.RabbitMq.md)
- [Euonia.Bus.HealthChecks 用户手册](Euonia.Bus.HealthChecks.md)
- [仓库根 README](../../README.md)
