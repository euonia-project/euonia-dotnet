# Euonia.Repository 用户手册

`Euonia.Repository` 定义持久化无关的仓储契约、上下文提供者与分页排序扩展；EF Core / MongoDB 包提供具体实现。

- **包名**：`Euonia.Repository`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository`
- **完整选项与边界**：[Source/Euonia.Repository/README.md](../../Source/Euonia.Repository/README.md)

## 教程：用统一仓储读写订单

这个场景用 SQLite + EF Core 作为具体实现，应用服务只依赖 `IRepository<TContext, TEntity, TKey>`，演示按键查询、条件查询与显式保存。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已安装 `Euonia.Repository.EfCore` 与 SQLite 提供程序。
- 示例不需要外部数据库服务。

### 1. 创建项目并安装

```bash
dotnet new web -n OrdersApi
cd OrdersApi
dotnet add package Euonia.Repository.EfCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

### 2. 实体与上下文

```csharp
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

public sealed class Order : IEntity<long>, IHasCreateTime
{
    public long Id { get; set; }
    public string Number { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }

    public object[] GetKeys() => [Id];

    public static Order Create(string number, decimal amount) => new()
    {
        Id = ObjectId.Snowflake(),
        Number = number,
        Amount = amount,
        CreatedAt = DateTime.UtcNow
    };
}

[ConnectionString(Name = "Default")]
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options)
    : DataContextBase<SalesDbContext>(options)
{
    protected override bool AutoSetEntryValues => false;

    public DbSet<Order> Orders => Set<Order>();
}
```

### 3. 模块接线与仓储服务

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

[DependsOn(typeof(RepositoryModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddEfCoreRepository<SalesDbContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("Default")));
        context.Services.AddScoped<OrderService>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        db.Database.EnsureCreated();
    }
}

public sealed class OrderService(IRepository<SalesDbContext, Order, long> repository)
{
    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public Task<List<Order>> FindRecentAsync(CancellationToken cancellationToken)
        => repository.FindAsync(
            order => order.CreatedAt > DateTime.UtcNow.AddDays(-7),
            query => query.OrderByDescending(order => order.CreatedAt),
            cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        await repository.InsertAsync(order, autoSave: false, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return order;
    }
}
```

### 4. 运行与验证

```csharp
app.MapPost("/orders", async (OrderService service, CancellationToken cancellationToken) =>
{
    var order = await service.CreateAsync(Order.Create("SO-1", 99.5m), cancellationToken);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:long}", async (long id, OrderService service, CancellationToken cancellationToken) =>
{
    var order = await service.GetAsync(id, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});
```

```bash
dotnet run
curl -X POST http://localhost:5000/orders
curl http://localhost:5000/orders/1
```

预期 POST 返回 201 与订单 JSON，GET 返回同一条记录；数据库文件 `sales.db` 由连接串决定。

### 常见失败点

- **连接串没生效**：`AddEfCoreRepository` 直接接收连接串，不解析名称；本示例从 `Configuration.GetConnectionString("Default")` 读取。
- **仓储无法解析**：`AddDataContextFactory` 不会注册 `IContextProvider`；模块必须依赖 `RepositoryModule` 或显式 `AddContextProvider()`。
- **数据提前提交**：`autoSave` 默认 `true`；工作单元内统一 `autoSave: false`。
- **排序不生效**：`Sort` 只接受 `[+|-]属性名`，如 `-CreatedAt`，不要写 `CreatedAt desc`。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `IRepository<TEntity>` | 基础仓储：查询、CRUD、分页 | `autoSave` 默认 `true` |
| `IRepository<TEntity, TKey>` | 增加按键查询 | `TKey : IEquatable<TKey>` |
| `IRepository<TContext, TEntity, TKey>` | 绑定上下文的仓储 | 暴露 `Context` |
| `IRepositoryContext` | 上下文契约：集合、连接、事务、保存 | `SaveChangesAsync`、`BeginTransactionAsync` |
| `IContextProvider` | 解析上下文；可注册自定义工厂 | `GetContext<TContext>()`、`SetFactory` |
| `IContextFactory` | 按 `Order` 尝试创建上下文 | 瞬态注册 |
| `ConnectionStringAttribute` | 声明连接串名称与代码默认值 | `Name`、`Value` |
| `IConnectionStringResolver<TContext>` | 异步解析连接串 | 仅 EF `AddDataContextFactory` 使用 |
| `AddContextProvider()` | 注册默认上下文提供者与工厂 | 提供者 Scoped、工厂 Transient |
| `RepositoryModule` | 模块入口，调用 `AddContextProvider()` | 通过 `[DependsOn]` 引入 |
| `RepositoryExtensions.Paginate` | 分页查询 | `page` 最小 1、`size` 最小 0 |
| `RepositoryExtensions.Sort` | 按 `[+|-]属性名` 排序 | 未知属性忽略 |
| `RepositoryExtensions.GetPagedCollectionAsync` | 返回总数与分页信息 | 总数为未分页统计 |
| `RepositoryTelemetry` | 仓储 ActivitySource 与耗时指标 | 不记录 SQL/参数 |
| `IEntity<TKey>` / `IHasCreateTime` | 实体与时间契约 | `CreatedAt` 为 `DateTime` |

### 配置：ConnectionStrings

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=sales.db"
  }
}
```

```bash
ConnectionStrings__Default="Data Source=sales.db"
CONNECTION_STRING_DEFAULT="Data Source=sales.db"
```

本示例用 `AddEfCoreRepository` 直接传连接串；若改用 `AddDataContextFactory` 走名称回退，连接串需带 `{provider}://` 前缀（如 `sqlite://Data Source=sales.db`）。

## 相关文档

- [Source README：完整安装、选项与边界](../../Source/Euonia.Repository/README.md)
- [Euonia.Repository.EfCore 用户手册](./Euonia.Repository.EfCore.md) —— EF Core 上下文与连接串解析
- [Euonia.Repository.Mongo 用户手册](./Euonia.Repository.Mongo.md) —— MongoDB 实现
- [Euonia.Uow 用户手册](./Euonia.Uow.md) —— 事务边界
- [Euonia.Domain 用户手册](./Euonia.Domain.md) —— 实体契约
- [仓库根 README](../../README.md)
