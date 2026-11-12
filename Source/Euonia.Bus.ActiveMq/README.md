# Euonia.Bus.ActiveMq

Euonia 总线的 Apache ActiveMQ 传输：基于 NMS 实现主题（Topic）发布/订阅、队列点对点发送，以及通过临时回复队列与关联 ID 完成请求-响应。支持持久连接与 Polly 重试。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.ActiveMq`
- **NuGet 包**：`Euonia.Bus.ActiveMq`

## 安装

```bash
dotnet add package Euonia.Bus.ActiveMq
```

## 主要功能

- **完整 `ITransporter` 实现**：`ActiveMqTransporter` 支持发布、发送与调用。
- **消息映射**：`IUnicast` → `ActiveMqConsumer`，`IMulticast` → `ActiveMqSubscriber`，`IRequest<>` → `ActiveMqExecutor`。
- **请求-响应**：临时回复队列 + 关联 ID。
- **持久连接**：`DefaultPersistentConnection` 管理连接生命周期，释放后立即失败而非自旋。
- **可靠性**：基于 Polly 的重试与 `MaxFailureRetries` 配置。
- **投递属性**：`Queue` 覆盖 `Send`/`Call` 的目标队列，`Priority` 映射为 NMS 优先级（0-9）。

## 快速开始

注册传输器并设为默认传输：

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.ActiveMq;

services.AddActiveMqBus("ActiveMq", options =>
{
    options.Connection = "activemq:tcp://localhost:61616";
    options.SubscriptionId = "orders-service";
    options.MaxFailureRetries = 3;
});

services.Configure<MessageBusOptions>(options =>
{
    options.DefaultTransporter = "ActiveMq";
});
```

也可以从配置节绑定（模块会自动读取）：

```json
{
  "Euonia": {
    "Bus": {
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

发送消息时设置优先级：

```csharp
await bus.Send(new CancelOrder { OrderId = 42 })
    .WithPriority(5)
    .ExecuteAsync();
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Enabled` | `false` | 是否启用（配置节绑定模式） |
| `Name` | `ActiveMq` | 传输器名称 |
| `Connection` | `null` | ActiveMQ 连接字符串 |
| `MaxFailureRetries` | `3` | 连接级失败的重试次数（Polly，间隔 3 秒） |
| `SubscriptionId` | `null` | 主题共享订阅名；为空时回退入口程序集简单名称，再回退 `DefaultSubscription` |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称，可设为 `NewtonsoftJson` |

```bash
Euonia__Bus__ActiveMq__Connection=activemq:tcp://localhost:61616   # .NET 原生：双下划线表示层级
EUONIA_BUS_ACTIVEMQ_CONNECTION=activemq:tcp://localhost:61616        # 仓库 alias：大写单下划线
Euonia__Bus__ActiveMq__Enabled=true
EUONIA_BUS_ACTIVEMQ_ENABLED=true
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ActiveMqTransporter` | 完整传输实现（`internal`，通过 `AddActiveMqBus` 注册） |
| `ActiveMqBusOptions` | 传输配置 |
| `ActiveMqConsumer` / `ActiveMqSubscriber` / `ActiveMqExecutor` | 单播、多播与请求接收者 |
| `ActiveMqRecipientRegistrar` | 创建并释放接收者 |
| `DefaultPersistentConnection` | 持久连接管理 |
| `AddActiveMqBus(name, configure)` | 注册传输器与键控 `ITransporter` |

## 使用示例

模块化装配与手动装配二选一：

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.ActiveMq;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MessageBusModule), typeof(ActiveMqBusModule))]
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
            .SetStrategy("ActiveMq", strategy =>
            {
                strategy.Add<DistributedMessageTransportStrategy>();
                strategy.EvaluateIncoming((_, _) => true);
                strategy.EvaluateOutgoing((_, _) => true);
            })
            .RegisterChannel(typeof(AppModule).Assembly));
    }
}
```

```csharp
services.AddActiveMqBus("ActiveMq", options =>
{
    options.Connection = "activemq:tcp://localhost:61616";
    options.SubscriptionId = "orders-service";
    options.MaxFailureRetries = 3;
});
services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "ActiveMq");
services.AddConfiguratorBuilder(config => config
    .SetConvention(convention => convention.Add<DefaultMessageConvention>())
    .SetStrategy("ActiveMq", strategy =>
    {
        strategy.Add<DistributedMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));
```

发送时设置优先级：

```csharp
using Nerosoft.Euonia.Bus;

[Unicast]
public sealed class CancelOrder
{
    public long OrderId { get; init; }
}

await bus.Send(new CancelOrder { OrderId = 42 })
    .WithPriority(5)
    .ExecuteAsync(cancellationToken);
```

> ActiveMQ 的队列名默认就是通道名（`ActiveMqDelivery.ResolveQueueName`）；`WithQueue` 会完整替换它，但消费端仍按通道名监听，只有该队列上确有消费者时消息才会被处理。优先级收敛到 NMS 的 0-9，是否生效取决于 broker 的 per-destination policy。

## 注意事项 / 限制

1. **模块开关与手动注册不同**：`ActiveMqBusModule` 只在 `Enabled=true` 且 `Connection` 非空时注册（`Source/Euonia.Bus.ActiveMq/ActiveMqBusModule.cs:18-28`）；直接调用 `AddActiveMqBus` 时 `Enabled` 不参与判断。
2. **`Connection` 必填**：传输器构造时用 `new NMSConnectionFactory(options.Connection!)` 创建连接工厂，为空会在解析时抛异常（`ServiceCollectionExtensions.cs:32-44`）。
3. **队列名由通道直接决定**：与 RabbitMQ 不同，没有 `{prefix}:{channel}@{subscription}` 组合；`SubscriptionId` 只用于主题共享订阅。
4. **请求-响应依赖临时回复队列**：每次调用都会创建并删除临时队列，broker 不可用或权限不足时调用会失败。
5. **重试只覆盖连接级异常**：`SendAsync` 只重试 `SocketException`、`IOException`、`NMSConnectionException`；请求超时不重试，避免 broker 已接收消息导致重复投递。
6. **发布使用持久投递**：`PublishAsync` 固定 `MsgDeliveryMode.Persistent`，无法通过选项关闭。

## 常见问题与故障排查

### 模块加载后没有注册传输器

**原因**：`ActiveMqBusModule` 只在 `Euonia:Bus:ActiveMq:Enabled=true` 且 `Connection` 非空时注册；手动 `AddActiveMqBus` 时 `Enabled` 不参与判断。

**解决**：补齐 `Enabled` 与 `Connection`，或直接调用 `AddActiveMqBus` 并显式赋值 `options.Connection`（`ActiveMqBusModule.cs:18-28`）。

### 请求-响应一直挂起

**原因**：目标队列没有消费者，或 broker 不允许创建临时回复队列；请求发出后没有回复可匹配。

**解决**：先启动消费端；确认账号有临时队列权限；调用侧设置取消令牌/`WithTimeout`（`ActiveMqTransporter.cs:91-152`）。

### 主题（Topic）订阅收不到消息

**原因**：共享订阅名 `SubscriptionId` 不一致，消费端未在对应主题上启动；或消息未被约定识别为多播。

**解决**：固定 `SubscriptionId`（为空时回退入口程序集简单名称，再回退 `DefaultSubscription`）；确认 `SetConvention` 与 `SetStrategy` 覆盖该通道。

### 失败重试没有发生

**原因**：发布路径对任意异常重试 `MaxFailureRetries`；发送/调用路径只重试连接级异常（`SocketException`、`IOException`、`NMSConnectionException`），请求超时不重试。

**解决**：业务失败在处理器内自行重试/补偿；连接失败检查 `MaxFailureRetries` 与 broker 连通性（`ActiveMqTransporter.cs:61-70,109-120`）。

### `WithQueue` 后消息无人消费

**原因**：消费端按通道名监听队列；`WithQueue` 只改发送目标，不会自动创建对应消费者。

**解决**：保持默认队列名（即通道名），或确保消费端也监听该覆盖队列。

## 最佳实践

1. **队列名即通道名**：不要随意 `WithQueue`，除非消费端明确监听同一队列。
2. **每个服务固定共享订阅名**：`SubscriptionId` 保持一致，滚动发布时不会产生重复订阅。
3. **优先级依赖 broker 策略**：`WithPriority` 只写 NMS 优先级，需在 ActiveMQ 端启用 per-destination policy 才真正生效。
4. **持久投递 + 幂等处理**：发布固定 `Persistent`，消费端按 `MessageId` 去重，避免重投导致重复副作用。
5. **连接串走环境变量**：`Euonia__Bus__ActiveMq__Connection` / `EUONIA_BUS_ACTIVEMQ_CONNECTION`，避免把凭据写进仓库。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Euonia.Bus.Abstract`、`Euonia.Core`、`Euonia.Modularity`、`Apache.NMS` 系列、`Polly`。
- **版本策略**：随仓库 `Source/project.props` 统一管理，本文不固定版本号。
- **已知边界**：无死信队列选项；重试仅覆盖连接级异常；主题共享订阅依赖 `SubscriptionId`。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
