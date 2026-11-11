# Euonia.Repository

Euonia 的持久化无关仓储抽象：定义实体契约、`IRepository<TEntity>` 系列接口、上下文提供者（`IContextProvider` / `IContextFactory`）与分页排序扩展。具体实现由 EF Core 或 MongoDB 包提供。

Persistence-agnostic repository abstractions for Euonia: entity contracts, the `IRepository<TEntity>` family, context providers (`IContextProvider` / `IContextFactory`) and paging/sorting extensions. Concrete implementations are supplied by the EF Core and MongoDB packages.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository`
- **NuGet 包**：`Euonia.Repository`

## 安装

```bash
dotnet add package Euonia.Repository
```

选择并添加一个实现：

```bash
dotnet add package Euonia.Repository.EfCore
# 或
dotnet add package Euonia.Repository.Mongo
```

## 主要功能

- **分层仓储接口**：`IRepository<TEntity>`、`IRepository<TEntity, TKey>`、`IRepository<TContext, TEntity, TKey>`。
- **统一 CRUD**：`InsertAsync`、`UpdateAsync`、`DeleteAsync`、`SaveChangesAsync`，均支持 `autoSave`。
- **查询能力**：`GetAsync`、`FindAsync`、`CountAsync`、`LongCountAsync`、`AnyAsync`、`AllAsync`、`BuildQuery`。
- **附加句柄**：所有查询可传入 `Func<IQueryable<TEntity>, IQueryable<TEntity>>` 追加过滤/排序。
- **上下文管理**：`IContextProvider`、`IContextFactory`、`IRepositoryContext` 与 `AddContextProvider()`。
- **实体契约**：`IEntity`、`IAuditable`、`IHasCreateTime`、`IHasUpdateTime`、`IHasDeleteTime`、`IHasSnowflakeId`、`ITombstone`、`IConcurrentEntity`。
- **分页排序**：`Paginate`、`Sort`、`GetPagedCollectionAsync`。

## 快速开始

声明实体并注入仓储：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class Order : IEntity<long>, IHasCreateTime
{
    public long Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTimeOffset CreateTime { get; set; }

    public object[] GetKeys() => [Id];
}

public sealed class OrderService(IRepository<Order, long> repository)
{
    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public Task<List<Order>> FindRecentAsync(CancellationToken cancellationToken)
        => repository.FindAsync(
            order => order.CreateTime > DateTimeOffset.UtcNow.AddDays(-7),
            query => query.OrderByDescending(order => order.CreateTime),
            cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
        => await repository.InsertAsync(order, autoSave: true, cancellationToken);
}
```

注册上下文提供者：

```csharp
using Nerosoft.Euonia.Repository;

services.AddContextProvider();
```

分页查询：

```csharp
using Nerosoft.Euonia.Repository;

var page = await db.Orders
    .Where(order => order.Number.StartsWith("SO"))
    .Paginate(page: 1, size: 20, sorts: "CreateTime desc")
    .ToListAsync(cancellationToken);
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `IRepository<TEntity>` | 基础仓储（查询、CRUD、分页） |
| `IRepository<TEntity, TKey>` | 增加按键查询 |
| `IRepository<TContext, TEntity, TKey>` | 绑定上下文的仓储 |
| `IRepositoryContext` | 数据上下文契约（连接、事务、保存） |
| `IContextProvider` / `IContextFactory` | 解析或创建上下文 |
| `IConnectionStringResolver` | 连接字符串解析 |
| `RepositoryExtensions` | `Paginate`、`Sort`、`GetPagedCollectionAsync` |
| `RepositoryModule` | 注册仓储基础服务 |

## 相关包

- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 实现
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Domain`](../Euonia.Domain/README.md) —— 实体与聚合

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
