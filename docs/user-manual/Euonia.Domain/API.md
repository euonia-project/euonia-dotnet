# Euonia.Domain API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Domain` 提供实体、聚合根、值对象、领域事件与命令等 DDD 构建块，不依赖具体持久化技术。

- **包名**：`Euonia.Domain`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Domain`
- **完整选项与边界**：[Source/Euonia.Domain/README.md](../../../Source/Euonia.Domain/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `Entity<TKey>` | 实体基类，提供 `Id`、`GetKeys()`、相等性 | `TKey : IEquatable<TKey>` |
| `Aggregate<TKey>` | 聚合根基类，管理领域事件 | 继承 `Entity<TKey>` |
| `ValueObject<T>` | 值对象基类，按公共属性比较 | `T : ValueObject<T>` |
| `Event` | 事件基类，含 `EventId`、`CorrelationId`、`Sequence` | `Sequence` 默认 `DateTime.UtcNow.Ticks` |
| `DomainEvent` | 领域事件基类，可 `Attach` 聚合 | `GetEventAggregate()` 生成快照 |
| `ApplicationEvent` | 应用层集成事件标记 | 继承 `Event` |
| `EventAggregate` | 事件持久化快照 | `Id`、`EventId`、`Payload`、`EventSequence` |
| `Command` / `Command<TData>` | 命令基类，自动生成 `CommandId` | `Properties` 可扩展 |
| `CommandStatus` | 命令状态 | `Succeed`、`Failure`、`Canceled` |
| `IAggregateRoot<TKey>` | 聚合根契约 | 继承 `IEntity<TKey>` |
| `IHasDomainEvents` | 事件载体契约 | `RaiseEvent`、`Apply`、`ClearEvents` |
| `Aggregate.Register<T>(Action<T>)` | 注册事件处理器 | `protected`，同一类型后注册覆盖 |
| `Aggregate.RaiseEvent<T>(T)` | 入队并调用已注册处理器 | 不自动发布到总线 |
| `Aggregate.GetAndClearEvents()` | 取出并清空事件 | 返回 `IReadOnlyList<DomainEvent>` |
| `Aggregate.LoadFromHistory(IEnumerable<DomainEvent>)` | 重放历史事件 | 只调用已注册处理器，不入队 |
| `EntityCacheAttribute` | 标记实体缓存开关 | `Enabled=true` |
| `AuditedAttribute` / `AuditingRecord` / `IAuditingStore` | 旧版审计契约 | 与 `Euonia.Auditing` 同名不同命名空间 |

### 配置

Domain 没有 `IConfiguration` 读取点，也没有 `Euonia:Domain` 配置节；行为由类型与派生类决定，不需要 `appsettings.json` 或环境变量。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Domain/README.md)
- [Euonia.Repository 用户手册](../Euonia.Repository/API.md) —— 聚合持久化
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 聚合操作的事务边界
- [Euonia.Auditing 用户手册](../Euonia.Auditing/API.md) —— 应用层审计
- [仓库根 README](../../../README.md)
