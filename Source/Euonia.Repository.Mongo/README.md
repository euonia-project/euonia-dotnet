# Euonia.Repository.Mongo

Euonia 仓储抽象的 MongoDB 实现：提供 `DataContextBase<TContext>`（基于 `MongoDbContext`）与 `MongoRepository<TContext, TEntity, TKey>`，并通过 `AddMongoRepository` 注册上下文与仓储。

MongoDB implementation for Euonia repository abstractions: `DataContextBase<TContext>` (based on `MongoDbContext`) and `MongoRepository<TContext, TEntity, TKey>`, with `AddMongoRepository` registering contexts and repositories.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.Mongo`
- **NuGet 包**：`Euonia.Repository.Mongo`

## 安装

```bash
dotnet add package Euonia.Repository.Mongo
```

## 主要功能

- **上下文基类**：`DataContextBase<TContext>` 实现 `IRepositoryContext`，暴露紧凑的模型构建 API。
- **仓储实现**：`MongoRepository<TContext, TEntity, TKey>` 提供与 EF Core 一致的仓储语义。
- **统一注册**：`AddMongoRepository<TContext>(options)` 注册上下文、仓储与工作单元工厂。
- **连接与数据库**：`MongoDbOptions.UseConnection(...)`、`UseDatabase(...)`。
- **模型配置**：`MongoDbContext` 与 `ModelProfile` / `ModelBuilder` 支持集合映射、键与属性配置。
- **事务**：`BeginTransactionAsync`、`CommitAsync`、`RollbackAsync`（副本集环境）。

## 快速开始

定义上下文：

```csharp
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Nerosoft.Euonia.Repository.Mongo;

public sealed class AppDbContext(IMongoDatabase database, ILoggerFactory loggerFactory)
    : DataContextBase<AppDbContext>(database, loggerFactory)
{
    protected override bool EnabledPublishEvents => true;

    public IMongoCollection<Order> Orders => Database.GetCollection<Order>("orders");
}
```

注册仓储：

```csharp
using Nerosoft.Euonia.Repository.Mongo;

services.AddMongoRepository<AppDbContext>(options =>
{
    options.UseConnection("mongodb://localhost:27017")
           .UseDatabase("euonia_sample");
});
```

使用仓储：

```csharp
public sealed class OrderService(IRepository<AppDbContext, Order, long> repository)
{
    public Task<List<Order>> FindAsync(CancellationToken cancellationToken)
        => repository.FindAsync(
            order => order.Amount > 100,
            query => query.OrderByDescending(order => order.CreateTime),
            cancellationToken);
}
```

## 配置项

| 选项 | 说明 |
|------|------|
| `MongoDbOptions.ConnectionString` | MongoDB 连接字符串（`UseConnection`） |
| `MongoDbOptions.Database` | 数据库名称（`UseDatabase`） |
| `contextLifeTime` | 上下文与仓储生命周期，默认 `Scoped` |

## 核心类型

| 类型 | 作用 |
|------|------|
| `DataContextBase<TContext>` | MongoDB 上下文基类 |
| `MongoDbContext` | 集合与模型配置入口 |
| `MongoRepository<TContext, TEntity, TKey>` | MongoDB 仓储实现 |
| `MongoDbOptions` | 连接与数据库配置 |
| `ModelProfile` / `ModelBuilder` / `ModelKeyBuilder` | 集合与属性映射配置 |
| `UnitOfWorkContext` / `UnitOfWorkContextFactory` | 工作单元 |
| `AddMongoRepository<TContext>(...)` | 注册上下文与仓储 |

## 相关包

- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储抽象
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Domain`](../Euonia.Domain/README.md) —— 实体与聚合

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
