# Euonia.Auditing.Mongo 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Auditing.Mongo |
| 公共类型 | 2 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.AuditingMongoServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuditingMongo(Microsoft.Extensions.DependencyInjection.IServiceCollection services, MongoDB.Driver.IMongoDatabase database, System.String collectionName = "euonia_audit_log")

## 命名空间 Nerosoft.Euonia.Auditing.Mongo

### Nerosoft.Euonia.Auditing.Mongo.MongoAuditingStore

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingStore、Nerosoft.Euonia.Auditing.IAuditingStoreQuery

- new Nerosoft.Euonia.Auditing.Mongo.MongoAuditingStore(MongoDB.Driver.IMongoDatabase database, System.String collectionName = "euonia_audit_log")
- static const System.String DefaultCollectionName = euonia_audit_log
- Task<System.Int32> DeleteBeforeAsync(System.DateTime cutoff, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task EnsureIndexesAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Auditing.AuditingRecord> FindByScopeAsync(System.String scopeId, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Nerosoft.Euonia.Auditing.AuditingRecord>> QueryAsync(Nerosoft.Euonia.Auditing.AuditingQuery query, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)
