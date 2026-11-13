# Euonia.Uow API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Uow` 用工作单元（Unit of Work）把多个仓储操作包进同一个事务边界，并提供环境上下文、嵌套工作单元与 Castle 拦截器。

- **包名**：`Euonia.Uow`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Uow`（DI 扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项与边界**：[Source/Euonia.Uow/README.md](../../../Source/Euonia.Uow/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `IUnitOfWork` | 工作单元契约：保存、提交、回滚、上下文与服务解析 | `CompleteAsync`、`RollbackAsync` |
| `IUnitOfWorkManager` | 获取当前工作单元、开启新工作单元 | `Current`、`Begin` |
| `IUnitOfWorkAccessor` | 基于 `AsyncLocal<T>` 的环境访问器 | `GetCurrentUnitOfWork()` |
| `IUnitOfWorkContext` | 单个数据上下文的 `SaveChanges` / `Commit` / `Rollback` | `Dispose` 由实现负责 |
| `UnitOfWorkOptions` | 事务性、隔离级别、超时 | `IsTransactional=false`、`IsolationLevel=null`、`Timeout=null` |
| `UnitOfWorkAttribute` | 标注类/方法/接口启用工作单元 | `IsTransactional`、`IsolationLevel`、`IsDisabled` |
| `IUnitOfWorkEnabled` | 标记接口，按约定参与工作单元 | 无成员 |
| `UnitOfWorkModule` | 注册拦截器并绑定 `Euonia:Uow` | 依赖模块加载 |
| `UnitOfWorkInterceptor` | Castle `IInterceptor` 实现 | 每个调用创建独立 DI 作用域 |
| `AddUnitOfWork()` | 注册 `IUnitOfWork`、访问器、管理器 | `IUnitOfWork` 瞬态；访问器/管理器单例 |
| `IUnitOfWorkManager.Begin(UnitOfWorkOptions, bool)` | 按选项开启工作单元 | `requiresNew=false` 时复用为子工作单元 |
| `IUnitOfWorkManager.Begin(bool, bool)` | 按事务标志开启 | `isTransactional=false`、`requiresNew=false` |
| `IUnitOfWork.CompleteAsync(CancellationToken)` | 保存所有上下文，再提交事务性上下文 | 已回滚时直接返回；重复调用抛异常 |
| `IUnitOfWork.SaveChangesAsync(CancellationToken)` | 只保存，不提交事务 | 已回滚时直接返回 |
| `IUnitOfWork.RollbackAsync(CancellationToken)` | 逐个回滚上下文 | 幂等 |
| `IUnitOfWork.Reserve(string)` | 将工作单元标记为独占 | `UnitOfWorkAccessor` 会跳过已预留实例 |

### 配置：Euonia:Uow

```json
{
  "Euonia": {
    "Uow": {
      "IsTransactional": true,
      "IsolationLevel": "ReadCommitted",
      "Timeout": "00:00:30"
    }
  },
  "ConnectionStrings": {
    "Default": "Data Source=sales.db"
  }
}
```

```bash
Euonia__Uow__IsTransactional=true
EUONIA_UOW_ISTRANSACTIONAL=true
ConnectionStrings__Default="Data Source=sales.db"
CONNECTION_STRING_DEFAULT="Data Source=sales.db"
```

`IsolationLevel` 为 `System.Data.IsolationLevel` 名称；`Timeout` 为 `TimeSpan` 字符串，只用于 `CompleteAsync` 的取消令牌。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Uow/README.md)
- [Euonia.Repository 用户手册](../Euonia.Repository/API.md) —— 仓储抽象与连接串
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/API.md) —— EF Core 上下文与工作单元工厂
- [Euonia.Auditing 用户手册](../Euonia.Auditing/API.md) —— 在事务边界外记录审计
- [仓库根 README](../../../README.md)
