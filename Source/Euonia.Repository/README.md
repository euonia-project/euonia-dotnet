# Euonia.Repository

Euonia 的持久化无关仓储抽象：定义实体契约、`IRepository<TEntity>` 系列接口、上下文提供者（`IContextProvider` / `IContextFactory`）与分页排序扩展；具体实现由 EF Core 或 MongoDB 包提供。

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
- **统一 CRUD**：`InsertAsync`、`UpdateAsync`、`DeleteAsync` 支持 `autoSave`；`SaveChangesAsync` 显式保存挂起变更。
- **查询能力**：`GetAsync`、`FindAsync`、`CountAsync`、`LongCountAsync`、`AnyAsync`、`AllAsync`、`BuildQuery`。
- **附加句柄**：所有查询可传入 `Func<IQueryable<TEntity>, IQueryable<TEntity>>` 追加过滤/排序/预加载。
- **上下文管理**：`IContextProvider`、`IContextFactory`、`IRepositoryContext` 与 `AddContextProvider()`。
- **实体契约**：`IEntity` / `IEntity<TKey>`、`IAuditable<TUser>`、`IHasCreateTime`、`IHasUpdateTime`、`IHasDeleteTime`、`IHasSnowflakeId`、`ITombstone`、`IConcurrentEntity<T>`。
- **分页排序**：`Paginate`、`Sort`、`GetPagedCollectionAsync`。
- **连接串契约**：`ConnectionStringAttribute`、`IConnectionStringResolver<TContext>`。
- **遥测**：`RepositoryTelemetry` 提供 `euonia.repository.execute` 活动与耗时指标。

## 快速开始

声明实体并注入仓储：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class Order : IEntity<long>, IHasCreateTime
{
    public long Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public object[] GetKeys() => [Id];
}

public sealed class OrderService(IRepository<Order, long> repository)
{
    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public Task<List<Order>> FindRecentAsync(CancellationToken cancellationToken)
        => repository.FindAsync(
            order => order.CreatedAt > DateTime.UtcNow.AddDays(-7),
            query => query.OrderByDescending(order => order.CreatedAt),
            cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
        => await repository.InsertAsync(order, autoSave: true, cancellationToken);
}
```

注册上下文提供者：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Repository;

services.AddContextProvider();
```

分页查询：

```csharp
using Nerosoft.Euonia.Repository;

var page = await db.Orders
    .Where(order => order.Number.StartsWith("SO"))
    .Paginate(page: 1, size: 20, sorts: "-CreatedAt")
    .ToListAsync(cancellationToken);
```

## 使用示例

自定义 `IContextFactory` 可以接入非 EF Core / Mongo 的上下文；工厂按 `Order` 升序尝试：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Repository;

public sealed class OrderContextFactory(IServiceProvider provider) : IContextFactory
{
    public int Order => 0;

    public TContext? GetContext<TContext>() where TContext : class, IRepositoryContext
        => provider.GetService<TContext>();
}

services.AddContextProvider();
services.AddTransient<IContextFactory, OrderContextFactory>();
```

也可以直接在提供者上注册工厂（`OrderContext` 为自定义 `IRepositoryContext` 实现）：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class ContextBootstrapper(IContextProvider provider)
{
    public void Configure() => provider.SetFactory(() => new OrderContext());
}
```

## 配置项

本包只定义连接串契约，实际读取发生在 `Euonia.Repository.EfCore` 的 `AddDataContextFactory<TContext>()` 与 EF Core / Mongo 的 `IContextFactory` 实现中。解析顺序（第一个非空白值获胜）：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | 显式 `connectionStringFactory` | 仅 `AddDataContextFactory<TContext>(Func<IServiceProvider, string?>)` |
| 2 | `ConnectionStrings:<ConnectionStringAttribute.Name>` | 特性上的名称，如 `[ConnectionString(Name = "Orders")]` |
| 3 | `ConnectionStrings:<数据上下文类型名>` | 如 `ConnectionStrings:OrderDbContext` |
| 4 | `ConnectionStrings:Default` | 通用回退 |
| 5 | `IConnectionStringResolver<TContext>` | 异步解析器（仅 EF Core 的 `AddDataContextFactory`） |
| 6 | `ConnectionStringAttribute.Value` | 代码默认值，优先级最低 |

```json
{
  "ConnectionStrings": {
    "Default": "sqlite://Data Source=app.db"
  }
}
```

```bash
ConnectionStrings__Default="sqlite://Data Source=app.db"   # .NET 原生：双下划线表示层级
CONNECTION_STRING_DEFAULT="sqlite://Data Source=app.db"   # 仓库 alias：大写单下划线
```

> `{provider}://{connection}` 前缀由 EF Core 的 `AddDataContextFactory` 解析；Mongo 与直接传 `DbContextOptionsBuilder` 的注册方式不经过该前缀。

## 核心类型

| 类型 | 作用 |
|------|------|
| `IRepository<TEntity>` | 基础仓储（查询、CRUD、分页） |
| `IRepository<TEntity, TKey>` | 增加按键查询 |
| `IRepository<TContext, TEntity, TKey>` | 绑定上下文的仓储 |
| `IRepositoryContext` | 数据上下文契约（连接、事务、保存） |
| `IContextProvider` / `IContextFactory` | 解析或创建上下文 |
| `ConnectionStringAttribute` | 声明上下文使用的连接串名称与代码默认值 |
| `IConnectionStringResolver<TContext>` | 按上下文类型异步解析连接串 |
| `RepositoryExtensions` | `Paginate`、`Sort`、`GetPagedCollectionAsync` |
| `RepositoryModule` | 注册 `AddContextProvider()` 的模块 |
| `RepositoryTelemetry` | 仓储 ActivitySource 与耗时指标 |

## 注意事项 / 限制

1. `autoSave` 默认是 `true`；工作单元内应传 `autoSave: false`，由 `IUnitOfWork.CompleteAsync()` 统一保存，否则每次 CRUD 都会立即提交。
2. `IRepository<TEntity>` 继承 `IDisposable`，实现通常是 Scoped；不要把它捕获到单例或跨请求复用。
3. `GetAsync(TKey key)` 通过 `nameof(IEntity<TKey>.Id)` 构造谓词，实体必须实现 `IEntity<TKey>` 并公开 `Id`。
4. `FindAsync(IEnumerable<TKey> keys)` 在键集合为空时直接返回空列表，不会访问数据库。
5. `Sort` 只识别公共实例属性，未知属性名会被静默忽略；`Paginate` 会把小于 1 的页码按 1、小于 0 的页大小按 0 处理。
6. `IRepositoryContext.BeginTransactionAsync` 的默认实现只是调用 `GetConnection().BeginTransaction()` 并丢弃返回值；自定义上下文应重写它，否则事务不会挂到上下文上。
7. `RepositoryTelemetry` 只记录操作名与实体名，不记录 SQL、连接串或参数；没有监听者时开销接近零。

## 相关包

- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 实现
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Domain`](../Euonia.Domain/README.md) —— 实体与聚合
- [`Euonia.Linq`](../Euonia.Linq/README.md) —— 查询与规约
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— `RepositoryModule` 所在框架

## 常见问题与故障排查

### 配置了连接串，但仓储没有使用它
`AddEfCoreRepository<TContext>(options => ...)` 直接接收 `DbContextOptionsBuilder`，不会读取 `ConnectionStrings`。要使用名称回退、环境变量与 `{provider}://` 前缀，改用 `AddDataContextFactory<TContext>()`；同时确认 `[ConnectionString(Name = "...")]` 的名称与配置键一致，且为该 provider 注册了键控 `ConnectionConfigurator`。

### 工作单元回滚了，数据却已经写入
仓储的 `autoSave` 默认是 `true`，每次 `InsertAsync`/`UpdateAsync`/`DeleteAsync` 都会立即保存，工作单元之后无法回滚已提交的数据。工作单元内统一传 `autoSave: false`，由 `IUnitOfWork.CompleteAsync()` 统一保存。

### 排序参数不生效
`Sort` 只接受 `[+|-]属性名` 格式，例如 `-CreatedAt`；写成 `"CreateTime desc"` 或带空格的字符串会被正则忽略，查询保持原顺序。`Paginate` 也会把小于 1 的页码按 1、小于 0 的页大小按 0 处理。

### `GetAsync(key)` 总是返回 `null`
`IRepository<TEntity, TKey>` 按键查询依赖 `IEntity<TKey>.Id` 属性；实体没有实现该接口、`Id` 名称不匹配，或 Mongo 的键不是可解析的 `ObjectId` 字符串时都会查不到；Mongo 的键格式错误会抛 `FormatException`。检查实体契约与键类型，Mongo 场景不要用 `long`/`Guid` 作为 `TKey`。

### 自定义上下文的 `BeginTransactionAsync` 没有开启事务
`IRepositoryContext.BeginTransactionAsync` 的默认实现只是调用 `GetConnection().BeginTransaction()` 并丢弃返回值；自定义上下文必须重写它并保存事务句柄，否则后续 `CommitAsync`/`RollbackAsync` 无法作用于真实事务。

## 最佳实践

- 用稳定的 `[ConnectionString(Name = "Orders")]` 命名连接串，环境差异用 `ConnectionStrings__Orders` 或 `CONNECTION_STRING_ORDERS` 覆盖；需要解析链时统一走 `AddDataContextFactory`。
- 工作单元内 `autoSave: false`，仓储按 Scoped 解析，不要把 `IRepository` 捕获到单例或跨请求复用。
- 分页排序使用 `Paginate(page, size, "-CreatedAt")` 这类格式；对外部传入的页码/页大小先做边界校验。
- 连接串与凭据放环境变量或密钥库，不要写进源码；用 `RepositoryTelemetry` 观察操作耗时，不记录 SQL 与参数。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Linq`、`Euonia.Modularity`、`Euonia.Uow`；具体包版本由仓库构建属性决定。
- 已知边界：连接串解析由 EF Core 适配包与 `IContextFactory` 实现，本包只定义契约；默认 `BeginTransactionAsync` 不保存事务；Mongo 的键与事务语义由 Mongo 适配包决定。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
