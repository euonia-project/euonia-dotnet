# Euonia.Bus.Abstract 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

Euonia 消息总线的契约层：信封与上下文、消息约定、传输策略、注解、序列化、可靠性存储与事件。本页用一个自定义约定 + 自定义策略 + 自定义传输器的端到端示例说明扩展点；完整契约清单见 [项目 README](../../../Source/Euonia.Bus.Abstract/README.md)。

- **NuGet 包**：`Euonia.Bus.Abstract`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus`（与 `Euonia.Bus` 共用）
- **完整契约**：[Source/Euonia.Bus.Abstract/README.md](../../../Source/Euonia.Bus.Abstract/README.md)

## 教程

### 场景：把审计事件路由到自建“记录型”传输器

业务事件需要写入内部审计通道，但不走 broker。做法：自定义约定把 `AuditEvent` 识别为多播，自定义策略只允许 `audit.*` 通道走 `recorder`，再实现一个记录消息的 `ITransporter`。示例依赖 `Euonia.Bus` 引擎与 `Euonia.Bus.InMemory` 作为对照传输器。

### 前置条件

- 已引用 `Euonia.Bus` 与 `Euonia.Bus.InMemory`；
- 控制台或测试项目；
- 无需外部基础设施。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.Abstract
dotnet add package Euonia.Bus.InMemory
```

### 2. 自定义约定与策略

```csharp
using Nerosoft.Euonia.Bus;

public sealed class AuditEvent
{
    public string Action { get; init; } = null!;
}

public sealed class AuditMessageConvention : IMessageConvention
{
    public string Name => "Audit convention";

    public bool IsUnicast(string channel, Type type) => false;

    public bool IsMulticast(string channel, Type type)
        => typeof(AuditEvent).IsAssignableFrom(type);

    public bool IsRequest(string channel, Type type) => false;
}

public sealed class AuditTransportStrategy : ITransportStrategy
{
    public string Name => "Audit strategy";

    public bool Outgoing(string channel, Type type)
        => channel.StartsWith("audit.", StringComparison.OrdinalIgnoreCase);

    public bool Incoming(string channel, Type type) => Outgoing(channel, type);
}
```

约定按“任一命中”组合：`BaseMessageConvention` 会同时询问内置默认约定与这里添加的约定。

### 3. 自定义传输器

```csharp
using System.Collections.Concurrent;
using Nerosoft.Euonia.Bus;

public sealed class RecordingTransporter : ITransporter
{
    private readonly ConcurrentQueue<IMessageEnvelope> _messages = new();

    public string Name => "recorder";

    public event EventHandler<MessageDeliveredEventArgs>? Delivered;

    public IReadOnlyCollection<IMessageEnvelope> Messages => _messages;

    public Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        Delivered?.Invoke(this, new MessageDeliveredEventArgs(message.Payload!, null!));
        return Task.CompletedTask;
    }

    public Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, CancellationToken cancellationToken = default)
        => Task.FromException<TResponse>(new NotSupportedException("recorder only supports publish."));

    public Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, CancellationToken cancellationToken = default)
        => Task.FromException<TResponse>(new NotSupportedException("recorder only supports publish."));
}
```

### 4. 接线并发布

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddInMemoryBus("InMemory");
builder.Services.AddSingleton<RecordingTransporter>();
builder.Services.AddKeyedSingleton<ITransporter>("recorder",
    (provider, _) => provider.GetRequiredService<RecordingTransporter>());

builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AuditMessageConvention>();
    })
    .SetStrategy("InMemory", strategy =>
    {
        strategy.Add<LocalMessageTransportStrategy>();
        strategy.EvaluateIncoming((_, _) => true);
        strategy.EvaluateOutgoing((_, _) => true);
    })
    .SetStrategy("recorder", strategy =>
    {
        strategy.Add<AuditTransportStrategy>();
        strategy.EvaluateOutgoing((_, _) => false);
        strategy.EvaluateIncoming((_, _) => false);
    }));

var host = builder.Build();
await host.StartAsync();

using var scope = host.Services.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IBus>();
var recorder = scope.ServiceProvider.GetRequiredService<RecordingTransporter>();

await bus.Publish(new AuditEvent { Action = "order.created" })
         .WithChannel("audit.orders")
         .ExecuteAsync();

Console.WriteLine($"recorded={recorder.Messages.Count}");
await host.StopAsync();
```

### 5. 运行与验证

```bash
dotnet run
# 期望输出：recorded=1
```

### 常见失败点

- **只引用 Abstract 找不到 `RoutedMessage`**：它在 `Euonia.Bus` 中，抽象包只有 `IMessageEnvelope`。
- **自定义约定不生效**：未通过 `SetConvention` 的 `Add`/`Evaluate*` 注册，而是直接替换字段。
- **自定义传输器未被解析**：必须同时注册 `ITransporter` 的键控服务与 `SetStrategy("recorder", ...)`，键名一致。
- **`Publish` 抛 `MessageTypeException`**：约定未把消息识别为多播，检查 `AuditMessageConvention.IsMulticast`。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Bus.Abstract/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/TUTORIAL.md)
- [Euonia.Bus.InMemory 用户手册](../Euonia.Bus.InMemory/TUTORIAL.md)
- [Euonia.Bus.RabbitMq 用户手册](../Euonia.Bus.RabbitMq/TUTORIAL.md)
- [仓库根 README](../../../README.md)
