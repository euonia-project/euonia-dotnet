# Euonia.Repository API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Repository` 定义持久化无关的仓储契约、上下文提供者与分页排序扩展；EF Core / MongoDB 包提供具体实现。

- **包名**：`Euonia.Repository`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository`
- **完整选项与边界**：[Source/Euonia.Repository/README.md](../../../Source/Euonia.Repository/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Repository/README.md)
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/API.md) —— EF Core 上下文与连接串解析
- [Euonia.Repository.Mongo 用户手册](../Euonia.Repository.Mongo/API.md) —— MongoDB 实现
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 事务边界
- [Euonia.Domain 用户手册](../Euonia.Domain/API.md) —— 实体契约
- [仓库根 README](../../../README.md)
