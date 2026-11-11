# Euonia.Auditing.EfCore

Euonia 审计的 EF Core 存储适配：独立审计库、幂等落库（按 `ScopeId`）、动作按幂等键去重、索引约定。

- **目标框架**：`net10.0`、`net11.0`
- **依赖**：`Euonia.Auditing` + EF Core

## 快速开始

```csharp
services.AddAuditing(options => options.Enabled = true);
services.AddAuditingEfCore<AuditingDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("Audit")));
```

## 表结构

- `euonia_audit_log`：`scope_id` 唯一索引，时间 / 租户 / 目标租户 / 用户 / 关联索引。
- `euonia_audit_action`：`(log_id, idempotency_key)` 唯一索引，级联删除。

## 说明

- 审计上下文与业务上下文完全独立，不参与业务事务。
- 写入失败由 `AuditingWriter` 捕获并丢弃，不改变业务结果。
- 同一 `ScopeId` 重复保存不会产生重复行；动作按 `IdempotencyKey` 去重。
