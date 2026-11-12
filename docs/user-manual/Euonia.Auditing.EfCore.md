# Euonia.Auditing.EfCore 用户手册

`Euonia.Auditing.EfCore` 是审计内核的 EF Core 存储适配：独立 `AuditingDbContext`、按 `ScopeId` 幂等落库、动作按幂等键去重，并内置表名/列名/索引约定。

- **包名**：`Euonia.Auditing.EfCore`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.EfCore`
- **完整选项与边界**：[Source/Euonia.Auditing.EfCore/README.md](../../Source/Euonia.Auditing.EfCore/README.md)

## 教程：建立审计库并验证幂等落库

这个场景为审计库生成表结构，注册 `AddAuditingEfCore<AuditingDbContext>`，连续两次保存同一个 `ScopeId`，再用 `IAuditingStoreQuery` 验证只有一条记录与一条动作。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已安装 `Euonia.Auditing`、`Euonia.Auditing.EfCore`、`Microsoft.EntityFrameworkCore.Sqlite` 与 `Microsoft.EntityFrameworkCore.Design`。
- 示例使用 SQLite；生产环境替换为 SQL Server/PostgreSQL 等关系型提供程序。

### 1. 创建项目并安装

```bash
dotnet new web -n AuditHost
cd AuditHost
dotnet add package Euonia.Auditing.EfCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. 模块接线

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;
using Nerosoft.Euonia.Modularity;

public sealed class AuditModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAuditing(options =>
        {
            options.Enabled = true;
            options.ApplicationName = "Orders";
            // 仅示例：允许跨租户查询；生产环境按合规策略实现。
            options.CrossTenantQueryAuthorizer = _ => true;
        });

        context.Services.AddAuditingEfCore<AuditingDbContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("Audit")));
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        using var db = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<AuditingDbContext>>()
            .CreateDbContext();
        db.Database.EnsureCreated();
    }
}
```

### 3. 保存与查询

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class AuditDemo(
    IAuditingStore store,
    IAuditingStoreQuery query)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var record = new AuditingRecord
        {
            Id = Guid.NewGuid(),
            ScopeId = "t1:req:demo",
            ExecutionTime = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            IsSuccess = true,
            Actions =
            [
                new AuditingAction
                {
                    ActionId = Guid.NewGuid(),
                    IdempotencyKey = "demo-action",
                    ServiceName = "Orders",
                    MethodName = "Create",
                    IsSuccess = true
                }
            ]
        };

        // 同一 ScopeId 重复保存：按幂等键合并，不产生重复行。
        await store.SaveAsync(record, cancellationToken);
        await store.SaveAsync(record, cancellationToken);

        var records = await query.QueryAsync(new AuditingQuery
        {
            AllowCrossTenant = true,
            CrossTenantReason = "demo",
            Take = 20
        }, cancellationToken);

        Console.WriteLine($"{records.Count}:{records[0].Actions.Count}");
    }
}
```

### 4. 建库与运行

```bash
dotnet ef migrations add AuditInit --context AuditingDbContext
dotnet ef database update --context AuditingDbContext
dotnet run
```

> 若 `dotnet ef` 无法构造上下文，添加 `IDesignTimeDbContextFactory<AuditingDbContext>`，或在启动项目中注册该上下文。

预期输出 `1:1`：同一个 `ScopeId` 只保留一条 `euonia_audit_log`，动作按 `idempotency_key` 去重为一条 `euonia_audit_action`。如果没有执行迁移，`EnsureCreated` 也可用于本地建表，但生产环境应使用迁移。

### 常见失败点

- **业务成功但审计库没数据**：本包不含迁移，表不存在时写入异常被审计写入器按失败开放处理；先建表。
- **写到了别的存储**：`AddAuditingEfCore` 只在容器中尚无 `IAuditingStore` 时注册；检查注册顺序。
- **唯一冲突/重复行**：`scope_id` 唯一索引由 `ApplyAuditingModel` 创建；自定义上下文没有应用该约定就不会去重。
- **单例工厂解析 Scoped 服务失败**：`configure` 回调只接收 `DbContextOptionsBuilder`，不要在回调中解析 Scoped 服务。

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

- [Source README：完整安装、选项与边界](../../Source/Euonia.Auditing.EfCore/README.md)
- [Euonia.Auditing 用户手册](./Euonia.Auditing.md) —— 作用域、写入队列与查询契约
- [Euonia.Repository.EfCore 用户手册](./Euonia.Repository.EfCore.md) —— EF Core 上下文基类
- [Euonia.Uow 用户手册](./Euonia.Uow.md) —— 业务事务边界
- [仓库根 README](../../README.md)
