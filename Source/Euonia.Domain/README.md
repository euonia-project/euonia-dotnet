# Euonia.Domain

Euonia 的领域驱动设计（DDD）构建块：实体、聚合根、值对象、领域事件、命令与审计契约；包本身不依赖具体持久化技术，可单独用于领域层建模。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Domain`
- **NuGet 包**：`Euonia.Domain`

## 安装

```bash
dotnet add package Euonia.Domain
```

## 主要功能

- **实体**：`Entity` / `Entity<TKey>` 提供类型化标识、瞬态判断与基于标识的相等性。
- **聚合根**：`Aggregate<TKey>` 管理领域事件（`RaiseEvent`、`GetEvents`、`GetAndClearEvents`、`LoadFromHistory`）。
- **值对象**：`ValueObject<T>` 以反射比较公共属性，自动实现 `Equals`、`GetHashCode` 与 `==`/`!=`。
- **领域事件**：`DomainEvent` 可挂载聚合、生成 `EventAggregate` 快照，并携带序列号、意图与来源。
- **命令**：`Command<TData>` 自动生成 `CommandId`，并提供可扩展的 `Properties` 字典。
- **审计契约**：`AuditedAttribute`、`AuditingRecord`、`IAuditingStore` 与 `AuditingOptions`（旧版占位，见注意事项）。
- **缓存标记**：`EntityCacheAttribute`、`IEntityCache` 供上层缓存组件识别实体。

## 快速开始

```csharp
using Nerosoft.Euonia.Domain;

public sealed class Order : Aggregate<long>
{
    private Order()
    {
    }

    public string Number { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public static Order Create(string number, decimal amount)
    {
        var order = new Order
        {
            Id = ObjectId.Snowflake(),
            Number = number,
            Amount = amount
        };

        order.RaiseEvent(new OrderCreatedEvent(order));
        return order;
    }

    public void ChangeAmount(decimal amount)
    {
        Amount = amount;
        RaiseEvent(new OrderAmountChangedEvent(this, amount));
    }
}

public sealed class OrderCreatedEvent : DomainEvent
{
    public OrderCreatedEvent(Order order) => Attach(order);
}

public sealed class OrderAmountChangedEvent : DomainEvent
{
    public OrderAmountChangedEvent(Order order, decimal amount)
    {
        Attach(order);
        Amount = amount;
    }

    public decimal Amount { get; }
}
```

值对象：

```csharp
public sealed class Address : ValueObject<Address>
{
    public string Country { get; init; } = null!;

    public string City { get; init; } = null!;

    public string Street { get; init; } = null!;
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `Entity` / `Entity<TKey>` | 实体基类，含标识与相等性语义 |
| `Aggregate<TKey>` | 聚合根基类，管理领域事件 |
| `ValueObject<T>` | 值对象基类，按公共属性比较 |
| `Event` / `DomainEvent` / `ApplicationEvent` | 事件基类、领域事件与集成事件标记 |
| `EventAggregate` | 事件持久化快照（Id、EventId、Timestamp、TypeName、Payload、Sequence） |
| `Command` / `Command<TData>` | 命令基类，自动生成 `CommandId` |
| `CommandStatus` | 命令执行状态（`Succeed`、`Failure`、`Canceled`） |
| `IAggregateRoot<TKey>` / `IHasDomainEvents` / `IDomainService` | 聚合根、事件载体与领域服务契约 |
| `EntityCacheAttribute` / `IEntityCache` | 实体缓存标记 |
| `AuditedAttribute` / `AuditingRecord` / `IAuditingStore` | 旧版审计契约，与 `Euonia.Auditing` 同名不同命名空间 |

## 选项（代码配置）

本包不读取 `IConfiguration`，没有 `Euonia:Domain` 配置节，也没有需要绑定的运行期选项。

- `Nerosoft.Euonia.Domain.AuditingOptions` 是空类型，仅为兼容旧代码保留；实际审计选项是 `Nerosoft.Euonia.Auditing.AuditingOptions`。
- 实体/值对象/事件的行为全部由类型本身与派生类重写决定，不需要配置。

## 使用示例

聚合收集事件后由应用层统一分发（示例依赖 `Euonia.Repository` 包）：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class OrderAppService
{
    private readonly IRepository<Order, long> _orders;

    public OrderAppService(IRepository<Order, long> orders)
    {
        _orders = orders;
    }

    public async Task<long> CreateAsync(string number, decimal amount, CancellationToken cancellationToken)
    {
        var order = Order.Create(number, amount);

        // 仓储只负责持久化；事件在提交前取出，交给总线或领域事件分发器。
        await _orders.InsertAsync(order, autoSave: true, cancellationToken);

        var events = order.GetAndClearEvents();
        await DispatchAsync(events, cancellationToken);

        return order.Id;
    }

    private static Task DispatchAsync(IReadOnlyList<DomainEvent> events, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

命令：

```csharp
public sealed class CreateOrderCommand : Command<CreateOrderData>
{
    public CreateOrderCommand(CreateOrderData data)
    {
        Data = data;
    }
}

public sealed class CreateOrderData
{
    public string Number { get; init; } = null!;

    public decimal Amount { get; init; }
}
```

## 注意事项 / 限制

1. `ValueObject<T>` 只比较公共属性；嵌套值对象属性按**引用**比较（`left is TValueObject` 时走 `ReferenceEquals`），需要值语义时应存原始字段或重写 `Equals`。
2. `Entity<TKey>` 的相等性要求运行时类型完全相同；EF Core 动态代理等派生类型与实体类型比较时可能不相等。
3. `Aggregate.RaiseEvent` 只把事件加入本地队列并调用 `Register<T>` 注册的处理器，不会自动发布到消息总线；需由应用层或工作单元取出后分发。
4. `Aggregate.Register<T>` 是 `protected`，应在派生类构造函数中注册处理器；`LoadFromHistory` 只重放已注册处理器，不会重新入队。
5. `DomainEvent.GetEventAggregate()` 每次调用都会生成新的 `EventAggregate`（新的 Id 与 Timestamp），它是快照而非持久化实体。
6. 本包的 `AuditedAttribute` / `AuditingRecord` / `IAuditingStore` / `AuditingOptions` 与 `Euonia.Auditing` 中的同名类型**不是同一类型**；应用层审计请使用 `Nerosoft.Euonia.Auditing`。
7. `ValueObject<T>.GetHashCode()` 基于公共属性计算；属性可变时不要把值对象用作字典键或哈希集合元素。

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层与用例
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 持久化无关仓储
- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计内核
- [`Euonia.Linq`](../Euonia.Linq/README.md) —— 规约与查询扩展
- [`Euonia.Core`](../Euonia.Core/README.md) —— `ObjectId` 等基础类型

## 常见问题与故障排查

### `RaiseEvent` 之后消息总线没有收到事件
`Aggregate.RaiseEvent` 只把事件加入本地 `_events` 队列并调用 `Register<T>` 注册的处理器，不会自动发布到总线。应用层需要在保存前后调用 `GetAndClearEvents()`，再交给总线或领域事件分发器；`ClearEvents()` 会直接丢弃未处理事件。

### 两个属性相同的值对象却不相等
`ValueObject<T>` 用反射比较公共属性，但嵌套值对象属性走引用比较（`left is TValueObject` 时使用 `ReferenceEquals`）。把嵌套值对象拆成原始字段、重写 `Equals`，或确保比较的是同一实例。

### EF Core 加载的实体与内存实体不相等
`Entity<TKey>.Equals` 要求 `GetType()` 完全相同；EF Core 动态代理是派生类型，与普通实体比较会返回 `false`。不要在代理对象上依赖实体相等性，改用 `Id` 比较。

### 标注了 `[Audited]` 却没有审计记录
本包的 `AuditedAttribute` 位于 `Nerosoft.Euonia.Domain`，只是旧版契约，不会被 `Euonia.Application` 的审计拦截器识别；应用层审计请使用 `Nerosoft.Euonia.Auditing.AuditedAttribute`，并确认 `AddAuditing` 与存储已注册。

### `Register<T>` 的处理器没有被调用
`Register<T>` 是 `protected`，必须在派生类构造函数中调用；`LoadFromHistory` 只重放已注册处理器，不会重新入队，也不处理未注册的事件类型。检查注册顺序与事件运行时类型是否完全匹配。

## 最佳实践

- 在聚合构造函数中 `Register<T>` 事件处理器，业务方法只 `RaiseEvent`；应用层在持久化后统一 `GetAndClearEvents()` 并分发。
- 值对象保持属性不可变，嵌套值对象尽量扁平化；需要集合语义时重写 `Equals`/`GetHashCode`。
- 实体相等性只用于同一运行时类型；跨代理/跨层比较使用 `Id`。
- 审计统一使用 `Nerosoft.Euonia.Auditing` 的契约与特性，不要混用 Domain 中的旧版同名类型。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Core`、`Euonia.Linq`、`Microsoft.Extensions.DependencyInjection` / `Logging.Abstractions`；具体版本由仓库构建属性决定。
- 已知边界：领域事件不自动发布；`ValueObject<T>` 嵌套值对象按引用比较；Domain 的审计类型与 `Euonia.Auditing` 同名但不同命名空间。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。