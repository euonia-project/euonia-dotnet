# Euonia.Repository 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Repository` 定义持久化无关的仓储契约、上下文提供者与分页排序扩展；EF Core / MongoDB 包提供具体实现。

- **包名**：`Euonia.Repository`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository`
- **完整选项与边界**：[Source/Euonia.Repository/README.md](../../../Source/Euonia.Repository/README.md)

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

## 相关文档

- [API 参考](API.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Repository/README.md)
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/TUTORIAL.md) —— EF Core 上下文与连接串解析
- [Euonia.Repository.Mongo 用户手册](../Euonia.Repository.Mongo/TUTORIAL.md) —— MongoDB 实现
- [Euonia.Uow 用户手册](../Euonia.Uow/TUTORIAL.md) —— 事务边界
- [Euonia.Domain 用户手册](../Euonia.Domain/TUTORIAL.md) —— 实体契约
- [仓库根 README](../../../README.md)
