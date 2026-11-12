# Euonia.Repository.Mongo

Euonia 仓储抽象的 MongoDB 实现：提供基于 `MongoDbContext` 的 `DataContextBase<TContext>`、`MongoRepository<TContext, TEntity, TKey>`，以及集合/键/属性的流式模型配置。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.Mongo`
- **NuGet 包**：`Euonia.Repository.Mongo`

## 安装

```bash
dotnet add package Euonia.Repository.Mongo
```

## 快速开始

定义实体与上下文：

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

    public object[] GetKeys() => [Id];
}

public sealed class AppDbContext(IMongoDatabase database, ILoggerFactory loggerFactory)
    : DataContextBase<AppDbContext>(database, loggerFactory)
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

注册仓储：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.Mongo;

services.AddContextProvider();
services.AddMongoRepository<AppDbContext>(options =>
{
    options.UseConnection("mongodb://localhost:27017")
           .UseDatabase("euonia_sample");
});
```

使用仓储：

```csharp
using Nerosoft.Euonia.Repository;

public sealed class OrderService(IRepository<AppDbContext, Order, string> repository)
{
    public Task<List<Order>> FindAsync(CancellationToken cancellationToken)
        => repository.FindAsync(
            order => order.Amount > 100,
            query => query.OrderByDescending(order => order.Amount),
            cancellationToken);
}
```

## 主要功能

- **上下文基类**：`DataContextBase<TContext>` 实现 `IRepositoryContext`，持有 `IClientSessionHandle` 并暴露流式模型配置。
- **仓储实现**：`MongoRepository<TContext, TEntity, TKey>` 实现同一套 `IRepository` 契约，但保存与事务语义由 Mongo 决定。
- **统一注册**：`AddMongoRepository<TContext>(Action<MongoDbOptions>, ServiceLifetime)` 注册 `IMongoDatabase`、上下文与开放式仓储。
- **连接与数据库**：`MongoDbOptions.UseConnection(...)`、`UseDatabase(...)`。
- **模型配置**：`MongoDbContext`、`ModelBuilder`、`ModelProfile<T>`、`ModelKeyBuilder`、`ModelPropertyBuilder`。
- **主键序列化**：`ObjectIdSerializer` 在 `ObjectId` 与 24 位十六进制字符串/12 字节之间转换。
- **值生成**：`SnowflakeIdValueGenerator`、`SequentialGuidValueGenerator`。
- **事务**：`BeginTransactionAsync`、`CommitAsync`、`RollbackAsync`（依赖副本集/分片集群）。
- **多租户**：`MultiTenantMongoDataContextBase<TContext>`、`MongoTenantFilters`。

## 使用示例

在事务性工作单元中复用同一个 Mongo 上下文：

```csharp
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Uow;

public sealed class TransferService(
    IUnitOfWorkManager manager,
    IRepository<AppDbContext, Order, string> orders)
{
    public async Task ExecuteAsync(Order order, CancellationToken cancellationToken)
    {
        using var uow = manager.Begin(new UnitOfWorkOptions(isTransactional: true), requiresNew: true);

        await orders.InsertAsync(order, autoSave: false, cancellationToken);

        await uow.CompleteAsync(cancellationToken);
    }
}
```

按 `ObjectId` 查询：

```csharp
var id = ObjectId.GenerateNewId().ToString();
var order = await repository.GetAsync(id, cancellationToken);
```

## 选项（代码配置）

本包不读取 `IConfiguration`，不存在 `Euonia:Repository:Mongo` 配置节；`AddMongoRepository` 只接受 `Action<MongoDbOptions>` 回调。

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `MongoDbOptions.ConnectionString` | 无（必须设置） | 由 `UseConnection(string)` 设置，用于构造 `MongoClient` |
| `MongoDbOptions.Database` | 无（必须设置） | 由 `UseDatabase(string)` 设置 |
| `contextLifeTime` | `Scoped` | 上下文与仓储注册的生命周期参数 |

```csharp
services.AddMongoRepository<AppDbContext>(
    options => options.UseConnection("mongodb://localhost:27017").UseDatabase("euonia_sample"),
    contextLifeTime: ServiceLifetime.Scoped);
```

> 该包是代码配置：不要为它编写 `appsettings.json` 节。`IMongoDatabase` 以单例注册，连接串与数据库名必须在首次解析前设置。

## 核心类型

| 类型 | 作用 |
|------|------|
| `DataContextBase<TContext>` | MongoDB 上下文基类，管理会话与事务 |
| `MongoDbContext` | 集合、查询、插入、更新、删除入口 |
| `MongoRepository<TContext, TEntity, TKey>` | MongoDB 仓储实现 |
| `MongoDbOptions` | 连接串与数据库名 |
| `ModelBuilder` / `ModelProfile<T>` | 集合名、键、属性映射配置 |
| `ModelKeyBuilder` / `ModelPropertyBuilder` | 键生成与属性映射细节 |
| `ObjectIdSerializer` | `ObjectId` 与字符串/字节数组互转 |
| `IValueGenerator` / `SnowflakeIdValueGenerator` / `SequentialGuidValueGenerator` | 值生成器 |
| `MongoTenantFilters` / `MultiTenantMongoDataContextBase<TContext>` | 多租户过滤器与上下文基类 |
| `AddMongoRepository<TContext>(...)` | 注册上下文与仓储 |

## 注意事项 / 限制

1. `MongoRepository` 用 `MongoDB.Bson.ObjectId.Parse(entity.Id.ToString())` 定位文档，`TKey` 必须是可解析为 `ObjectId` 的字符串（24 位十六进制）或 `ObjectId`；不要用 `long`、`Guid` 作为 Mongo 仓储的键。
2. 事务要求 MongoDB 副本集或分片集群；单机 `mongod` 不支持事务，`BeginTransactionAsync` 会在提交/回滚时失败。
3. `DataContextBase.SaveChangesAsync` 固定返回 0，Mongo 的写入在仓储调用时已经发生；工作单元的提交/回滚只对显式开启的会话事务有意义。
4. `GetConnection()` 与 `GetTransaction()` 抛 `NotSupportedException`；Mongo 不提供 `IDbConnection`/`IDbTransaction`。
5. `AddMongoRepository` **不注册** `IContextProvider`，也不显式注册 Mongo 的 `IContextFactory`；需要 `[DependsOn(typeof(RepositoryModule))]` 或 `services.AddContextProvider()`，否则解析仓储会失败。
6. `Collection<T>()` 使用 `AssignIdOnInsert = true`；手动赋值的 `Id` 不会被驱动覆盖，只有默认值才会生成。
7. `OnModelCreating` 按上下文类型缓存并只执行一次；运行期修改 `ModelProfile` 不会重新注册 `BsonClassMap`。
8. `MongoDbContext` 的 `UpdateAsync` 会移除 `_id` 后以 `$set` 更新；`_id` 不可变，不能用更新修改主键。

## 相关包

- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储抽象
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 实现
- [`Euonia.Uow`](../Euonia.Uow/README.md) —— 工作单元
- [`Euonia.Domain`](../Euonia.Domain/README.md) —— 实体与聚合
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 多租户

## 常见问题与故障排查

### 解析 Mongo 仓储时报 `IContextProvider` 缺失
`AddMongoRepository` 只注册 `IMongoDatabase`、上下文与开放式仓储，不注册 `IContextProvider`。添加 `[DependsOn(typeof(RepositoryModule))]` 或显式 `services.AddContextProvider()`；否则 `MongoRepository` 的构造函数无法解析。

### `ObjectId.Parse` 抛格式异常
`MongoRepository` 的按键查询、更新、删除都会执行 `ObjectId.Parse(entity.Id.ToString())`。`TKey` 必须是 24 位十六进制字符串或 `ObjectId`；`long`、`Guid` 或普通业务编号会直接抛异常。

### 事务在本地 MongoDB 上失败
`BeginTransactionAsync` 依赖副本集或分片集群；单机 `mongod` 不支持多文档事务，提交/回滚会失败。开发环境使用单节点副本集，或在非事务模式下运行。

### `SaveChangesAsync` 返回 0，回滚也没有撤销写入
`DataContextBase.SaveChangesAsync` 固定返回 0，Mongo 的插入/更新/删除在仓储调用时已经发送；只有显式开启会话事务后，`CommitAsync`/`RollbackAsync` 才有意义。需要原子性时用 `IsTransactional=true` 的工作单元。

### 事务性工作单元没有复用同一个 Mongo 上下文
`AddMongoRepository` 不显式注册 Mongo 的 `IContextFactory`（源码中的 `UnitOfWorkContextFactory` 是 internal）；事务性工作单元的上下文复用需要宿主自行注册工厂或通过模块扫描装配。单库非事务场景使用默认 `DefaultContextFactory` 即可。

## 最佳实践

- 实体键使用 `ObjectId` 或 24 位十六进制字符串，并在 `OnModelCreating` 中用 `HasKey` 配置 `ObjectIdSerializer`。
- 用 `ModelBuilder.For<T>` 明确集合名、元素名与必填约束；`OnModelCreating` 每个上下文类型只执行一次，运行期不要再改模型。
- 生产环境使用副本集并配置唯一索引；批量写入前评估 `UpdateAsync` 的逐条请求成本。
- 连接串与数据库名通过代码配置或密钥库注入；本包不读取 `IConfiguration`，不要为它编写 `appsettings.json` 节。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Repository`、`Euonia.Tenancy`、`MongoDB.Driver`；具体版本由仓库构建属性决定。
- 已知边界：事务需要副本集；`SaveChangesAsync` 不参与提交；`GetConnection`/`GetTransaction` 不支持；`AddMongoRepository` 不注册 `IContextProvider`。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
