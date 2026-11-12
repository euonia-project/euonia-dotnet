# Euonia.Repository.Mongo API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Repository.Mongo` 是仓储抽象的 MongoDB 实现：提供 `MongoDbContext`、`DataContextBase<TContext>`、`MongoRepository<,,>` 与流式模型配置。

- **包名**：`Euonia.Repository.Mongo`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.Mongo`
- **完整选项与边界**：[Source/Euonia.Repository.Mongo/README.md](../../../Source/Euonia.Repository.Mongo/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Repository.Mongo/README.md)
- [Euonia.Repository 用户手册](../Euonia.Repository/API.md) —— 仓储抽象
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/API.md) —— EF Core 实现
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 事务边界与 Mongo 限制
- [Euonia.Domain 用户手册](../Euonia.Domain/API.md) —— 实体契约
- [仓库根 README](../../../README.md)
