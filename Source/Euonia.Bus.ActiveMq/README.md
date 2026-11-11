# Euonia.Bus.ActiveMq

Euonia 总线的 Apache ActiveMQ 传输：基于 NMS 实现主题（Topic）发布/订阅、队列点对点发送，以及通过临时回复队列与关联 ID 完成请求-响应。支持持久连接与 Polly 重试。

Apache ActiveMQ transport for Euonia Bus: topic-based publish/subscribe and queue-based point-to-point messaging over NMS, with request-response handled through temporary reply queues and correlation ids, plus persistent connections and Polly-based retries.

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

services.AddActiveMqBus("activemq", options =>
{
    options.Connection = "activemq:tcp://localhost:61616";
    options.SubscriptionId = "orders-service";
    options.MaxFailureRetries = 3;
});

services.Configure<MessageBusOptions>(options =>
{
    options.DefaultTransporter = "activemq";
});
```

也可以从配置节绑定（模块会自动读取）：

```json
{
  "Euonia": {
    "Bus": {
      "ActiveMq": {
        "Enabled": true,
        "Name": "activemq",
        "Connection": "activemq:tcp://localhost:61616",
        "SubscriptionId": "orders-service",
        "MaxFailureRetries": 3
      }
    }
  }
}
```

发送消息时覆盖目标队列与优先级：

```csharp
await bus.Send(new CancelOrder { OrderId = 42 })
    .WithQueue("orders.priority")
    .WithPriority(5)
    .ExecuteAsync();
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Enabled` | `false` | 是否启用（配置节绑定模式） |
| `Name` | `ActiveMq` | 传输器名称 |
| `Connection` | `null` | ActiveMQ 连接字符串 |
| `MaxFailureRetries` | `3` | 失败重试次数 |
| `SubscriptionId` | `null` | 订阅标识，用于推导队列名 |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称，可设为 `NewtonsoftJson` |

## 核心类型

| 类型 | 作用 |
|------|------|
| `ActiveMqTransporter` | 完整传输实现 |
| `ActiveMqBusOptions` | 传输配置 |
| `ActiveMqConsumer` / `ActiveMqSubscriber` / `ActiveMqExecutor` | 单播、多播与请求接收者 |
| `ActiveMqRecipientRegistrar` | 创建并释放接收者 |
| `DefaultPersistentConnection` | 持久连接管理 |
| `AddActiveMqBus(name, configure)` | 注册传输器与键控 `ITransporter` |

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
