# Euonia.Auditing.EfCore API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Auditing.EfCore` 是审计内核的 EF Core 存储适配：独立 `AuditingDbContext`、按 `ScopeId` 幂等落库、动作按幂等键去重，并内置表名/列名/索引约定。

- **包名**：`Euonia.Auditing.EfCore`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.EfCore`
- **完整选项与边界**：[Source/Euonia.Auditing.EfCore/README.md](../../../Source/Euonia.Auditing.EfCore/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `AuditingDbContext` | 独立审计上下文 | `Records` 为 `DbSet<AuditingRecord>` |
| `AddAuditingEfCore<TContext>(Action<DbContextOptionsBuilder>)` | 注册单例 `IDbContextFactory<TContext>` 与存储 | `TContext : AuditingDbContext` |
| `EfCoreAuditingStore<TContext>` | `IAuditingStore` + `IAuditingStoreQuery` 实现 | 按 `ScopeId` 幂等 |
| `AuditingModelBuilderExtensions.ApplyAuditingModel()` | 应用表名/列名/索引约定 | `euonia_audit_log`、`euonia_audit_action` |
| `IAuditingStore.SaveAsync` | 保存一条记录 | 空 `ScopeId` 抛 `ArgumentException` |
| `IAuditingStore.SaveManyAsync` | 批量保存 | 唯一冲突时逐条降级 |
| `IAuditingStoreQuery.QueryAsync` | 条件查询，按执行时间倒序 | `Take` 默认 50，最大 500 |
| `IAuditingStoreQuery.FindByScopeAsync` | 按 `ScopeId` 查找 | 受租户访问校验 |
| `IAuditingStoreQuery.DeleteBeforeAsync` | 删除早于截止时间的记录 | 需显式跨租户上下文 |
| `AuditingRecord` | 作用域记录 | `ScopeId` 唯一，`IsSuccess` 可空 |
| `AuditingAction` | 动作记录 | `(log_id, idempotency_key)` 唯一 |
| `AuditingQuery` | 查询条件 | `AllowCrossTenant`、`CrossTenantReason` |
| `IDbContextFactory<AuditingDbContext>` | 审计上下文工厂 | 单例注册 |
| `ux_audit_log_scope` / `ux_audit_action_key` | 唯一索引 | 幂等保障 |
| `ix_audit_log_time` / `ix_audit_log_tenant_time` | 查询索引 | 时间与租户过滤 |

### 配置：ConnectionStrings

本包不读取 `IConfiguration`，也不存在 `Euonia:Auditing:EfCore` 配置节；连接串由宿主在 `configure` 回调中读取并传入。

```json
{
  "ConnectionStrings": {
    "Audit": "Data Source=audit.db"
  }
}
```

```bash
ConnectionStrings__Audit="Data Source=audit.db"
CONNECTION_STRING_AUDIT="Data Source=audit.db"
```

### 表结构要点

- `euonia_audit_log`：主键 `id`，`scope_id` 唯一索引 `ux_audit_log_scope`，时间/租户/用户/关联索引。
- `euonia_audit_action`：主键 `id`，`log_id` 外键级联删除，`(log_id, idempotency_key)` 唯一索引 `ux_audit_action_key`。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Auditing.EfCore/README.md)
- [Euonia.Auditing 用户手册](../Euonia.Auditing/API.md) —— 作用域、写入队列与查询契约
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/API.md) —— EF Core 上下文基类
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 业务事务边界
- [仓库根 README](../../../README.md)
