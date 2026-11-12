# Euonia.Auditing.Mongo

Euonia 审计记录的 MongoDB 存储适配：以 `ScopeId` 唯一索引保证幂等写入，动作按幂等键去重，并同时提供查询与保留删除能力。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.Mongo`
- **NuGet 包**：`Euonia.Auditing.Mongo`
- **依赖**：`Euonia.Auditing`（项目引用）、`MongoDB.Driver`
- **默认集合**：`euonia_audit_log`

## 安装

```bash
dotnet add package Euonia.Auditing.Mongo
```

## 快速开始

```csharp
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.Mongo;

var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("app");

services.AddSingleton<IMongoDatabase>(database);
services.AddAuditing(options => options.Enabled = true);
services.AddAuditingMongo(database);
```

`AddAuditingMongo` 的签名：

```csharp
IServiceCollection AddAuditingMongo(
    this IServiceCollection services,
    IMongoDatabase database,
    string collectionName = MongoAuditingStore.DefaultCollectionName);
```

启动后可显式预热索引（`SaveAsync` 也会惰性调用，漏掉不会丢数据，但会多一次写入路径开销）：

```csharp
public static async Task EnsureAuditIndexesAsync(IServiceProvider services)
{
    var store = services.GetRequiredService<MongoAuditingStore>();
    await store.EnsureIndexesAsync();
}
```

## 主要功能

- **幂等保存**：`SaveAsync` 以 `ScopeId` 为条件执行单条原子 upsert，重复上报同一作用域不会产生重复文档。
- **唯一索引**：`EnsureIndexesAsync` 在 `ScopeId` 上创建唯一索引 `ux_audit_log_scope`；并发 upsert 命中 `DuplicateKey` 时重试一次。
- **动作合并**：每个动作按 `IdempotencyKey` 先 `Pull` 再 `Push`（无幂等键时按 `ActionId`），实现“同键更新”而不是整文档去重。
- **合并语义**：`ExecutionDuration` 取最大值；`IsSuccess`、`HttpStatusCode` 只回填首次出现的非空值。
- **查询契约**：同时实现 `IAuditingStore` 与 `IAuditingStoreQuery`，注册后两个接口指向同一单例。

## 核心类型

| 类型 | 作用 |
|------|------|
| `MongoAuditingStore` | 审计存储实现；`IAuditingStore` + `IAuditingStoreQuery` |
| `MongoAuditingStore.EnsureIndexesAsync()` | 创建（并缓存成功结果）`ScopeId` 唯一索引 |
| `MongoAuditingStore.DefaultCollectionName` | 默认集合名常量 `euonia_audit_log` |
| `AddAuditingMongo(...)` | 注册存储与两个契约的 DI 扩展（命名空间 `Microsoft.Extensions.DependencyInjection`） |
| `AuditingRecord` / `AuditingAction` | 审计记录与动作模型，来自 `Euonia.Auditing` |
| `IAuditingStoreQuery` | `QueryAsync` / `FindByScopeAsync` / `DeleteBeforeAsync` |

## 选项（代码配置）

本包不读取 `IConfiguration`，也不绑定配置节；所有输入都在注册时以代码传入。审计行为开关由 `Euonia.Auditing` 的 `AddAuditing(Action<AuditingOptions>)` 回调配置（见 [`Euonia.Auditing`](../Euonia.Auditing/README.md)）。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `database` | `IMongoDatabase` | 必填 | 审计记录所在数据库；`null` 抛出 `ArgumentNullException` |
| `collectionName` | `string` | `euonia_audit_log` | 集合名称；空白抛出 `ArgumentException` |

> 不要为本包编写 `appsettings` 配置节；连接串、数据库与集合名均由宿主自行解析后注入。

## 使用示例

最小可运行宿主（Minimal API）：

```csharp
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.Mongo;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("ConnectionStrings:Mongo is not configured.");
var database = new MongoClient(connectionString).GetDatabase("app");

builder.Services.AddSingleton(database);
builder.Services.AddAuditing(options =>
{
    options.Enabled = true;
    options.ApplicationName = "orders";
});
builder.Services.AddAuditingMongo(database);

var app = builder.Build();

// 建议在开始接收流量前预热唯一索引。
await app.Services.GetRequiredService<MongoAuditingStore>().EnsureIndexesAsync();

app.MapGet("/audits", async (IAuditingStoreQuery store, string userId) =>
{
    var records = await store.QueryAsync(new AuditingQuery
    {
        UserId = userId,
        Take = 20
    });
    return Results.Ok(records);
});

app.Run();
```

查询与保留删除（均经 `IAuditingStoreQuery` 解析，结果按 `ExecutionTime` 倒序）：

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class AuditQueryService(IAuditingStoreQuery store)
{
    public Task<IReadOnlyList<AuditingRecord>> RecentAsync(string correlationId)
        => store.QueryAsync(new AuditingQuery
        {
            CorrelationId = correlationId,
            From = DateTime.UtcNow.AddDays(-7),
            Skip = 0,
            Take = 50
        });

    // 删除 cutoff 之前的记录（需要显式跨租户上下文，见下节）。
    public Task<int> PurgeAsync(DateTime cutoff)
        => store.DeleteBeforeAsync(cutoff);
}
```

## 注意事项 / 限制

- **注册顺序会决定生效的存储**：`AddAuditingMongo` 只在容器里还没有 `IAuditingStore` 时注册；若之前已调用 `AddInMemoryAuditingStore` 或 `AddAuditingEfCore`，Mongo 存储不会被使用。
- **唯一索引是幂等的前提**：若集合中已存在重复 `ScopeId`，`EnsureIndexesAsync` 会失败且不会缓存失败结果，下次 `SaveAsync` 会再试；上线前应先清理重复数据。
- **不是强一致读写**：写入为 upsert，冲突重试一次；审计读取跟随 MongoDB 的读关注点，刚写入的记录在极端情况下可能延迟可见。
- **查询有租户边界**：`QueryAsync` 会先规范化条件，`FindByScopeAsync` 要求当前环境上下文属于该租户；`DeleteBeforeAsync` 要求显式跨租户上下文（不同 actor/target 且给出原因），否则抛 `UnauthorizedAccessException`。
- **写入失败开放**：`AuditingWriter` 捕获存储异常并记录日志，按 `AuditingOptions.WriteRetryCount` 重试，不会改变业务结果。
- **无自动保留策略**：本包不创建 TTL 索引；按时间清理需由宿主调用 `DeleteBeforeAsync` 或自建作业。

## 相关包

- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计核心：`AddAuditing`、`AuditingOptions`、写入器与查询契约
- [`Euonia.Auditing.EfCore`](../Euonia.Auditing.EfCore/README.md) —— EF Core 存储适配
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 仓储与 `MongoDbOptions`

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
