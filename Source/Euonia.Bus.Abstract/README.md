# Euonia.Bus.Abstract

Euonia 消息总线的抽象契约层：消息信封与上下文、消息分类约定、传输策略、注解、序列化、可靠性存储（发件箱 / 收件箱 / 死信）以及事件体系。它是所有总线传输与 `Euonia.Bus` 引擎的扩展基础。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`
- **NuGet 包**：`Euonia.Bus.Abstract`

## 安装

```bash
dotnet add package Euonia.Bus.Abstract
```

## 主要功能

- **消息分类**：标记接口 `IUnicast`、`IMulticast`、`IRequest<TResponse>`，或对应的 `[Unicast]`、`[Multicast]`、`[Request]` 特性。
- **信封与上下文**：`IMessageEnvelope<T>` 携带 MessageId、CorrelationId、ConversationId、Channel、Authorization、User、Metadata 与 Payload；`IMessageContext` 暴露同样的元数据与请求头，并提供 `Response`、`Failure` 回复入口。
- **传输契约**：`ITransporter` 定义 `PublishAsync`（多播）、`SendAsync`（单播，可返回响应）与 `CallAsync`（请求-响应）。
- **约定与策略**：`IMessageConvention`、`ITransportStrategy` 及其组合构建器，决定消息走本地还是分布式传输。
- **可靠性**：`IOutboxStore`、`IInboxStore`、`IDeadLetterStore`、`IDeadLetterService` 与对应条目类型。
- **接收者契约**：`IRecipientRegistrar`、`IRecipient`、`IConsumer`、`ISubscriber`、`IExecutor`。
- **事件与异常**：投递、接收、确认、回复、处理等事件参数，以及分层异常类型。

## 快速开始

定义消息并声明其分类：

```csharp
using Nerosoft.Euonia.Bus;

// 发布-订阅（多播）
[Multicast]
public sealed class OrderCreated
{
    public long OrderId { get; init; }
}

// 点对点（单播）
[Unicast]
public sealed class CancelOrder
{
    public long OrderId { get; init; }
}

// 请求-响应
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
```

也可以使用标记接口：

```csharp
public sealed class PaymentReceived : IMulticast
{
    public long OrderId { get; init; }
}
```

自定义传输器时实现 `ITransporter`：

```csharp
public sealed class MyTransporter : ITransporter
{
    public string Name => "my-transport";

    public event EventHandler<MessageDeliveredEventArgs>? Delivered;

    public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ITransporter` | 传输抽象：发布、发送、调用 |
| `IMessageEnvelope` / `IMessageEnvelope<T>` | 消息信封与元数据 |
| `IMessageContext` / `MessageContext` | 处理器执行上下文与响应/失败回调 |
| `RoutedMessage` / `RoutedMessage<T>` / `RoutedMessage<T, TResponse>` | 路由消息基类与泛型消息（定义于 `Euonia.Bus`） |
| `IMessageSerializer` | 序列化契约 |
| `IMessageConvention` / `IMessageConventionBuilder` | 消息分类约定及其构建器 |
| `ITransportStrategy` / `ITransportStrategyBuilder` | 出站/入站传输选择策略及其构建器 |
| `IOutboxStore` / `IInboxStore` / `IDeadLetterStore` | 可靠性存储契约 |
| `IDeadLetterService` | 死信查询、重放与丢弃 |
| `IRecipientRegistrar` / `IRecipient` / `IConsumer` / `ISubscriber` / `IExecutor` | 接收者契约 |
| `ChannelAttribute` / `SubscribeAttribute` / `EnqueueAttribute` | 通道、订阅与队列注解 |

> 约定与策略的默认实现或基类（如 `AnnotationMessageConvention`、`BaseTransportStrategy`）位于 [`Euonia.Bus`](../Euonia.Bus/README.md)。

## 使用示例

自定义消息约定与传输策略，并交给 `IConfigurator` 组合：

```csharp
using Nerosoft.Euonia.Bus;

// 自定义约定：按类型名后缀分类，与内置默认约定按「任一命中」组合
public sealed class SuffixMessageConvention : IMessageConvention
{
    public string Name => "Suffix convention";

    public bool IsUnicast(string channel, Type type)
        => type.Name.EndsWith("Command", StringComparison.Ordinal);

    public bool IsMulticast(string channel, Type type)
        => type.Name.EndsWith("Event", StringComparison.Ordinal);

    public bool IsRequest(string channel, Type type)
        => type.Name.EndsWith("Query", StringComparison.Ordinal);
}

// 自定义策略：只允许指定命名空间下的消息走该传输
public sealed class NamespaceTransportStrategy : ITransportStrategy
{
    public string Name => "Namespace strategy";

    public bool Outgoing(string channel, Type type)
        => type.Namespace?.StartsWith("MyApp.Contracts", StringComparison.Ordinal) == true;

    public bool Incoming(string channel, Type type) => Outgoing(channel, type);
}
```

```csharp
services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<SuffixMessageConvention>();
        convention.EvaluateMulticast((_, type) => type.Name.EndsWith("IntegrationEvent", StringComparison.Ordinal));
    })
    .SetStrategy("RabbitMq", strategy =>
    {
        strategy.Add<NamespaceTransportStrategy>();
        strategy.Add(new AnnotationTransportStrategy(["RabbitMq"]));
        strategy.EvaluateOutgoing((channel, type) => !string.IsNullOrWhiteSpace(channel));
        strategy.EvaluateIncoming((channel, type) => !string.IsNullOrWhiteSpace(channel));
    })
    .SetChannelResolver(type => type.FullName!, replaceDefault: true)
    .RegisterChannel(typeof(Program).Assembly));
```

实现自定义可靠性存储时按契约补齐必选成员即可（`IOutboxStore` 另有带默认实现的 `GetAndCache`、`GetPendingMessages(DateTime)`、`Cleanup(DateTime)`）：

```csharp
using Nerosoft.Euonia.Bus;

public sealed class MyOutboxStore : IOutboxStore
{
    public bool Insert(OutboxEntry entry) => throw new NotImplementedException();

    public void MarkAsSuccess(string messageId, string transport) => throw new NotImplementedException();

    public void MarkAsFailed(string messageId, string transport, string errorMessage) => throw new NotImplementedException();

    public void MarkAsDeadLettered(string messageId, string transport, string errorMessage) => throw new NotImplementedException();

    public OutboxEntry? Get(string messageId) => throw new NotImplementedException();

    public IReadOnlyList<OutboxTransport> GetFailedMessages() => throw new NotImplementedException();
}
```

`Insert(IMessageEnvelope, IReadOnlyList<string>)`、`GetAndCache`、`ClearCache`、`GetPendingMessages(DateTime)` 与 `Cleanup(DateTime)` 都有默认实现，可按需覆盖。

## 注意事项 / 限制

1. **与 `Euonia.Bus` 共用根命名空间**：两个程序集都是 `Nerosoft.Euonia.Bus`，`using Nerosoft.Euonia.Bus;` 会同时导入二者；但类型归属仍按程序集区分，例如 `RoutedMessage` 只在 `Euonia.Bus` 中，仅引用本包时不可用。
2. **本包不含传输器与默认约定/策略**：`MessageContext` 等少数类型是具体实现，但传输器、序列化器、发件箱/收件箱/死信存储的可用实现位于 `Euonia.Bus` 或具体传输包。
3. **约定与策略会被并发调用**：`StrategicDispatcher` 与 `BaseMessageConvention` 会缓存判定结果，自定义实现必须线程安全且避免在求值中做阻塞 I/O。
4. **`IMessageContext` 是 `IDisposable`**：处理器不应把上下文保存到字段或缓存中，处理结束后其生命周期由总线管理。
5. **信封字段可能为空**：`Authorization`、`User`、`RequestTraceId` 在请求作用域外为 `null`，消费端必须判空后再使用。
6. **可靠性存储需自行保证幂等**：`IOutboxStore.Insert` 对重复 `MessageId` 应返回 `false`，`IInboxStore` 需按消息标识去重，否则 at-least-once 语义会退化为重复处理。

## 常见问题与故障排查

### 只引用 `Euonia.Bus.Abstract` 时找不到 `RoutedMessage`

**原因**：`RoutedMessage` / `RoutedMessage<T>` / `RoutedMessage<T, TResponse>` 定义在 `Euonia.Bus`，抽象包只声明 `IMessageEnvelope` 契约。

**解决**：引用 `Euonia.Bus`，或只面向 `IMessageEnvelope<T>` 编程。

### 自定义 `IHandler<T>` 没有被调用

**原因**：只把处理器注册进 DI（`AddMessageHandler`）还不够，通道注册来自 `RegisterChannel(...)` 或 `Euonia:Bus:AutoLoadAssemblies`；方法签名也必须是 `HandleAsync(TMessage, IMessageContext, CancellationToken)`。

**解决**：在 `AddConfiguratorBuilder` 中调用 `config.RegisterChannel(typeof(Program).Assembly)`；确认处理器实现了 `IHandler<,>` 且不是抽象类。

### 自定义 `IMessageConvention` 不生效

**原因**：约定未通过 `SetConvention` 加入构建器；或首次判定后直接替换了字段而没有经过 `IMessageConventionBuilder`（构建器才会重置判定缓存）。

**解决**：统一用 `convention.Add<TConvention>()` 或 `Evaluate*` 注册（`BaseMessageConvention.cs:137-165`）。

### 自定义 `IOutboxStore` 编译不通过

**原因**：`Insert(OutboxEntry)`、`MarkAsSuccess`、`MarkAsFailed`、`MarkAsDeadLettered`、`Get`、`GetFailedMessages` 是抽象成员；`Insert(IMessageEnvelope, IReadOnlyList<string>)` 等只是带默认实现的便捷重载。

**解决**：按 `IOutboxStore` 的必选成员补齐；重复 `MessageId` 的 `Insert` 返回 `false`，终态必须走 `MarkAsDeadLettered`。

### 消费端读到的 `Authorization` / `User` 为 `null`

**原因**：请求作用域外（后台任务、定时任务）没有这些信息，接口声明为可空。

**解决**：判空后再使用；需要跨进程身份时由发送端显式写入 `Metadata` 或自定义头。

## 最佳实践

1. **把消息契约放进独立程序集**：生产端与消费端共享同一份消息类型与 `[Channel]` 定义，避免通道名/类型名漂移。
2. **约定与策略写成纯函数**：`IsUnicast` / `Outgoing` 会在分发热路径上被调用并缓存，不要在其中做 I/O 或依赖可变全局状态。
3. **自定义存储必须幂等且可清理**：`Insert` 对重复键返回 `false`，并实现 `Cleanup(DateTime)` 防止存储无界增长。
4. **不要缓存 `IMessageContext`**：它实现 `IDisposable`，生命周期由总线管理，处理结束即失效。
5. **序列化器按名称解析**：内置键为 `SystemTestJson` 与 `NewtonsoftJson`，传输器通过 `SerializerProvider` 选择，自定义序列化器需用相同键注册。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Euonia.Core`、`Microsoft.Extensions.DependencyInjection.Abstractions`。
- **版本策略**：随仓库 `project.props` 统一管理，本文不固定版本号。
- **升级注意**：与 `Euonia.Bus` 共用根命名空间，类型归属按程序集区分；仅引用本包不会得到传输器、默认约定/策略或可靠性存储实现。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线运行时引擎
- [`Euonia.Bus.InMemory`](../Euonia.Bus.InMemory/README.md) —— 进程内传输
- [`Euonia.Bus.RabbitMq`](../Euonia.Bus.RabbitMq/README.md) —— RabbitMQ 传输
- [`Euonia.Bus.ActiveMq`](../Euonia.Bus.ActiveMq/README.md) —— ActiveMQ 传输
- [`Euonia.Bus.Http`](../Euonia.Bus.Http/README.md) / [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— 远程调用传输

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
