# Euonia.Repository.EfCore

Euonia 仓储抽象的 Entity Framework Core 实现：提供 `DataContextBase<TContext>` 上下文基类与 `EfCoreRepository<TContext, TEntity, TKey>` 仓储，并通过 `AddEfCoreRepository` 一并注册上下文、仓储与工作单元。

Entity Framework Core implementation for Euonia repository abstractions: `DataContextBase<TContext>` context base and `EfCoreRepository<TContext, TEntity, TKey>`, with `AddEfCoreRepository` registering contexts, repositories and unit of work together.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.EfCore`
- **NuGet 包**：`Euonia.Repository.EfCore`

## 安装

```bash
dotnet add package Euonia.Repository.EfCore
```

## 主要功能

- **上下文基类**：`DataContextBase<TContext>` 实现 `IRepositoryContext`，支持审计字段自动填充与日期时间类型控制。
- **仓储实现**：`EfCoreRepository<TContext, TEntity, TKey>` 基于 `IRepositoryContext` + `DbSet` 完成查询与 CRUD。
- **统一注册**：`AddEfCoreRepository<TContext>(options)` 注册 `DbContext` 与开放式泛型仓储。
- **工作单元**：`UnitOfWorkContext` / `UnitOfWorkContextFactory` 提供按仓储共享的上下文。
- **值转换与生成**：内置值转换器与值生成器。
- **分页排序**：复用 `RepositoryExtensions` 的 `Paginate` / `Sort`。

## 快速开始

定义上下文：

```csharp
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository.EfCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DataContextBase<AppDbContext>(options)
{
    protected override bool AutoSetEntryValues => true;

    public DbSet<Order> Orders => Set<Order>();
}
```

注册仓储：

```csharp
using Nerosoft.Euonia.Repository.EfCore;

services.AddEfCoreRepository<AppDbContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("Default"));
});
```

> 也支持 `AddEfCoreRepository<TContextService, TContextImplementation>(...)` 以及带 `IServiceProvider` 的重载。

使用仓储：

```csharp
public sealed class OrderService(IRepository<AppDbContext, Order, long> repository)
{
    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
        => await repository.InsertAsync(order, autoSave: true, cancellationToken);
}
```

## 配置项

| 成员 | 默认值 | 说明 |
|------|--------|------|
| `AutoSetEntryValues` | 派生类实现 | 保存时是否自动填充审计字段 |
| `DateTimeKind` | `Unspecified` | 写入日期时间值使用的 `DateTimeKind` |
| `contextLifeTime` | `Scoped` | 上下文与仓储生命周期 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `DataContextBase<TContext>` | EF Core 上下文基类 |
| `EfCoreRepository<TContext, TEntity, TKey>` | EF Core 仓储实现 |
| `UnitOfWorkContext` / `UnitOfWorkContextFactory` | 工作单元上下文与工厂 |
| `AddEfCoreRepository<TContext>(...)` | 注册上下文与仓储 |
| `DataContextExtensions` | 上下文查询扩展 |
| `ValueConversion` / `ValueGeneration` | 值转换与值生成支持 |

## 相关包

- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储抽象
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Linq`](../Euonia.Linq/README.md) —— 查询与规约

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
