# Euonia.Bus.ActiveMq API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

Euonia 总线的 Apache ActiveMQ 传输：主题（Topic）做发布/订阅、队列做点对点与请求-响应，基于 NMS 与持久连接，并用 Polly 处理连接级失败。本页给出本机可复现的端到端场景；完整选项见 [项目 README](../../../Source/Euonia.Bus.ActiveMq/README.md)。

- **NuGet 包**：`Euonia.Bus.ActiveMq`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.ActiveMq`
- **完整选项**：[Source/Euonia.Bus.ActiveMq/README.md](../../../Source/Euonia.Bus.ActiveMq/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.ActiveMq/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/API.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/API.md)
- [仓库根 README](../../../README.md)
