# Euonia.Bus.RabbitMq API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 总线的 RabbitMQ 传输：fanout 交换机做多播、队列做单播与请求-响应，内置持久连接、发布确认、优先级、死信与重试。本页用一个可本机复现的生产者/消费者场景说明接线；完整选项见 [项目 README](../../../Source/Euonia.Bus.RabbitMq/README.md)。

- **NuGet 包**：`Euonia.Bus.RabbitMq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.RabbitMq`
- **完整选项**：[Source/Euonia.Bus.RabbitMq/README.md](../../../Source/Euonia.Bus.RabbitMq/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.RabbitMq/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/API.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/API.md)
- [仓库根 README](../../../README.md)
