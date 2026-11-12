# Euonia.Repository.EfCore 用户手册

`Euonia.Repository.EfCore` 是仓储抽象的 EF Core 实现：提供 `DataContextBase<TContext>`、`EfCoreRepository<,,>`、连接串解析与工作单元上下文工厂。

- **包名**：`Euonia.Repository.EfCore`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.EfCore`
- **完整选项与边界**：[Source/Euonia.Repository.EfCore/README.md](../../Source/Euonia.Repository.EfCore/README.md)

## 教程：带预加载的 EF Core 订单查询

这个场景用 SQLite 建一个订单/订单行模型，通过 `AddEfCoreRepository` 注册上下文，注入 `IRepository<SalesDbContext, Order, long>`，用 `Include` 与 `Tracking(false)` 查询最近订单。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已安装 `Euonia.Repository.EfCore` 与 `Microsoft.EntityFrameworkCore.Sqlite`。
- 示例使用 `EnsureCreated`，生产环境应改用迁移。

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
    public DateTime CreatedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];

    public object[] GetKeys() => [Id];

    public static Order Create(string number) => new()
    {
        Id = ObjectId.Snowflake(),
        Number = number,
        CreatedAt = DateTime.UtcNow
    };
}

public sealed class OrderLine : IEntity<long>
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }

    public object[] GetKeys() => [Id];
}

[ConnectionString(Name = "Default")]
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options)
    : DataContextBase<SalesDbContext>(options)
{
    protected override bool AutoSetEntryValues => false;

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId);

        base.OnModelCreating(modelBuilder);
    }
}
```

### 3. 模块接线与服务

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
    public Task<List<Order>> FindRecentAsync(CancellationToken cancellationToken)
        => repository
            .Include(order => order.Lines)
            .Tracking(false)
            .FindAsync(order => order.CreatedAt > DateTime.UtcNow.AddDays(-7), null, cancellationToken);

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
    var order = Order.Create("SO-1");
    order.Lines.Add(new OrderLine { Id = ObjectId.Snowflake(), ProductName = "SKU-1", Quantity = 2 });
    return Results.Created("/orders", await service.CreateAsync(order, cancellationToken));
});

app.MapGet("/orders/recent", async (OrderService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.FindRecentAsync(cancellationToken)));
```

```bash
dotnet run
curl -X POST http://localhost:5000/orders
curl http://localhost:5000/orders/recent
```

预期最近订单列表包含刚创建的订单及其订单行；关闭跟踪后返回对象不再被上下文跟踪，适合只读查询。

### 常见失败点

- **连接串解析失败**：本示例直接传连接串；若改用 `AddDataContextFactory`，它要求 `{provider}://` 前缀并注册键控 `ConnectionConfigurator`，缺少前缀抛 `ArgumentException`，provider 未注册抛 `NotSupportedException`。
- **仓储无法解析**：`AddEfCoreRepository` / `AddDataContextFactory` 不注册 `IContextProvider`；模块依赖 `RepositoryModule` 或显式 `AddContextProvider()`。
- **实体配置不生效**：`DataContextBase` 只加载带 `[DbContext(typeof(SalesDbContext))]` 的 `IEntityTypeConfiguration`。
- **单例生命周期报错**：`DbContext` 保持 `Scoped`，不要把仓储或上下文注册为 `Singleton`。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `DataContextBase<TContext>` | EF Core 上下文基类，实现 `IRepositoryContext` | `AutoSetEntryValues` 必须重写 |
| `EfCoreRepository<TContext, TEntity, TKey>` | EF Core 仓储实现 | 通过 `IContextProvider` 取上下文 |
| `AddEfCoreRepository<TContext>(Action<DbContextOptionsBuilder>)` | 注册上下文、仓储与 `UnitOfWorkContextFactory` | `contextLifeTime` 默认 `Scoped` |
| `AddEfCoreRepository<TContextService, TContextImplementation>(...)` | 指定服务/实现类型 | 适合接口上下文 |
| `AddDataContextFactory<TContext>(Func<IServiceProvider, string?>?)` | 按连接串解析注册 `IDbContextFactory<TContext>` | 支持名称回退与 `{provider}://` |
| `ConnectionConfigurator` | 使用连接串配置 `DbContextOptionsBuilder` | 委托签名 `(builder, connection)` |
| `UnitOfWorkContextFactory` | 事务性工作单元复用上下文并开启事务 | internal，由 DI 注册 |
| `DataContextExtensions.GetTableName` | 蛇形小写表名 | `OrderDetail` → `order_detail` |
| `DataContextExtensions.SetTombstoneQueryFilter` | 为 `ITombstone` 加软删除过滤器 | 命名过滤器 |
| `RepositoryExtensions.Include` / `IncludeIf` | 预加载导航属性 | 字符串或表达式 |
| `RepositoryExtensions.Tracking` | 切换 `AsTracking` / `AsNoTracking` | `tracking=true` |
| `RepositoryExtensions.Attach` | 附加已存在实体 | 上下文必须是 `DbContext` |
| `UniversalTimeConverter` | 统一 UTC 时间转换 | 值转换器 |
| `SnowflakeIdValueGenerator` / `GuidVersion7ValueGenerator` / `UlidValueGenerator` | 值生成器 | 生成主键 |
| `MultiTenantDataContextBase<TContext>` / `TenantConnectionStringResolver<TContext>` | 多租户上下文与连接串 | 依赖 `Euonia.Tenancy` |

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

本示例用 `AddEfCoreRepository` 直接传连接串；若改用 `AddDataContextFactory`，连接串需带 `{provider}://` 前缀（如 `sqlite://Data Source=sales.db`），并注册键控 `ConnectionConfigurator`。

## 相关文档

- [Source README：完整安装、选项与边界](../../Source/Euonia.Repository.EfCore/README.md)
- [Euonia.Repository 用户手册](./Euonia.Repository.md) —— 仓储抽象
- [Euonia.Repository.Mongo 用户手册](./Euonia.Repository.Mongo.md) —— MongoDB 实现
- [Euonia.Uow 用户手册](./Euonia.Uow.md) —— 工作单元与事务
- [Euonia.Auditing.EfCore 用户手册](./Euonia.Auditing.EfCore.md) —— 审计库的 EF Core 实现
- [仓库根 README](../../README.md)
