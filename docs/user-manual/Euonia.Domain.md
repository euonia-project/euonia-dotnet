# Euonia.Domain 用户手册

`Euonia.Domain` 提供实体、聚合根、值对象、领域事件与命令等 DDD 构建块，不依赖具体持久化技术。

- **包名**：`Euonia.Domain`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Domain`
- **完整选项与边界**：[Source/Euonia.Domain/README.md](../../Source/Euonia.Domain/README.md)

## 教程：订单聚合、值对象与领域事件

这个场景从空控制台项目开始，定义 `Order` 聚合、`OrderLine` 值对象和 `OrderSubmittedEvent`，在聚合内部注册处理器，提交后取出事件并验证。Domain 本身没有 DI 模块，示例在应用模块中注册使用它的服务。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已了解聚合根与值对象的基本概念。
- 示例不访问数据库。

### 1. 创建项目并安装

```bash
dotnet new console -n SalesDomain
cd SalesDomain
dotnet add package Euonia.Domain
```

### 2. 定义值对象、事件与聚合

```csharp
using Nerosoft.Euonia.Domain;

public sealed class OrderLine : ValueObject<OrderLine>
{
    public string ProductId { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total => Quantity * UnitPrice;
}

public sealed class OrderSubmittedEvent : DomainEvent
{
    public OrderSubmittedEvent(Order order)
    {
        Attach(order);
        OrderId = order.Id;
        Total = order.Total;
    }

    public long OrderId { get; }
    public decimal Total { get; }
}

public sealed class Order : Aggregate<long>
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
        // Register 是 protected；只在聚合内部注册处理器。
        Register<OrderSubmittedEvent>(_ => SubmittedCount++);
    }

    public string Number { get; private set; } = null!;
    public IReadOnlyList<OrderLine> Lines => _lines;
    public int SubmittedCount { get; private set; }
    public decimal Total => _lines.Sum(line => line.Total);

    public static Order Create(string number)
        => new() { Id = ObjectId.Snowflake(), Number = number };

    public void AddLine(string productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0 || unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        _lines.Add(new OrderLine
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    public void Submit()
    {
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one line.");
        }

        RaiseEvent(new OrderSubmittedEvent(this));
    }
}
```

### 3. 应用服务与模块接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Modularity;

public sealed class OrderService
{
    public Order CreateSubmittedOrder()
    {
        var order = Order.Create("SO-1");
        order.AddLine("SKU-1", 2, 19.5m);
        order.Submit();
        return order;
    }
}

public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<OrderService>();
    }
}
```

Domain 没有自己的模块类型；把实体与聚合放在领域程序集，由应用模块注册使用它们的服务即可。持久化与事务分别见 [Repository](./Euonia.Repository.md) 与 [Uow](./Euonia.Uow.md) 手册。

### 4. 运行与验证

```csharp
var service = new OrderService();
var order = service.CreateSubmittedOrder();
var events = order.GetAndClearEvents();

Console.WriteLine($"{order.Number}:{order.Total}:{order.SubmittedCount}:{events.Count}");
```

预期输出类似 `SO-1:39.0:1:1`：`Total` 为 39，聚合内处理器把 `SubmittedCount` 加一，`GetAndClearEvents` 返回一个事件。再次调用 `GetAndClearEvents` 应返回空列表。

### 常见失败点

- **总线没收到事件**：`RaiseEvent` 只入本地队列，不会自动发布；应用层必须 `GetAndClearEvents()` 后分发。
- **值对象不相等**：嵌套值对象属性按引用比较；把嵌套对象拆成原始字段或重写 `Equals`。
- **EF 实体不相等**：`Entity<TKey>` 要求运行时类型相同；代理对象与普通实体比较用 `Id`。
- **`[Audited]` 不生效**：Domain 的 `AuditedAttribute` 是旧版契约，应用层审计要用 `Nerosoft.Euonia.Auditing`。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `Entity<TKey>` | 实体基类，提供 `Id`、`GetKeys()`、相等性 | `TKey : IEquatable<TKey>` |
| `Aggregate<TKey>` | 聚合根基类，管理领域事件 | 继承 `Entity<TKey>` |
| `ValueObject<T>` | 值对象基类，按公共属性比较 | `T : ValueObject<T>` |
| `Event` | 事件基类，含 `EventId`、`CorrelationId`、`Sequence` | `Sequence` 默认 `DateTime.UtcNow.Ticks` |
| `DomainEvent` | 领域事件基类，可 `Attach` 聚合 | `GetEventAggregate()` 生成快照 |
| `ApplicationEvent` | 应用层集成事件标记 | 继承 `Event` |
| `EventAggregate` | 事件持久化快照 | `Id`、`EventId`、`Payload`、`EventSequence` |
| `Command` / `Command<TData>` | 命令基类，自动生成 `CommandId` | `Properties` 可扩展 |
| `CommandStatus` | 命令状态 | `Succeed`、`Failure`、`Canceled` |
| `IAggregateRoot<TKey>` | 聚合根契约 | 继承 `IEntity<TKey>` |
| `IHasDomainEvents` | 事件载体契约 | `RaiseEvent`、`Apply`、`ClearEvents` |
| `Aggregate.Register<T>(Action<T>)` | 注册事件处理器 | `protected`，同一类型后注册覆盖 |
| `Aggregate.RaiseEvent<T>(T)` | 入队并调用已注册处理器 | 不自动发布到总线 |
| `Aggregate.GetAndClearEvents()` | 取出并清空事件 | 返回 `IReadOnlyList<DomainEvent>` |
| `Aggregate.LoadFromHistory(IEnumerable<DomainEvent>)` | 重放历史事件 | 只调用已注册处理器，不入队 |
| `EntityCacheAttribute` | 标记实体缓存开关 | `Enabled=true` |
| `AuditedAttribute` / `AuditingRecord` / `IAuditingStore` | 旧版审计契约 | 与 `Euonia.Auditing` 同名不同命名空间 |

### 配置

Domain 没有 `IConfiguration` 读取点，也没有 `Euonia:Domain` 配置节；行为由类型与派生类决定，不需要 `appsettings.json` 或环境变量。

## 相关文档

- [Source README：完整安装、选项与边界](../../Source/Euonia.Domain/README.md)
- [Euonia.Repository 用户手册](./Euonia.Repository.md) —— 聚合持久化
- [Euonia.Uow 用户手册](./Euonia.Uow.md) —— 聚合操作的事务边界
- [Euonia.Auditing 用户手册](./Euonia.Auditing.md) —— 应用层审计
- [仓库根 README](../../README.md)
