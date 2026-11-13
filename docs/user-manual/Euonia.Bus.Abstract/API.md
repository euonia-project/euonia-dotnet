# Euonia.Bus.Abstract API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 消息总线的契约层：信封与上下文、消息约定、传输策略、注解、序列化、可靠性存储与事件。本页用一个自定义约定 + 自定义策略 + 自定义传输器的端到端示例说明扩展点；完整契约清单见 [项目 README](../../../Source/Euonia.Bus.Abstract/README.md)。

- **NuGet 包**：`Euonia.Bus.Abstract`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`（与 `Euonia.Bus` 共用）
- **完整契约**：[Source/Euonia.Bus.Abstract/README.md](../../../Source/Euonia.Bus.Abstract/README.md)

## API 参考

### 核心类型

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `IMessageEnvelope` / `IMessageEnvelope<T>` | 消息信封：ID、通道、授权、用户、元数据、负载 | `Payload` 为负载 |
| `IMessageContext` / `MessageContext` | 处理器上下文：元数据、头、回复与失败回调 | `Response`、`Failure`、`Complete` |
| `ITransporter` | 传输契约 | `PublishAsync`、`SendAsync<T,TResponse>`、`CallAsync<T,TResponse>` |
| `IMessageConvention` | 消息分类（单播/多播/请求） | `IsUnicast`、`IsMulticast`、`IsRequest` |
| `IMessageConventionBuilder` | 组合约定 | `Add<T>()`、`EvaluateUnicast/Multicast/Request` |
| `ITransportStrategy` | 出站/入站选择 | `Outgoing(channel, type)`、`Incoming(channel, type)` |
| `ITransportStrategyBuilder` | 组合策略 | `Add<T>()`、`EvaluateOutgoing`、`EvaluateIncoming` |
| `IHandlerContext` | 按通道执行处理器 | `HandleAsync(channel, message, context, ct)` |
| `IOutboxStore` / `IInboxStore` / `IDeadLetterStore` | 可靠性存储契约 | `Insert`、`MarkAsFailed`、`GetFailedMessages` |
| `IDeadLetterService` | 死信查询/重放/丢弃 | `GetAll()`、`ReplayAsync`、`Discard` |
| `ChannelAttribute` / `SubscribeAttribute` / `EnqueueAttribute` | 通道、订阅、队列注解 | `[Channel("orders")]` |
| `UnicastAttribute` / `MulticastAttribute` / `RequestAttribute` | 消息分类注解 | `[Request(typeof(TResult))]` |
| `DispatchInAttribute` / `ReceiveInAttribute` | 限定分发/接收传输器 | `[DispatchIn("RabbitMq")]` |
| `MessageHeaders` | 标准头常量 | `x-message-id`、`x-correlation-id` 等 |
| `MessageProperties` | 投递属性读写 | `SetQueue`、`SetPriority` |
| `IMessageSerializer` | 序列化契约 | `Serialize<T>`、`DeserializeEnvelope` |
| `ChannelRegistration` / `ChannelHandler` | 通道注册信息与处理器描述 | `MessageType`、`Handlers` |

### 配置说明

本包没有配置节，也没有 `*Options` 类型：约定、策略、序列化器与存储都通过代码/DI 注册。可配置项来自使用方的 `Euonia:Bus` 等配置节，参见 [Euonia.Bus 用户手册](../Euonia.Bus/API.md)。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Bus.Abstract/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/API.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/API.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/API.md)
- [仓库根 README](../../../README.md)
