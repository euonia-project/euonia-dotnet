# Euonia.Auditing

Euonia 审计能力内核：作用域 / 动作模型、记录契约、写入队列、脱敏序列化与 InMemory 存储。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing`
- **NuGet 包**：`Euonia.Auditing`

## 定位

`Euonia.Auditing` 只依赖 `Euonia.Core` 与 `Microsoft.Extensions.*` 抽象包；不引用 Tenancy、Telemetry、EF Core、MongoDB 或 ASP.NET Core。持久化与触发器由上层适配包反向引用。

## 快速开始

```csharp
services.AddAuditing(options =>
{
    options.Enabled = true;
    options.ApplicationName = "Orders";
})
.AddInMemoryAuditingStore();   // 生产环境替换为 EF / Mongo 适配
```

HTTP 管道（审计必须在租户之前）：

```csharp
app.UseAuditing();
app.UseTenancy();
```

## 核心契约

| 类型 | 说明 |
|------|------|
| `IAuditingRecorder` | `EnsureScope` / `SubmitAsync` / `FlushAsync` |
| `IAuditingScope` | 一个执行作用域；完成时提交一条记录，动作可为空 |
| `IAuditingAction` | 设置参数 / 扩展属性，`Complete` / `Fail` |
| `IAuditingWriter` | 写入队列；失败开放 |
| `IAuditingStore` | 持久化契约；同 `ScopeId` 幂等 |
| `AuditedAttribute` | 标记必须审计的方法或类型 |

## 语义

- `ScopeId` = `{actorTenantId ?? "-"}:{opKey}`；HTTP 用 `req:{Guid}`，消息用 `msg:{MessageId}`。
- 审计失败开放：写入异常只记录日志，不改变业务结果。
- 参数与结果在序列化前经 `SensitiveDataMasker` 脱敏。
- `IsSuccess` 为 `null` 表示未知（未封口动作）。
- 未安装租户能力时租户字段为空，不影响审计可用性。
