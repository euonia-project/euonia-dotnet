# Euonia.Auditing API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Auditing` 提供审计作用域、动作模型、异步写入队列、脱敏序列化与 InMemory 存储；持久化由 EF Core / Mongo 适配包完成。

- **包名**：`Euonia.Auditing`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing`
- **完整选项与边界**：[Source/Euonia.Auditing/README.md](../../../Source/Euonia.Auditing/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `AddAuditing(Action<AuditingOptions>?)` | 注册记录器、写入器与 hosted service | 不注册 `IAuditingStore` |
| `AddInMemoryAuditingStore()` | 进程内存储，测试用 | 同时注册 `IAuditingStoreQuery` |
| `AddFileAuditingSpool(string directory)` | 文件暂存，支持崩溃重放 | 在调用方线程写文件 |
| `IAuditingRecorder` | 作用域、提交与排空 | `EnsureScope`、`SubmitAsync`、`FlushAsync` |
| `IAuditingScope` | 一次执行作用域 | `BeginAction`、`CompleteAsync`、`AbortAsync` |
| `IAuditingAction` | 作用域内动作 | `SetParameter`、`Complete`、`Fail` |
| `IAuditingWriter` | 写入队列契约 | `WriteAsync`、`FlushAsync` |
| `IAuditingStore` | 持久化契约 | `SaveAsync`、`SaveManyAsync`；同 `ScopeId` 幂等 |
| `IAuditingStoreQuery` | 查询与保留清理 | `QueryAsync`、`FindByScopeAsync`、`DeleteBeforeAsync` |
| `IAuditingStoreRouter` | 按记录选择存储 | `GetStore(record)` |
| `AuditingRecord` | 作用域记录 | `ScopeId`、`IsSuccess`、`Actions` |
| `AuditingAction` | 动作记录 | `IdempotencyKey`、`Parameters`、`Result` |
| `AuditingOptions` | 全局选项 | `Enabled=false`、`Mode=All`、`BatchSize=50` |
| `AuditingScopeOptions` | 作用域选项 | `OperationKey`、`AutoComplete=true` |
| `[Audited]` | 标记必须审计的方法/类型 | `ActionName`、`RecordResult=true` |
| `AuditingDelivery` | 投递方式 | `Immediate`、`Batched` |
| `AuditingTriggerMode` | 触发模式 | `All`、`WritesOnly` |

### 配置：Euonia:Auditing 与 ConnectionStrings

```json
{
  "Euonia": {
    "Auditing": {
      "Enabled": true,
      "ApplicationName": "Orders",
      "Mode": "All",
      "SuccessDelivery": "Batched",
      "FailureDelivery": "Immediate"
    }
  },
  "ConnectionStrings": {
    "Audit": "Data Source=audit.db"
  }
}
```

```bash
Euonia__Auditing__Enabled=true
EUONIA_AUDITING_ENABLED=true
ConnectionStrings__Audit="Data Source=audit.db"
CONNECTION_STRING_AUDIT="Data Source=audit.db"
```

`SuccessDelivery` 默认 `Batched`、`FailureDelivery` 默认 `Immediate`；`BatchInterval` 默认 2 秒，`QueueCapacity=0` 表示无界。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Auditing/README.md)
- [Euonia.Auditing.EfCore 用户手册](../Euonia.Auditing.EfCore/API.md) —— 审计表结构与幂等落库
- [Euonia.Uow 用户手册](../Euonia.Uow/API.md) —— 业务事务边界
- [Euonia.Domain 用户手册](../Euonia.Domain/API.md) —— 被审计的领域对象
- [仓库根 README](../../../README.md)
