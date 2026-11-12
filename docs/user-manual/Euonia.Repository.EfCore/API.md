# Euonia.Repository.EfCore API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Repository.EfCore` 是仓储抽象的 EF Core 实现：提供 `DataContextBase<TContext>`、`EfCoreRepository<,,>`、连接串解析与工作单元上下文工厂。

- **包名**：`Euonia.Repository.EfCore`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Repository.EfCore`
- **完整选项与边界**：[Source/Euonia.Repository.EfCore/README.md](../../../Source/Euonia.Repository.EfCore/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Repository.EfCore/README.md)
- [Euonia.Repository 用户手册](../Euonia.Repository/API.md) —— 仓储抽象
- [Euonia.Repository.Mongo 用户手册](../Euonia.Repository.Mongo/API.md) —— MongoDB 实现
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 工作单元与事务
- [Euonia.Auditing.EfCore 用户手册](../Euonia.Auditing.EfCore/API.md) —— 审计库的 EF Core 实现
- [仓库根 README](../../../README.md)
