# Euonia.Domain

Euonia 的领域驱动设计（DDD）构建块：实体、聚合根、值对象、领域事件、命令与审计契约。包本身不依赖具体持久化技术，可单独用于领域层建模。

Domain-Driven Design building blocks for Euonia: entities, aggregate roots, value objects, domain events, commands and auditing contracts. The package is persistence-agnostic and can be used on its own for domain modeling.

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
- **值对象**：`ValueObject<T>` 以反射比较属性，自动实现 `Equals`、`GetHashCode` 与 `==`/`!=`。
- **领域事件**：`DomainEvent` 可挂载聚合、生成 `EventAggregate` 快照并携带序列号、意图与来源。
- **命令**：`Command<TData>` 自动生成 `CommandId`，并提供可扩展的 `Properties` 字典。
- **审计**：`AuditedAttribute`、`AuditingRecord`、`IAuditingStore` 与 `AuditingOptions`。

## 快速开始

```csharp
using Nerosoft.Euonia;
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
| `ValueObject<T>` | 不可变值对象基类 |
| `DomainEvent` | 领域事件基类，可挂载聚合与生成事件快照 |
| `ApplicationEvent` | 应用层集成事件标记 |
| `EventAggregate` | 事件持久化快照（Id、EventId、Timestamp、TypeName、Payload、Sequence） |
| `Command` / `Command<TData>` | 命令基类，自动生成 `CommandId` |
| `CommandResponse<T>` / `CommandStatus` | 命令执行结果与状态 |
| `IAggregateRoot<TKey>` / `IHasDomainEvents` / `IDomainService` | 聚合根、事件载体与领域服务契约 |
| `AuditedAttribute` / `AuditingRecord` / `IAuditingStore` | 变更审计支持 |

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层与用例
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 持久化无关仓储
- [`Euonia.Linq`](../Euonia.Linq/README.md) —— 规约与查询扩展

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
