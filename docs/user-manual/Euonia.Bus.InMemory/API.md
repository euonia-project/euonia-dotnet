# Euonia.Bus.InMemory API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

Euonia 总线的进程内传输：基于容器级强/弱引用信使实现多播、单播与请求-响应，不需要任何外部基础设施。本页演示如何在单进程/单测中可靠地等待异步投递；安装与完整选项见 [项目 README](../../../Source/Euonia.Bus.InMemory/README.md)。

- **NuGet 包**：`Euonia.Bus.InMemory`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.InMemory`
- **完整选项**：[Source/Euonia.Bus.InMemory/README.md](../../../Source/Euonia.Bus.InMemory/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.InMemory/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.Abstract 用户手册](../Euonia.Bus.Abstract/API.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/API.md)
- [仓库根 README](../../../README.md)
