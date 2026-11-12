# Euonia.Bus

Euonia 消息总线运行时引擎：按消息约定给消息分类，按传输策略把消息路由到已注册的传输器，并提供处理器发现、管道中间件、发件箱/收件箱可靠性与 OpenTelemetry 遥测。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`
- **NuGet 包**：`Euonia.Bus`
- **直接依赖**：`Euonia.Bus.Abstract`、`Euonia.Pipeline`、`Euonia.Modularity`、`Euonia.Telemetry`、`Euonia.Tenancy`、`Euonia.Auditing`

## 安装

```bash
dotnet add package Euonia.Bus
```

本包只提供引擎，不包含传输器。实际使用至少再安装一个传输包：

```bash
dotnet add package Euonia.Bus.InMemory   # 单进程、测试
dotnet add package Euonia.Bus.RabbitMq   # 跨进程
```

## 快速开始

### 方式一：模块化装配（推荐）

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MessageBusModule), typeof(InMemoryBusModule))]
public sealed class OrderModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConfiguratorBuilder(config => config
            .SetConvention(convention =>
            {
                convention.Add<DefaultMessageConvention>();    // 识别 IUnicast / IMulticast / IRequest<>
                convention.Add<AnnotationMessageConvention>(); // 识别 [Unicast] / [Multicast] / [Request]
            })
            .SetStrategy("InMemory", strategy =>
            {
                strategy.Add<LocalMessageTransportStrategy>();
                strategy.EvaluateIncoming((_, _) => true);
                strategy.EvaluateOutgoing((_, _) => true);
            })
            .RegisterChannel(typeof(OrderModule).Assembly));
    }
}
```

- `MessageBusModule` 在 `AheadConfigureServices` 中把 `MessageBusOptions` 绑定到 `Euonia:Bus`（`Source/Euonia.Bus/MessageBusModule.cs:18-20`）。
- `InMemoryBusModule` 绑定 `Euonia:Bus:InMemory` 并按 `Name` 注册传输器（`Source/Euonia.Bus.InMemory/InMemoryBusModule.cs:18-30`）。

### 方式二：手动 DI 装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;

services.AddEuoniaBus();
services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
services.AddInMemoryBus("InMemory");
services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
services.AddConfiguratorBuilder(config => config
    .SetConvention(convention => convention.Add<DefaultMessageConvention>())
    .SetStrategy("InMemory", strategy =>
    {
        strategy.Add<LocalMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));
```

> `AddEuoniaBus()` 只注册引擎服务（`IConfigurator`、`IBus`、`IDispatcher`、`ServiceActivator` 等，见 `Source/Euonia.Bus/ServiceCollectionExtensions.cs:22-46`），**本身不读配置节**。需要配置节时显式 `BindConfiguration("Euonia:Bus")`，或改用 `MessageBusModule`。
>
> `AddConfiguratorBuilder` 的委托由 `ServiceActivator` 在宿主启动时执行，并且先于 `AutoLoadAssemblies` 的自动扫描（`Source/Euonia.Bus/ServiceActivator.cs:62-70`）。

## 主要功能

- **三种投递语义**：`PublishAsync`（多播）、`SendAsync`（单播，可带 `Subject<TResult>` 回调）、`CallAsync`（请求-响应）。
- **消息约定**：`SetConvention` 组合多个 `IMessageConvention`，决定一条消息属于单播、多播还是请求。
- **传输策略**：`SetStrategy(name, ...)` 为每个传输器名称声明出站/入站断言；`StrategicDispatcher` 按「通道 + 类型」求值并缓存，策略变更时按版本号失效（`Source/Euonia.Bus/Dispatching/StrategicDispatcher.cs:42-68`）。
- **处理器发现**：`RegisterChannel(assembly)` 扫描处理器类型；`AddMessageHandler` 把处理器注册进 DI；`Euonia:Bus:AutoLoadAssemblies` 按程序集简单名称自动扫描。
- **管道中间件**：基于 `Euonia.Pipeline` 的 `IPipelineBehavior<,>`，内置消息日志与发件箱行为。
- **可靠性**：发件箱（Outbox）、收件箱（Inbox）、死信（Dead Letter）三套契约与内存实现。
- **遥测**：`bus.publish`、`bus.send`、`bus.call` 活动与指标（`Source/Euonia.Bus/Telemetry/BusTelemetry.cs`）。

## 核心类型

| 类型 | 作用 |
|------|------|
| `IBus` | 发布、发送、调用的统一入口 |
| `MessageBusOptions` | 总线选项（默认传输器、自动加载、Outbox、Inbox） |
| `IConfigurator` / `ConfiguratorBuilder` | 约定、策略与通道注册的配置入口（接口定义在 `Euonia.Bus.Abstract`） |
| `DefaultMessageConvention` / `AnnotationMessageConvention` | 按标记接口 / 按特性分类消息 |
| `LocalMessageTransportStrategy` / `DistributedMessageTransportStrategy` / `AnnotationTransportStrategy` | 内置传输策略 |
| `RoutedMessage` / `RoutedMessage<T>` / `RoutedMessage<T, TResponse>` | 消息信封实现（定义于本包） |
| `IHandler<TMessage>` / `IHandler<TMessage, TResult>` / `HandlerBase<TMessage>` | 处理器契约与基类 |
| `PublishBuilder<T>` / `SendBuilder<T, TResult>` / `CallBuilder<T, TResult>` | 流式调用构建器 |
| `InMemoryOutboxStore` / `InMemoryInboxStore` / `InMemoryDeadLetterStore` | 内存可靠性实现；死信服务实现 `DeadLetterService` 为 `internal` |
| `ServiceActivator` | 启动接收器、执行配置器委托的 `BackgroundService` |
| `RemoteReceiver` / `RemoteReply<T>` / `RemoteError` | HTTP 与 gRPC 传输共用的线路协议 |

> `RoutedMessage` 系列位于本包而不是 `Euonia.Bus.Abstract`；抽象包只声明 `IMessageEnvelope` 契约。

## 配置项

`MessageBusOptions` 绑定配置节 `Euonia:Bus`（`Source/Euonia.Bus/Constants.cs:11`）。

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `DefaultTransporter` | `string` | `""` | 没有任何策略匹配时回退的传输器名称；为空则抛 `MessageTypeException` |
| `AutoLoadAssemblies` | `string[]?` | `null` | 启动时自动扫描处理器的程序集简单名称；加载失败直接使启动失败 |
| `Outbox:Enabled` | `bool` | `false` | 发件箱全局开关；仍可用消息级 `UseOutbox` 单条开启 |
| `Outbox:PollingInterval` | `TimeSpan` | `00:01:00` | 发件箱失败记录的轮询间隔 |
| `Outbox:MaxRetryAttempts` | `int` | `0` | 最大重试轮次；`0` 或负数表示不限制 |
| `Outbox:PendingReclaimGracePeriod` | `TimeSpan` | `00:05:00` | 回收长时间 Pending 记录的宽限期 |
| `Outbox:RetentionPeriod` | `TimeSpan` | `1.00:00:00` | 已完成/死信条目的保留时长 |
| `Inbox:Enabled` | `bool` | `false` | 收件箱全局开关，也是唯一生效的开关 |
| `Inbox:PollingInterval` | `TimeSpan` | `00:01:00` | 收件箱失败记录的轮询间隔 |
| `Inbox:MaxRetryAttempts` | `int` | `0` | 最大重试轮次；`0` 或负数表示不限制 |
| `Inbox:RetentionPeriod` | `TimeSpan` | `1.00:00:00` | 已完成/死信条目的保留时长 |

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "InMemory",
      "AutoLoadAssemblies": [ "MyApp.Handlers" ],
      "Outbox": { "Enabled": true, "PollingInterval": "00:00:30" },
      "Inbox": { "Enabled": true, "MaxRetryAttempts": 5 }
    }
  }
}
```

```bash
Euonia__Bus__DefaultTransporter=InMemory      # .NET 原生：双下划线表示层级
EUONIA_BUS_DEFAULTTRANSPORTER=InMemory        # 仓库 alias：大写单下划线
Euonia__Bus__Outbox__Enabled=true
EUONIA_BUS_OUTBOX_ENABLED=true
```

## 使用示例

定义消息与处理器：

```csharp
using Nerosoft.Euonia.Bus;

// 多播（事件）；[Channel] 决定注册与分发使用的通道名
[Channel("orders")]
public sealed class OrderCreated : IMulticast
{
    public long OrderId { get; init; }
}

// 单播（命令）
public sealed class CancelOrder : IUnicast
{
    public long OrderId { get; init; }
}

// 请求-响应
public sealed class GetOrderStatus : IRequest<OrderStatus>
{
    public long OrderId { get; init; }
}

public sealed class OrderStatus
{
    public long OrderId { get; init; }
    public string State { get; init; } = null!;
}

public sealed class OrderCreatedHandler : IHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class GetOrderStatusHandler : IHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> HandleAsync(GetOrderStatus message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderStatus { OrderId = message.OrderId, State = "Shipped" });
}
```

发布、发送与调用：

```csharp
public sealed class OrderService(IBus bus)
{
    public Task PublishAsync(long orderId, CancellationToken cancellationToken)
        => bus.Publish(new OrderCreated { OrderId = orderId })
              .ExecuteAsync(cancellationToken);

    public Task CancelAsync(long orderId, CancellationToken cancellationToken)
        => bus.Send(new CancelOrder { OrderId = orderId })
              .ExecuteAsync(cancellationToken);

    public Task<OrderStatus> QueryAsync(long orderId, CancellationToken cancellationToken)
        => bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = orderId })
              .WithTimeout(TimeSpan.FromSeconds(5))
              .ExecuteAsync(cancellationToken);
}
```

启用内存版可靠性组件：

```csharp
services.AddInMemoryOutbox();     // IOutboxStore
services.AddInMemoryInbox();      // IInboxStore
services.AddInMemoryDeadLetters(); // IDeadLetterStore + IDeadLetterService
services.AddOptions<MessageBusOptions>().Configure(options => options.Outbox.Enabled = true);
```

查询与重放死信：

```csharp
public sealed class DeadLetterController(IDeadLetterService deadLetters)
{
    public IReadOnlyList<DeadLetterEntry> List() => deadLetters.GetAll();

    public Task<bool> ReplayAsync(string messageId, CancellationToken cancellationToken)
        => deadLetters.ReplayAsync(messageId, cancellationToken);
}
```

> 使用 `[Unicast]`、`[Multicast]`、`[Request]` 特性而非标记接口时，必须注册 `AnnotationMessageConvention`；默认只有 `DefaultMessageConvention` 生效。

## 注意事项 / 限制

1. **`AddConfiguratorBuilder` 只有第一次生效**：内部用 `TryAddSingleton<ConfiguratorBuilder>` 注册（`Source/Euonia.Bus/ServiceCollectionExtensions.cs:103`），同一容器内重复调用不会叠加，后一次会被静默忽略。
2. **配置器委托在启动时执行一次**：`ServiceActivator` 只在 `ExecuteAsync` 中调用一次委托；运行期再调用 `SetConvention` / `SetStrategy` 不会重新装配接收器（策略判定缓存虽有版本号，但接收器已启动）。
3. **单播消息匹配多个传输器会抛异常**：`StrategicDispatcher.Determine` 对非多播消息返回多个传输器时抛 `MessageTypeException`（`Source/Euonia.Bus/Dispatching/StrategicDispatcher.cs:63-65`）。
4. **没有匹配策略且未设默认传输器会失败**：此时抛 `MessageTypeException`，不会静默丢弃消息。
5. **`WithDelay` 是进程内延迟**：消息在延迟期间只存在于内存，进程退出即丢失；需要持久化延迟应依赖传输器能力或开启发件箱。
6. **发件箱需要存储实现**：开启 `Outbox:Enabled` 或消息级 `UseOutbox` 但未注册 `IOutboxStore` 时，发布直接抛 `MessagePersistentException`。
7. **收件箱只认全局开关**：消息级 `UseInbox` 仅是发送侧标记，内置传输器不会透传，单独设置对消费端去重没有影响。
8. **远程传输只支持请求-响应**：HTTP 与 gRPC 传输器的 `PublishAsync` / `SendAsync` 返回失败任务（`NotSupportedException`），应使用 `CallAsync`。

## 常见问题与故障排查

### 发布消息抛 `MessageTypeException`

**原因**：默认只注册 `DefaultMessageConvention`（识别 `IUnicast` / `IMulticast` / `IRequest<>` 标记接口）；使用 `[Unicast]` 等特性但未注册 `AnnotationMessageConvention`；或没有任何策略匹配且 `DefaultTransporter` 为空；或单播消息同时匹配多个传输器。

**解决**：在 `SetConvention` 中补 `convention.Add<AnnotationMessageConvention>()`；设置 `Euonia:Bus:DefaultTransporter`；单播消息只保留一个出站策略（`StrategicDispatcher.cs:63-65`）。

### `Euonia:Bus` 配置节不生效

**原因**：`AddEuoniaBus()` 只注册引擎服务，不读取配置节；未使用 `MessageBusModule` 也未手动绑定。

**解决**：`services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus")`，或改用 `[DependsOn(typeof(MessageBusModule))]`（`MessageBusModule.cs:18-20`）。

### `EUONIA_BUS_*` 大写单下划线变量不生效

**原因**：该 alias 配置源由 `Euonia.Modularity` 的配置构建注册；普通 `Host.CreateApplicationBuilder` 只认原生双下划线。

**解决**：使用 `HostUtility` / `AddModularityApplication`，或改用 `Euonia__Bus__DefaultTransporter=...`。

### 开启 Outbox 后发布抛 `MessagePersistentException`

**原因**：`Outbox:Enabled=true` 或消息级 `UseOutbox` 生效，但容器中没有 `IOutboxStore`；或同一 `MessageId` 重复插入。

**解决**：注册 `services.AddInMemoryOutbox()` 或自定义存储；发送侧保证 `MessageId` 唯一（`MessageBus.cs:139-150`）。

### HTTP / gRPC 传输器调用 `Publish` / `Send` 抛 `NotSupportedException`

**原因**：远程传输只实现请求-响应，多播/单播没有对应的服务端协议。

**解决**：改用 `CallAsync` 或 `CallBuilder.ExecuteAsync`；需要多播时换用 broker 传输。

## 最佳实践

1. **按边界选传输器**：单进程与测试用 `Euonia.Bus.InMemory`；跨服务事件用 RabbitMQ/ActiveMQ；同步 RPC 用 HTTP/gRPC，并只走 `CallAsync`。
2. **默认传输器显式配置**：即使只注册一个传输器，也设置 `Euonia:Bus:DefaultTransporter`，避免策略漏配时抛 `MessageTypeException`。
3. **Outbox 只覆盖需要至少一次投递的路径**：开启 `Outbox:Enabled` 后为 `PollingInterval`、`MaxRetryAttempts`、`RetentionPeriod` 给出明确值，并用 `AddInMemoryDeadLetters()` 接住终态失败。
4. **消费端做幂等**：至少一次语义下重复不可避免；按业务键或 `MessageId` 去重，或开启 `Inbox:Enabled` 并注册 `IInboxStore`。
5. **环境变量分层部署**：容器/CI 用 `Euonia__Bus__Outbox__Enabled=true`，模块化宿主可用 `EUONIA_BUS_OUTBOX_ENABLED=true`；两种写法不要混用于同一键。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Euonia.Bus.Abstract`、`Euonia.Pipeline`、`Euonia.Modularity`、`Euonia.Telemetry`、`Euonia.Tenancy`、`Euonia.Auditing`；另引用 `Newtonsoft.Json`、`System.Reactive`。
- **版本策略**：包版本随仓库 `Source/project.props` 统一管理，本文不固定具体版本号。
- **升级注意**：`RoutedMessage` 系列在本包而非 `Euonia.Bus.Abstract`；两个包共用 `Nerosoft.Euonia.Bus` 命名空间；`UseInbox` 只是发送侧标记，收件箱去重只由 `Inbox:Enabled` 决定。

## 相关包

- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 信封、约定、策略、可靠性契约
- [`Euonia.Bus.InMemory`](../Euonia.Bus.InMemory/README.md) —— 进程内传输
- [`Euonia.Bus.RabbitMq`](../Euonia.Bus.RabbitMq/README.md) —— RabbitMQ 传输
- [`Euonia.Bus.ActiveMq`](../Euonia.Bus.ActiveMq/README.md) —— ActiveMQ 传输
- [`Euonia.Bus.Http`](../Euonia.Bus.Http/README.md) / [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— 远程请求-响应传输
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
