# Euonia.Auditing.Mongo

Euonia 审计的 MongoDB 存储适配：按 `ScopeId` 幂等、动作按幂等键合并、唯一索引。

- **目标框架**：`net10.0`、`net11.0`
- **依赖**：`Euonia.Auditing` + `MongoDB.Driver`

## 快速开始

```csharp
services.AddAuditing(options => options.Enabled = true);
services.AddAuditingMongo(database);   // collection 默认 euonia_audit_log
```

启动后调用一次 `MongoAuditingStore.EnsureIndexesAsync()` 创建 `scope_id` 唯一索引。

## 说明

- 同一 `ScopeId` 重复保存不会产生重复文档；动作按 `IdempotencyKey` 合并。
- Mongo 同时实现 `IAuditingStoreQuery`（查询、按 scope 查找、保留删除），注册时与 `IAuditingStore` 指向同一实例。
- 审计失败开放：写入异常由 `AuditingWriter` 捕获，不改变业务结果。
