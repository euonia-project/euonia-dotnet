# Euonia.Auditing.Mongo API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

把 Euonia 审计记录持久化到 MongoDB，并用 `ScopeId` 唯一索引保证重复上报幂等。本页给出一个可运行的“月末关账”场景。

- **NuGet 包**：`Euonia.Auditing.Mongo`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.Mongo`（注册扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Auditing.Mongo/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddAuditingMongo(IServiceCollection, IMongoDatabase, string)` | 注册存储与查询契约 | `collectionName` 默认 `euonia_audit_log` |
| `MongoAuditingStore(IMongoDatabase, string)` | 存储实现 | 集合名不能为空 |
| `MongoAuditingStore.DefaultCollectionName` | 默认集合名常量 | `euonia_audit_log` |
| `EnsureIndexesAsync(CancellationToken)` | 创建 `ScopeId` 唯一索引 | 索引名 `ux_audit_log_scope` |
| `SaveAsync(AuditingRecord, CancellationToken)` | 幂等 upsert 一条记录 | 自动确保索引；冲突重试一次 |
| `QueryAsync(AuditingQuery, CancellationToken)` | 条件分页查询 | 按 `ExecutionTime` 倒序；`Take` 默认 50 |
| `FindByScopeAsync(string, CancellationToken)` | 按作用域查单条 | 需要匹配的租户上下文 |
| `DeleteBeforeAsync(DateTime, CancellationToken)` | 删除截止时间前的记录 | 需要显式跨租户上下文 |
| `IAuditingStore.SaveManyAsync(...)` | 批量保存的默认实现 | 逐条调用 `SaveAsync` |
| `IAuditingRecorder.CreateScope(AuditingScopeOptions?)` | 新建并设为当前作用域 | `OperationKey`、`AutoComplete` |
| `IAuditingRecorder.EnsureScope(...)` | 幂等获取/创建作用域 | 已有作用域时直接返回 |
| `IAuditingScope.BeginAction(service, method, name?)` | 开始一个动作 | 自动生成幂等键 |
| `IAuditingScope.CompleteAsync/AbortAsync` | 提交/失败提交作用域 | 幂等；失败会写 `IsSuccess=false` |
| `IAuditingAction.SetParameter/Complete/Fail` | 记录参数、结果或异常 | 参数写入前脱敏 |
| `AuditingOptions.Enabled/ApplicationName` | 审计开关与应用名 | 默认关闭 |
| `AuditingOptions.WriteRetryCount` | 存储写入重试次数 | 默认 1 |
| `AuditingQuery` | 查询条件 | `TenantId`、`UserId`、`From`、`To`、`Take` |

### 配置

本包没有自己的配置节，`IMongoDatabase` 与集合名都在代码中传入。审计行为由 `AddAuditing(Action<AuditingOptions>)` 的代码回调配置；如果宿主提供了 `IConfiguration`，`Euonia.Auditing` 还会绑定共享的 `Euonia:Auditing` 节，详见 [`Euonia.Auditing` 项目 README](../../../Source/Euonia.Auditing/README.md)。

> 不要为本包编写 `appsettings` 节；连接串通过宿主的 `ConnectionStrings` 读取后构造 `IMongoDatabase`。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、行为边界与注意事项](../../../Source/Euonia.Auditing.Mongo/README.md)
- [Euonia.Concurrency 用户手册](../Euonia.Concurrency/API.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/API.md)
- [根 README](../../../README.md)
