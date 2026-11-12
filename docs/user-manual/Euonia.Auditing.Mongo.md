# Euonia.Auditing.Mongo 用户手册

把 Euonia 审计记录持久化到 MongoDB，并用 `ScopeId` 唯一索引保证重复上报幂等。本页给出一个可运行的“月末关账”场景。

- **NuGet 包**：`Euonia.Auditing.Mongo`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.Mongo`（注册扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **完整安装与选项**：[项目 README](../../Source/Euonia.Auditing.Mongo/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

订单服务提供一个幂等的“关账”接口。每次执行写入一条审计记录，动作中包含关账期间与结果；重复调用同一期间不会产生重复记录。管理员随后直接查询 MongoDB 校验。

### 前置条件

- .NET 10 或 11 SDK。
- 可连接的 MongoDB（本地或容器），以及可写的 `app` 数据库。
- 已安装 `Euonia.Auditing.Mongo`；它会带入 `Euonia.Auditing` 与 `MongoDB.Driver`。

### 1. 安装

```bash
dotnet add package Euonia.Auditing.Mongo
```

### 2. 注册审计与存储

```csharp
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.Mongo;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("ConnectionStrings:Mongo is missing.");

var database = new MongoClient(connectionString).GetDatabase("app");

builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddAuditing(options =>
{
    options.Enabled = true;
    options.ApplicationName = "orders-api";
    options.WriteRetryCount = 2;
});
builder.Services.AddAuditingMongo(database);
builder.Services.AddSingleton<OrderClosingService>();

var app = builder.Build();

// 建议在开始接收流量前创建 ScopeId 唯一索引。
await app.Services.GetRequiredService<MongoAuditingStore>().EnsureIndexesAsync();
```

### 3. 用审计作用域包住业务动作

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class OrderClosingService(IAuditingRecorder recorder)
{
    public async Task<(int Closed, string ScopeId)> CloseAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var scope = recorder.CreateScope(new AuditingScopeOptions
        {
            OperationKey = $"orders:close:{period:yyyyMMdd}",
            IdempotencyKey = $"orders:close:{period:yyyyMMdd}",
            AutoComplete = false
        });

        var action = scope.BeginAction("orders", nameof(CloseAsync), "Close orders");
        action.SetParameter("period", period.ToString("yyyy-MM-dd"));

        try
        {
            var closed = await CloseOrdersAsync(period, cancellationToken);
            action.Complete(closed);
            await scope.CompleteAsync(cancellationToken);
            return (closed, scope.ScopeId);
        }
        catch (Exception exception)
        {
            action.Fail(exception);
            await scope.AbortAsync(exception, cancellationToken);
            throw;
        }
    }

    // 业务实现：示例返回固定值，真实项目替换为数据库操作。
    private static Task<int> CloseOrdersAsync(DateOnly period, CancellationToken cancellationToken)
        => Task.FromResult(42);
}
```

### 4. 暴露 HTTP 入口

```csharp
app.MapPost("/orders/close", async (DateOnly period, OrderClosingService service, CancellationToken cancellationToken) =>
{
    var result = await service.CloseAsync(period, cancellationToken);
    return Results.Ok(new { Closed = result.Closed, ScopeId = result.ScopeId });
});
```

### 运行与验证

```bash
ConnectionStrings__Mongo="mongodb://localhost:27017" \
  dotnet run --urls http://localhost:5080

curl -X POST "http://localhost:5080/orders/close?period=2026-11-30"
```

返回的 `scopeId` 形如 `-:orders:close:20261130`（无租户上下文时前缀为 `-`）。在 MongoDB 中核对：

```bash
mongosh "mongodb://localhost:27017/app" --eval \
  'db.euonia_audit_log.findOne({ ScopeId: "-:orders:close:20261130" })'
```

再调用一次相同期间，文档数应保持为 1；动作按 `IdempotencyKey` 合并，而不是追加重复动作。

### 常见失败点

- **唯一索引创建失败**：集合内已有重复 `ScopeId`。先清理重复数据再启动；`EnsureIndexesAsync` 不会缓存失败结果。
- **记录写进了内存存储**：在 `AddAuditingMongo` 之前调用了 `AddInMemoryAuditingStore` 或 `AddAuditingEfCore`，先注册的 `IAuditingStore` 生效。
- **按作用域查询抛 `UnauthorizedAccessException`**：`FindByScopeAsync` 要求当前环境上下文的租户与 `ScopeId` 前缀一致；本示例直接用 MongoDB 查询绕过该限制。
- **查不到刚写入的记录**：`QueryAsync` 的时间过滤基于 `ExecutionTime`，不是 `RecordedAt`；且默认 `Take` 为 50。

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

本包没有自己的配置节，`IMongoDatabase` 与集合名都在代码中传入。审计行为由 `AddAuditing(Action<AuditingOptions>)` 的代码回调配置；如果宿主提供了 `IConfiguration`，`Euonia.Auditing` 还会绑定共享的 `Euonia:Auditing` 节，详见 [`Euonia.Auditing` 项目 README](../../Source/Euonia.Auditing/README.md)。

> 不要为本包编写 `appsettings` 节；连接串通过宿主的 `ConnectionStrings` 读取后构造 `IMongoDatabase`。

## 相关文档

- [项目 README：安装、行为边界与注意事项](../../Source/Euonia.Auditing.Mongo/README.md)
- [Euonia.Concurrency 用户手册](./Euonia.Concurrency.md)
- [Euonia.Caching 用户手册](./Euonia.Caching.md)
- [根 README](../../README.md)
