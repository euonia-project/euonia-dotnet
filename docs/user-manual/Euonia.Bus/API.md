# Euonia.Bus API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 消息总线的运行时引擎：消息分类、传输策略路由、处理器调用、管道、发件箱/收件箱与遥测。本页是 API 速查；安装与完整选项见 [项目 README](../../../Source/Euonia.Bus/README.md)。

- **NuGet 包**：`Euonia.Bus`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`
- **完整选项**：[Source/Euonia.Bus/README.md](../../../Source/Euonia.Bus/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus/README.md)
- [Euonia.Bus.Abstract 用户手册](../Euonia.Bus.Abstract/API.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/API.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/API.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/API.md)
- [仓库根 README](../../../README.md)
