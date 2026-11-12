# Euonia.Repository.Mongo 用户手册

`Euonia.Repository.Mongo` 是仓储抽象的 MongoDB 实现：提供 `MongoDbContext`、`DataContextBase<TContext>`、`MongoRepository<,,>` 与流式模型配置。

- **包名**：`Euonia.Repository.Mongo`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.Mongo`
- **完整选项与边界**：[Source/Euonia.Repository.Mongo/README.md](../../Source/Euonia.Repository.Mongo/README.md)

## 教程：用 MongoDB 仓储保存订单

这个场景在本地 MongoDB 中建立 `orders` 集合，配置 `ObjectId` 字符串主键，通过 `AddMongoRepository` 注册上下文与仓储，再用 `IRepository<SalesDbContext, Order, string>` 完成插入、按键查询与条件查询。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 本地 MongoDB 已启动（默认 `mongodb://localhost:27017`）；事务场景需要副本集。
- 已安装 `Euonia.Repository.Mongo`。

### 1. 创建项目并安装

```bash
dotnet new web -n OrdersApi
cd OrdersApi
dotnet add package Euonia.Repository.Mongo
```

### 2. 实体与上下文

```csharp
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.Mongo;

public sealed class Order : IEntity<string>
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Number { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public object[] GetKeys() => [Id];
}

public sealed class SalesDbContext(IMongoDatabase database, ILoggerFactory loggerFactory)
    : DataContextBase<SalesDbContext>(database, loggerFactory)
{
    protected override bool EnabledPublishEvents => true;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.For<Order>(profile =>
        {
            profile.ToCollection("orders");
            profile.HasKey(order => order.Id).GenerateOnInsert(true);
            profile.HasProperty(order => order.Number).HasElementName("number").Required(true);
        });
    }
}
```

### 3. 模块接线与仓储服务

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.Mongo;

[DependsOn(typeof(RepositoryModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMongoRepository<SalesDbContext>(options =>
        {
            options.UseConnection("mongodb://localhost:27017")
                   .UseDatabase("euonia_sales");
        });

        context.Services.AddScoped<OrderService>();
    }
}

public sealed class OrderService(IRepository<SalesDbContext, Order, string> repository)
{
    public Task<Order?> GetAsync(string id, CancellationToken cancellationToken)
        => repository.GetAsync(id, cancellationToken);

    public Task<List<Order>> FindAsync(CancellationToken cancellationToken)
        => repository.FindAsync(order => order.Amount > 0, null, cancellationToken);

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        await repository.InsertAsync(order, autoSave: false, cancellationToken);
        return order;
    }
}
```

### 4. 运行与验证

```csharp
app.MapPost("/orders", async (OrderService service, CancellationToken cancellationToken) =>
{
    var order = new Order { Number = "SO-1", Amount = 99.5m };
    return Results.Created("/orders", await service.CreateAsync(order, cancellationToken));
});

app.MapGet("/orders/{id}", async (string id, OrderService service, CancellationToken cancellationToken) =>
{
    var order = await service.GetAsync(id, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});
```

```bash
dotnet run
curl -X POST http://localhost:5000/orders
curl http://localhost:5000/orders/<返回的Id>
```

预期集合中出现 `number: "SO-1"` 的文档；GET 按 24 位十六进制字符串取回同一条文档。

### 常见失败点

- **仓储无法解析**：`AddMongoRepository` 不注册 `IContextProvider`；模块依赖 `RepositoryModule` 或显式 `AddContextProvider()`。
- **按键查询抛格式异常**：`MongoRepository` 使用 `ObjectId.Parse(entity.Id.ToString())`，`TKey` 必须是 24 位十六进制字符串或 `ObjectId`。
- **事务失败**：单机 `mongod` 不支持多文档事务；需要副本集或分片集群。
- **回滚无效**：`SaveChangesAsync` 固定返回 0，写入在仓储调用时已发送；只有显式开启会话事务后提交/回滚才有意义。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `DataContextBase<TContext>` | MongoDB 上下文基类，管理会话与事务 | `EnabledPublishEvents` 必须重写 |
| `MongoDbContext` | 集合、查询、插入、更新、删除入口 | `Collection<T>()`、`Find<T>()` |
| `MongoRepository<TContext, TEntity, TKey>` | MongoDB 仓储实现 | `TKey` 需可解析为 `ObjectId` |
| `MongoDbOptions` | 连接串与数据库名 | `UseConnection`、`UseDatabase` |
| `AddMongoRepository<TContext>(Action<MongoDbOptions>)` | 注册 `IMongoDatabase`、上下文与开放式仓储 | `contextLifeTime` 默认 `Scoped` |
| `AddMongoRepository<TContextService, TContextImplementation>(...)` | 指定服务/实现类型 | 适合接口上下文 |
| `AddMongoRepository(ServiceLifetime)` | 只注册开放式泛型仓储 | 不注册上下文 |
| `ModelBuilder.For<T>(Action<ModelProfile<T>>)` | 配置集合、键与属性 | 每个上下文类型执行一次 |
| `ModelProfile<T>.ToCollection` / `HasKey` / `HasProperty` | 集合名、键、属性映射 | 未配置集合名时用类型名 |
| `ModelKeyBuilder.GenerateOnInsert(bool)` | 是否在插入时生成 Id | 默认 `true` |
| `ModelPropertyBuilder.HasElementName` / `Required` | 文档字段名与必填 | 可链式调用 |
| `ObjectIdSerializer` | `ObjectId` 与字符串/12 字节互转 | 无法表示时抛异常 |
| `SnowflakeIdValueGenerator` / `SequentialGuidValueGenerator` | 值生成器 | 通过 `IValueGenerator` 使用 |
| `MongoTenantFilters` / `MultiTenantMongoDataContextBase<TContext>` | 多租户过滤器与上下文 | 依赖 `Euonia.Tenancy` |

### 配置

本包不读取 `IConfiguration`，没有 `Euonia:Repository:Mongo` 配置节；`AddMongoRepository` 只接受 `Action<MongoDbOptions>` 回调，不需要 `appsettings.json` 或环境变量。

```csharp
services.AddMongoRepository<SalesDbContext>(
    options => options.UseConnection("mongodb://localhost:27017").UseDatabase("euonia_sales"),
    contextLifeTime: ServiceLifetime.Scoped);
```

`IMongoDatabase` 以单例注册，连接串与数据库名必须在首次解析前设置。

## 相关文档

- [Source README：完整安装、选项与边界](../../Source/Euonia.Repository.Mongo/README.md)
- [Euonia.Repository 用户手册](./Euonia.Repository.md) —— 仓储抽象
- [Euonia.Repository.EfCore 用户手册](./Euonia.Repository.EfCore.md) —— EF Core 实现
- [Euonia.Uow 用户手册](./Euonia.Uow.md) —— 事务边界与 Mongo 限制
- [Euonia.Domain 用户手册](./Euonia.Domain.md) —— 实体契约
- [仓库根 README](../../README.md)
