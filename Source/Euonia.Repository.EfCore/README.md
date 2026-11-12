# Euonia.Repository.EfCore

Euonia 仓储抽象的 Entity Framework Core 实现：提供 `DataContextBase<TContext>` 上下文基类与 `EfCoreRepository<TContext, TEntity, TKey>` 仓储，并通过 `AddEfCoreRepository` 注册上下文、仓储与工作单元上下文工厂。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.EfCore`
- **NuGet 包**：`Euonia.Repository.EfCore`

## 安装

```bash
dotnet add package Euonia.Repository.EfCore
```

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
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

services.AddContextProvider();
services.AddEfCoreRepository<AppDbContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("Default"));
});
```

> 也可使用 `AddEfCoreRepository<TContextService, TContextImplementation>(...)`、带 `IServiceProvider` 的重载，以及只注册开放式泛型仓储的 `AddEfCoreRepository(ServiceLifetime)`。

使用仓储：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class OrderService(IRepository<AppDbContext, Order, long> repository)
{
    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
        => await repository.InsertAsync(order, autoSave: true, cancellationToken);
}
```

## 主要功能

- **上下文基类**：`DataContextBase<TContext>` 实现 `IRepositoryContext`，提供 `SetEntryValues` 与 `ConfigureModel` 钩子。
- **仓储实现**：`EfCoreRepository<TContext, TEntity, TKey>` 基于 `IRepositoryContext` + `DbSet` 完成查询与 CRUD，并记录仓储遥测。
- **统一注册**：`AddEfCoreRepository` 注册 `DbContext` 与开放式泛型仓储，并注册 `UnitOfWorkContextFactory`。
- **连接串解析**：`AddDataContextFactory<TContext>` 按名称、上下文类型名、`Default`、解析器与代码默认值依次解析。
- **工作单元**：`UnitOfWorkContext` / `UnitOfWorkContextFactory` 在事务性工作单元中复用上下文并开启事务。
- **值转换与生成**：`UniversalTimeConverter`、`SnowflakeIdValueGenerator`、`GuidVersion7ValueGenerator`、`UlidValueGenerator` 等。
- **查询扩展**：`Include` / `IncludeIf` / `Tracking` / `Attach`（EF Core 专用）。
- **多租户**：`MultiTenantDataContextBase<TContext>`、`TenantConnectionStringResolver<TContext>` 等。

## 使用示例

按连接串名称解析并注册上下文工厂（`{provider}://{connection}` 前缀用于选择已注册的 `ConnectionConfigurator`）：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

[ConnectionString(Name = "Default")]
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DataContextBase<AppDbContext>(options)
{
    protected override bool AutoSetEntryValues => true;
}

[DependsOn(typeof(RepositoryModule))]
public sealed class PersistModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddKeyedSingleton<ConnectionConfigurator>("sqlite",
            (builder, connection) => builder.UseSqlite(connection));

        context.Services.AddDataContextFactory<AppDbContext>();
    }
}
```

> `AddEfCoreRepository` 只接收 `Action<DbContextOptionsBuilder>`，不读取 `ConnectionStrings`；需要按配置/环境变量解析时使用 `AddDataContextFactory<TContext>`。

## 配置项

`AddDataContextFactory<TContext>()` 的连接串解析顺序：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | 显式 `connectionStringFactory` | 调用方直接返回连接串 |
| 2 | `ConnectionStrings:<ConnectionStringAttribute.Name>` | 上下文特性上的名称 |
| 3 | `ConnectionStrings:<TContext 类型名>` | 如 `ConnectionStrings:AppDbContext` |
| 4 | `ConnectionStrings:Default` | 通用回退 |
| 5 | `IConnectionStringResolver<TContext>` | 异步解析器 |
| 6 | `ConnectionStringAttribute.Value` | 代码默认值 |

```json
{
  "ConnectionStrings": {
    "Default": "sqlite://Data Source=app.db"
  }
}
```

```bash
ConnectionStrings__Default="sqlite://Data Source=app.db"   # .NET 原生
CONNECTION_STRING_DEFAULT="sqlite://Data Source=app.db"   # 仓库 alias
```

### 代码选项

| 成员 | 默认值 | 说明 |
|------|--------|------|
| `AutoSetEntryValues` | 抽象属性 | 保存时是否调用 `SetEntryValues`；派生类必须重写 |
| `DateTimeKind` | `Unspecified` | 派生类在 `SetEntryValues` 中写入日期时间时使用的 Kind |
| `contextLifeTime` | `Scoped` | `AddEfCoreRepository` / `AddDataContextFactory` 的生命周期参数 |
| `SetEntryValues` | 空实现 | 保存前填充审计字段等；基类不做任何填充 |
| `ConfigureModel` | 空实现 | 在软删除过滤器之后追加模型配置 |

> 本包没有 `Euonia:Repository` 配置节；除 `ConnectionStrings` 外均为代码配置。

## 核心类型

| 类型 | 作用 |
|------|------|
| `DataContextBase<TContext>` | EF Core 上下文基类 |
| `EfCoreRepository<TContext, TEntity, TKey>` | EF Core 仓储实现 |
| `UnitOfWorkContext` / `UnitOfWorkContextFactory`（internal） | 工作单元上下文与工厂，由 DI 注册 |
| `AddEfCoreRepository<TContext>(...)` | 注册上下文与仓储 |
| `AddDataContextFactory<TContext>(...)` | 按连接串解析注册 `IDbContextFactory<TContext>` |
| `ConnectionConfigurator` | `(DbContextOptionsBuilder, string) -> DbContextOptionsBuilder` 委托 |
| `DataContextExtensions` | `GetTableName`、`GetForeignKey`、`SetTombstoneQueryFilter` |
| `RepositoryExtensions` | EF Core 专用 `Include` / `IncludeIf` / `Tracking` / `Attach` |
| `UniversalTimeConverter` / `SnowflakeIdValueGenerator` / `GuidVersion7ValueGenerator` / `UlidValueGenerator` | 值转换与值生成器 |

## 注意事项 / 限制

1. `AddEfCoreRepository` **不注册** `IContextProvider`；需要 `[DependsOn(typeof(RepositoryModule))]` 或显式 `services.AddContextProvider()`，否则解析仓储会失败。
2. `AddEfCoreRepository` 不解析 `ConnectionStrings`；要使用名称回退、环境变量与 `{provider}://` 前缀，请改用 `AddDataContextFactory<TContext>()`。
3. `DataContextBase.OnModelCreating` 只应用带 `[DbContext(typeof(TContext))]` 的 `IEntityTypeConfiguration`；没有该特性的配置不会被加载。
4. `AutoSetEntryValues => true` 只会调用 `SetEntryValues`；基类实现为空，审计字段需要派生类自己填充。
5. 软删除过滤器以命名过滤器 `TenantFilterNames.Tombstone` 注册；EF Core 不允许命名与匿名查询过滤器混用。
6. `AddDataContextFactory` 要求连接串带 `{provider}://` 前缀，并为该 provider 注册键控 `ConnectionConfigurator`；否则抛 `NotSupportedException`。
7. `AddEfCoreRepository<TContext>(..., ServiceLifetime.Singleton)` 会把 `DbContext` 注册为单例，`AddEfCoreRepository(ServiceLifetime.Singleton)` 会把开放式仓储注册为单例；两者都与 EF Core 的线程模型冲突，生产环境应保持 `Scoped`。
8. `UnitOfWorkContextFactory` 只在 `IUnitOfWork.Options.IsTransactional` 为 `true` 时开启事务；非事务性工作单元直接解析上下文，不会提交/回滚。

## 相关包

- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储抽象
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Linq`](../Euonia.Linq/README.md) —— 查询与规约
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 多租户过滤器与连接串

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
