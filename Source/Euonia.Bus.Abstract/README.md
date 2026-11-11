# Euonia.Bus.Abstract

Euonia 消息总线的抽象契约层：消息信封与上下文、消息分类约定、传输策略、注解、序列化、可靠性存储（发件箱 / 收件箱 / 死信）以及事件体系。它是所有总线传输与 `Euonia.Bus` 引擎的扩展基础。

Messaging abstractions for Euonia Bus: message envelopes and contexts, conventions, transport strategies, annotations, serialization, reliability stores (outbox, inbox and dead letter) and the event system. This package is the extension base for all bus transports and the `Euonia.Bus` engine.

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
- **传输契约**：`ITransporter` 定义 `PublishAsync`（多播）、`SendAsync`（单播/请求-响应）与 `CallAsync`。
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

> 约定与策略的默认实现（如 `AnnotationMessageConvention`、`BaseTransportStrategy`）位于 [`Euonia.Bus`](../Euonia.Bus/README.md)。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线运行时引擎
- [`Euonia.Bus.InMemory`](../Euonia.Bus.InMemory/README.md) —— 进程内传输
- [`Euonia.Bus.RabbitMq`](../Euonia.Bus.RabbitMq/README.md) —— RabbitMQ 传输
- [`Euonia.Bus.ActiveMq`](../Euonia.Bus.ActiveMq/README.md) —— ActiveMQ 传输
- [`Euonia.Bus.Http`](../Euonia.Bus.Http/README.md) / [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— 远程调用传输

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
