# Euonia.Auditing.EfCore

Euonia 审计的 EF Core 存储适配：独立 `AuditingDbContext`、按 `ScopeId` 幂等落库、动作按幂等键去重，并内置表名/列名/索引约定。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing.EfCore`
- **NuGet 包**：`Euonia.Auditing.EfCore`
- **依赖**：`Euonia.Auditing` + EF Core / EF Core Relational

## 安装

```bash
dotnet add package Euonia.Auditing.EfCore
```

按数据库选择 EF Core 提供程序，例如：

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## 快速开始

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;

services.AddAuditing(options => options.Enabled = true);
services.AddAuditingEfCore<AuditingDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("Audit")));
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `AuditingDbContext` | 独立审计上下文；`Records` 为 `DbSet<AuditingRecord>` |
| `AddAuditingEfCore<TContext>(Action<DbContextOptionsBuilder>)` | 注册单例 `IDbContextFactory<TContext>` 与 `EfCoreAuditingStore<TContext>` |
| `EfCoreAuditingStore<TContext>` | `IAuditingStore` + `IAuditingStoreQuery` 实现 |
| `AuditingModelBuilderExtensions.ApplyAuditingModel()` | 应用表名、列名、索引与级联删除约定 |

## 使用示例

### 1. 创建审计库结构

本包**不包含迁移**；在宿主项目中安装 `Microsoft.EntityFrameworkCore.Design` 后，为 `AuditingDbContext` 生成并应用迁移：

```bash
dotnet ef migrations add AuditInit --context AuditingDbContext
dotnet ef database update --context AuditingDbContext
```

> 需要宿主提供设计时工厂（`IDesignTimeDbContextFactory<AuditingDbContext>`），或在启动项目中配置该上下文。

也可以使用 `EnsureCreated`（仅适合测试或首次建库）：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using var scope = app.Services.CreateScope();
await using var db = await scope.ServiceProvider
    .GetRequiredService<IDbContextFactory<AuditingDbContext>>()
    .CreateDbContextAsync();
await db.Database.EnsureCreatedAsync();
```

### 2. 查询与清理

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class AuditMaintenance(IAuditingStoreQuery store)
{
    public Task<int> PurgeAsync(DateTime cutoff, CancellationToken cancellationToken)
        => store.DeleteBeforeAsync(cutoff, cancellationToken);
}
```

### 3. 每租户审计库

写入器可通过 `IAuditingStoreRouter` 按记录选择存储；每租户库可注册多个 `IAuditingStore` 并由自定义路由器选择。单库场景直接使用 `AddAuditingEfCore` 即可。

## 表结构

列名、长度与索引名来自 `ApplyAuditingModel`；具体数据库类型由 EF Core 提供程序决定，下表按 SQL Server 展示。

### `euonia_audit_log`

| 列 | 类型/约束 | 说明 |
|----|-----------|------|
| `id` | PK，`Guid` | 记录标识 |
| `scope_id` | `nvarchar(256)`，必填，唯一索引 `ux_audit_log_scope` | 作用域标识；幂等键 |
| `tenant_id` / `tenant_name` | `nvarchar(64)` / `nvarchar(256)` | 租户 |
| `target_tenant_id` | `nvarchar(64)` | 跨租户目标 |
| `is_cross_tenant` / `cross_tenant_reason` | `bit` / `nvarchar(512)` | 跨租户标记与原因 |
| `user_id` / `user_name` | `nvarchar(128)` / `nvarchar(256)` | 用户 |
| `client_id` / `correlation_id` | `nvarchar(128)` / `nvarchar(64)` | 客户端与关联标识 |
| `trace_id` / `span_id` / `request_trace_id` | `nvarchar(64)` / `nvarchar(32)` / `nvarchar(64)` | W3C 跟踪 |
| `unit_of_work_id` / `application_name` | `nvarchar(64)` / `nvarchar(128)` | 工作单元与应用 |
| `execution_time` / `execution_duration` | `datetime2` / `int` | 开始时间（UTC）与耗时（毫秒） |
| `client_ip_address` / `client_name` / `browser_info` | `nvarchar(64)` / `nvarchar(128)` / `nvarchar(512)` | 客户端信息 |
| `http_method` / `http_status_code` / `url` | `nvarchar(16)` / `int` / `nvarchar(2048)` | HTTP 信息 |
| `is_success` / `extra_properties` / `recorded_at` | `bit` / 文本 / `datetime2` | 结果、扩展属性与写入时间 |

索引：`ix_audit_log_time`、`ix_audit_log_tenant_time`、`ix_audit_log_target_time`、`ix_audit_log_user_time`、`ix_audit_log_correlation`。

### `euonia_audit_action`

| 列 | 类型/约束 | 说明 |
|----|-----------|------|
| `id` | PK，`Guid` | 动作标识 |
| `log_id` | FK → `euonia_audit_log.id`，级联删除 | 所属记录 |
| `parent_action_id` | `Guid?` | 父动作（嵌套调用） |
| `idempotency_key` | `nvarchar(320)`，必填 | 动作幂等键 |
| `service_name` / `method_name` / `action_name` | `nvarchar(512)` / `nvarchar(256)` / `nvarchar(256)` | 动作来源与显示名 |
| `execution_time` / `execution_duration` / `is_success` | `datetime2` / `int` / `bit` | 执行信息 |
| `parameters` / `result` | 文本 | 脱敏后的参数与结果 |
| `exception_type` / `exception_message` / `exception_stack_trace` | `nvarchar(512)` / `nvarchar(2048)` / 文本 | 异常信息 |
| `extra_properties` | 文本 | 扩展属性 |

索引：`ix_audit_action_log`（`log_id`）、唯一索引 `ux_audit_action_key`（`log_id`, `idempotency_key`）。

## 配置项

本包不读取 `IConfiguration`，也不存在 `Euonia:Auditing:EfCore` 配置节；连接串由宿主在 `configure` 回调中读取并传入。

| 来源 | 键 | 说明 |
|------|----|------|
| 显式参数 | `AddAuditingEfCore` 的 `configure` | 直接配置 `DbContextOptionsBuilder`，优先级最高 |
| 配置/环境变量 | `ConnectionStrings:<name>` | 示例使用 `Audit`；名称可自定义 |
| 环境变量（.NET） | `ConnectionStrings__Audit` | 双下划线表示层级 |
| 环境变量（仓库 alias） | `CONNECTION_STRING_AUDIT` | 映射为 `ConnectionStrings:AUDIT`（键名不区分大小写） |

```json
{
  "ConnectionStrings": {
    "Audit": "Server=localhost;Database=euonia_audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

```bash
ConnectionStrings__Audit="Server=localhost;Database=euonia_audit;Trusted_Connection=True;TrustServerCertificate=True"
CONNECTION_STRING_AUDIT="Server=localhost;Database=euonia_audit;Trusted_Connection=True;TrustServerCertificate=True"
```

## 注意事项 / 限制

1. 包内**没有迁移**；必须由宿主为 `AuditingDbContext` 生成迁移或调用 `EnsureCreated`，否则写入会因表不存在而失败。
2. 审计上下文与业务上下文完全独立，不参与业务事务；不要把审计库连接串指向业务库并期望同一事务。
3. `TContext` 必须继承 `AuditingDbContext`；通常直接使用 `AddAuditingEfCore<AuditingDbContext>`。`AuditingDbContext` 的构造函数接收 `DbContextOptions<AuditingDbContext>`，自定义派生上下文需要自行处理选项类型。
4. `AddAuditingEfCore` 只在容器中尚无 `IAuditingStore` 时注册存储；若先注册了其他存储，本包的存储不会覆盖它。
5. `IDbContextFactory<TContext>` 以单例注册，`configure` 回调不提供 `IServiceProvider` 重载，不能在其中解析 Scoped 服务。
6. `ScopeId` 必填且唯一；直接调用 `IAuditingStore.SaveAsync` 时不要传空 `ScopeId`。
7. 并发重复写入依赖数据库唯一索引；唯一冲突检测覆盖 SQL Server（2627/2601）、PostgreSQL（23505）、MySQL（1062）与 SQLite（19 + UNIQUE），其他提供程序会把异常原样抛出。
8. `SaveManyAsync` 仅在唯一冲突时清空跟踪器并退化为逐条保存；其他批量错误不会自动逐条重试。
9. `DeleteBeforeAsync` 使用 `ExecuteDeleteAsync`，要求关系型提供程序，并受 `AuditingQueryAuthorizer` 的租户/授权范围限制。

## 相关包

- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计内核与契约
- [`Euonia.Auditing.Mongo`](../Euonia.Auditing.Mongo/README.md) —— MongoDB 存储
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 仓储与上下文基类
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— `UseAuditing()` 中间件

## 常见问题与故障排查

### 业务成功，但审计库没有数据
先确认表已创建：本包不含迁移，只注册存储不会建表。为 `AuditingDbContext` 生成并应用迁移，或在测试中调用 `EnsureCreated`。表不存在时写入异常会被审计写入器按失败开放处理，业务仍返回成功，但记录会丢失。

### 已注册 `AddAuditingEfCore`，实际却写到了别的存储
`AddAuditingEfCore` 只在容器中尚无 `IAuditingStore` 时注册自己的存储；如果先注册了 InMemory 或 Mongo 存储，本包不会覆盖。调整注册顺序，或移除其他存储注册。

### 同一 `ScopeId` 重复保存报唯一冲突或出现重复行
`euonia_audit_log.scope_id` 有唯一索引 `ux_audit_log_scope`，`EfCoreAuditingStore` 在冲突时会重新加载并合并终态与动作。若自定义上下文没有调用 `ApplyAuditingModel()`，唯一索引不会创建，重复行就会出现；请直接使用 `AuditingDbContext` 或在其 `OnModelCreating` 中应用该约定。

### `ScopeId` 为空导致 `ArgumentException`
`EfCoreAuditingStore` 要求 `ScopeId` 非空白；正常路径由 `AuditingRecorder` 生成 `{tenant}:{opKey}`。直接调用 `IAuditingStore.SaveAsync` 或手工构造 `AuditingRecord` 时必须自行填充 `ScopeId`。

### 单例工厂中解析 Scoped 服务失败
`AddAuditingEfCore` 以单例注册 `IDbContextFactory<TContext>`，其 `configure` 回调只接收 `DbContextOptionsBuilder`，没有 `IServiceProvider` 重载。不要在回调中解析 Scoped 服务；需要动态连接串时在宿主侧提前计算。

## 最佳实践

- 把 `AuditingDbContext` 迁移纳入部署流水线，并在发布前执行 `dotnet ef database update`；不要依赖运行时自动建表。
- 审计库使用独立连接串（示例 `ConnectionStrings:Audit`），不要指向业务库；环境差异用 `ConnectionStrings__Audit` 或 `CONNECTION_STRING_AUDIT` 覆盖。
- 按保留策略定期调用 `IAuditingStoreQuery.DeleteBeforeAsync`，并确保处于显式跨租户上下文；清理前确认 `ix_audit_log_time` 可用。
- 批量写入依赖 `SaveManyAsync` 的预取与唯一冲突降级；保留 `SuccessDelivery=Batched`，并监控审计写入失败日志。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Auditing`、`Microsoft.EntityFrameworkCore` 与 `Microsoft.EntityFrameworkCore.Relational`；具体版本由仓库构建属性决定。
- 已知边界：包内无迁移；唯一冲突检测覆盖 SQL Server、PostgreSQL、MySQL、SQLite，其他提供程序会把异常原样抛出；`DeleteBeforeAsync` 依赖 `ExecuteDeleteAsync`，需要关系型提供程序。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
