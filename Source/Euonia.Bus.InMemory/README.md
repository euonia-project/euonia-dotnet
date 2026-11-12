# Euonia.Bus.InMemory

Euonia 总线的进程内传输：基于容器级强/弱引用信使实现多播发布、单播发送与请求-响应，不依赖任何外部基础设施。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.InMemory`
- **NuGet 包**：`Euonia.Bus.InMemory`
- **依赖**：`Euonia.Bus.Abstract`、`Euonia.Core`、`Euonia.Modularity`

## 安装

```bash
dotnet add package Euonia.Bus.InMemory
```

## 快速开始

### 模块化装配

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MessageBusModule), typeof(InMemoryBusModule))]
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
            .SetStrategy("InMemory", strategy =>
            {
                strategy.Add<LocalMessageTransportStrategy>();
                strategy.EvaluateIncoming((_, _) => true);
                strategy.EvaluateOutgoing((_, _) => true);
            })
            .RegisterChannel(typeof(AppModule).Assembly));
    }
}
```

`InMemoryBusModule` 会绑定 `Euonia:Bus:InMemory` 并按 `Name` 注册传输器（`Source/Euonia.Bus.InMemory/InMemoryBusModule.cs:18-30`）。`Name` 默认是 `InMemory`，与上面的 `SetStrategy("InMemory", ...)` 对应。

### 手动 DI 装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

services.AddEuoniaBus();
services.AddInMemoryBus("InMemory", options =>
{
    options.MultipleSubscriberInstance = false;
});
services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .SetStrategy("InMemory", strategy =>
    {
        strategy.Add<LocalMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .RegisterChannel(typeof(Program).Assembly));
```

`AddInMemoryBus` 的签名是 `AddInMemoryBus(string name, Action<InMemoryBusOptions>? configureOptions = null)`（`Source/Euonia.Bus.InMemory/ServiceCollectionExtensions.cs:24`），`name` 必填且同时作为键控 `ITransporter` 的键。

## 主要功能

- **完整 `ITransporter` 实现**：`PublishAsync` 走弱引用信使，`SendAsync` / `CallAsync` 走强引用信使并等待处理结果（`Source/Euonia.Bus.InMemory/InMemoryTransporter.cs:70-258`）。
- **容器级隔离**：每个服务容器通过 `InMemoryMessengerProvider` 共享一对信使实例，避免进程级单例把消息投递给不相关的容器（`Source/Euonia.Bus.InMemory/InMemoryMessengerProvider.cs:6-21`）。
- **作用域复用**：单播/请求复用调用方作用域，多播发布创建新作用域，二者由 `MessageExecutionDescriptor` 区分（`InMemoryTransporter.cs:283-297`）。
- **接收者生命周期**：`InMemoryRecipientRegistrar` 单例持有接收者并在停机时注销，不使用进程级 `Reset()`（`InMemoryRecipientRegistrar.cs:154-163`）。
- **可观测**：每次投递触发 `ITransporter.Delivered` 事件。

## 核心类型

| 类型 | 作用 |
|------|------|
| `InMemoryTransporter` | 公开的 `ITransporter` 实现，作用域生命周期 |
| `InMemoryBusOptions` | 传输名称与接收者行为选项 |
| `InMemoryBusModule` | 模块入口：绑定配置节并注册传输器 |
| `InMemoryMessengerProvider` | 提供容器级 `WeakReferenceMessenger` / `StrongReferenceMessenger` |
| `InMemoryRecipientRegistrar` | 按约定创建并注册单播/多播/请求接收者 |
| `InMemoryConsumer` / `InMemorySubscriber` / `InMemoryExecutor` | 单播、多播、请求接收者 |
| `WeakReferenceMessenger` / `StrongReferenceMessenger` | 内置信使实现 |

## 配置项

`InMemoryBusOptions` 绑定配置节 `Euonia:Bus:InMemory`（`Source/Euonia.Bus.InMemory/Constants.cs:16`）。

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `Name` | `string` | `InMemory` | 传输器名称，同时作为 `SetStrategy` 与默认传输器的匹配键 |
| `LazyInitialize` | `bool` | `true` | 预留选项；当前实现未读取该值（`InMemoryBusOptions.cs:13-17`） |
| `MaxConcurrentCalls` | `int` | `1` | 预留选项；当前实现未读取该值（`InMemoryBusOptions.cs:19-22`） |
| `MultipleSubscriberInstance` | `bool` | `false` | `true` 时每个注册项创建独立接收者；`false` 时按接收者类型复用单例（`InMemoryRecipientRegistrar.cs:136-138`） |

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "InMemory",
      "InMemory": {
        "Name": "InMemory",
        "MultipleSubscriberInstance": false
      }
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

## 使用示例

```csharp
using Nerosoft.Euonia.Bus;

[Multicast]
public sealed class OrderCreated
{
    public long OrderId { get; init; }
}

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

```csharp
public sealed class OrderService(IBus bus)
{
    public Task PublishAsync(long orderId, CancellationToken cancellationToken)
        => bus.Publish(new OrderCreated { OrderId = orderId }).ExecuteAsync(cancellationToken);

    public Task<OrderStatus> QueryAsync(long orderId, CancellationToken cancellationToken)
        => bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = orderId })
              .ExecuteAsync(cancellationToken);
}
```

> 上面的消息使用 `[Multicast]` / `[Request]` 特性，因此配置器里必须注册 `AnnotationMessageConvention`；若改用 `IMulticast` / `IRequest<T>` 标记接口，则 `DefaultMessageConvention` 即可。

## 注意事项 / 限制

1. **仅限单进程**：所有投递都在当前进程的 DI 容器内完成，跨进程、跨容器不会收到消息；多实例部署时每个实例各自独立。
2. **发布是即发即忘**：`PublishAsync` 只把消息交给弱引用信使并返回已完成任务，处理器抛出的异常不会回传给发布方（`InMemoryTransporter.cs:70-80`）。
3. **没有接收者时会一直等待**：单播/请求依赖接收者触发 `Completed` / `Failed`；若通道没有注册处理器，`SendAsync` / `CallAsync` 会挂起，务必传入取消令牌（`CallAsync` 还可用 `WithTimeout`）。
4. **传输器是作用域服务**：`InMemoryTransporter` 注册为 `Scoped`（`ServiceCollectionExtensions.cs:45`），不要注入到单例服务，否则会在容器校验或运行期失败。
5. **无持久化**：进程退出即丢失全部未处理消息，也不提供发件箱/收件箱存储；需要可靠性时注册 `Euonia.Bus` 的内存存储或改用 broker 传输。
6. **`LazyInitialize` / `MaxConcurrentCalls` 目前不生效**：这两个属性仅为兼容保留，修改它们不会改变行为。

## 常见问题与故障排查

### `CallAsync` / `SendAsync` 一直挂起

**原因**：目标通道没有注册接收者（未 `RegisterChannel`、约定不识别该消息），单播/请求依赖接收者触发 `Completed` / `Failed`。

**解决**：确认 `SetConvention` 能识别消息类型、`RegisterChannel` 覆盖了处理器所在程序集；调用时传入取消令牌，`CallAsync` 可配合 `WithTimeout`（`InMemoryTransporter.cs:90-129`）。

### `PublishAsync` 返回后处理器没有执行

**原因**：多播是即发即忘，接收者把消息投递到自己的后台串行泵；处理器异常只写日志并调用 `context.Failure`，不会回传给发布方。

**解决**：确认通道上有 `InMemorySubscriber` 注册；在测试里用信号量/`TaskCompletionSource` 等待，而不是发布后立即断言（`InMemoryRecipient.cs:74-80,165-181`）。

### 同一条消息被处理两次

**原因**：同一进程同时启用了多个传输器（例如 InMemory + RabbitMq），策略对同一通道都返回 `true`，多播消息会向每个传输器各投递一次；也可能是业务侧重复发布。

**解决**：用 `EvaluateOutgoing` / `EvaluateIncoming` 按通道或消息类型限定唯一传输器；消费端按 `MessageId` 幂等；`MultipleSubscriberInstance` 保持默认 `false`。

### 把 `InMemoryTransporter` 注入单例服务时报作用域错误

**原因**：传输器注册为 `Scoped`（`ServiceCollectionExtensions.cs:45`），不能从根容器或单例构造函数解析。

**解决**：在请求/消息作用域内解析，或改为注入 `IBus`（同样 Scoped）。

### 新建的 `ServiceProvider` 收不到另一个容器的消息

**原因**：信使按容器隔离，`InMemoryMessengerProvider` 为每个容器提供独立的一对强弱引用信使，这是刻意设计。

**解决**：跨容器/跨进程改用 RabbitMQ 或 ActiveMQ；不要尝试共享进程级 `WeakReferenceMessenger.Default`。

## 最佳实践

1. **开发与单测固定用 InMemory**：设置 `Euonia:Bus:DefaultTransporter=InMemory`，测试无需外部 broker。
2. **测试里显式注册通道并等待异步完成**：`AddConfiguratorBuilder(c => c.RegisterChannel(typeof(Test).Assembly))`，处理器内用 `TaskCompletionSource` 通知断言。
3. **默认保持 `MultipleSubscriberInstance=false`**：只有确实需要同一通道多个独立订阅者实例时才开启。
4. **所有调用传取消令牌**：无接收者时不会自动失败，取消令牌是唯一的兜底。
5. **生产环境不要依赖进程内可靠性**：InMemory 无持久化，进程退出即丢失；需要重试/死信请改用 broker 或外置存储。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Euonia.Bus.Abstract`、`Euonia.Core`、`Euonia.Modularity`、`Microsoft.Extensions.Options`。
- **版本策略**：随仓库 `Source/project.props` 统一管理，本文不固定版本号。
- **已知边界**：`LazyInitialize`、`MaxConcurrentCalls` 仅保留属性、当前实现不读取；无持久化、无跨进程投递。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎与约定/策略配置
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约与信封
- [`Euonia.Bus.RabbitMq`](../Euonia.Bus.RabbitMq/README.md) —— 需要跨进程时改用 RabbitMQ
- [`Euonia.Bus.ActiveMq`](../Euonia.Bus.ActiveMq/README.md) —— ActiveMQ 传输
- [`Euonia.Bus.HealthChecks`](../Euonia.Bus.HealthChecks/README.md) —— 总线健康检查

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
