# Euonia.Tenancy

Euonia 多租户能力内核：租户上下文、解析链、存储、隔离模式，以及 EF Core / Mongo / 消息 / 后台任务的租户适配契约。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Tenancy`
- **NuGet 包**：`Euonia.Tenancy`

## 安装

```bash
dotnet add package Euonia.Tenancy
```

HTTP 解析器（Claim / Host / Route / Header）与中间件在 `Euonia.Hosting`，需要 Web 宿主时一并安装。

## 定位

本包只依赖 `Euonia.Core` 与少量 `Microsoft.Extensions.*` 抽象包，不引用 EF Core、ASP.NET Core、MongoDB 或任何 Euonia 上层项目。持久化与宿主适配由各上层项目反向引用本包完成。

## 快速开始

只注册内核（控制台、后台作业、消息处理）：

```csharp
using Nerosoft.Euonia.Tenancy;

services.AddTenancy(options =>
{
    options.Enabled = true;
    options.RequireTenant = true;
    options.HeaderName = "X-Tenant-Id";
})
.AddConfigurationTenantStore();   // 或 AddInMemoryTenantStore(...)
```

Web 宿主使用 `Euonia.Hosting` 的增强入口，它会额外注册 HTTP 解析器：

```csharp
using Nerosoft.Euonia.Hosting;

services.AddTenancyAspNetCore(options =>
{
    options.Enabled = true;
    options.RequireTenant = true;
});

// 管道：UseAuthentication → UseRouting → UseTenancy → UseAuthorization
app.UseRouting();
app.UseTenancy();
app.UseAuthorization();
```

`AddTenancy` 注册的内容：

| 注册项 | 生命周期 | 说明 |
|---|---|---|
| `ITenantAccessor` → `TenantAccessor` | 单例 | 读取当前租户快照 |
| `ITenantScopeFactory` → `TenantScopeFactory` | 单例 | 安装普通 / 隔离 / 跨租户作用域 |
| `ITenantResolver` → `TenantResolver` | 作用域 | 按 `Order` 收集候选并检测冲突 |
| `ITenantAccessValidator` → `DefaultTenantAccessValidator` | 作用域 | 用户能否访问该租户 |
| `IKeyTransformer` → `TenantKeyTransformer` | 单例 | 缓存键 / 锁键 / 幂等键的租户前缀 |
| `ITenantMessageTrustPolicy` → `DefaultTenantMessageTrustPolicy` | 单例 | 消息租户元数据是否可信 |
| `ITenantResolveContributor` → `MessageTenantResolveContributor` | 单例（可多个） | `Order = 100`，来自可信消息信封 |
| `TenantStartupValidator` | `IHostedService` | 启动期静态校验，失败即终止宿主 |

## 核心类型

| 类型 | 说明 |
|------|------|
| `ITenantInfo` / `TenantInfo` | 租户信息；`Id` 不可变并进入数据行/缓存键，`Identifier` 为可变的短标识（子域名等） |
| `TenantContext` | 当前异步流快照：`TenantId`、`ActorTenantId`、`TargetTenantId`、`Tenant`、`IsCrossTenant`、`CrossTenantReason` |
| `ITenantAccessor` | `Current` / `TenantId`，未安装作用域时为 `null` |
| `ITenantScopeFactory` | `Begin`、`BeginIsolated`、`BeginCrossTenant`、四参数 `Begin`（消息语义） |
| `ITenantStore` | 宿主级租户查询：`FindByIdAsync`、`FindByIdentifierAsync`、`ListAsync`、`IsActiveAsync` |
| `ConfigurationTenantStore` / `InMemoryTenantStore` / `CachedTenantStore` | 三种存储实现；缓存装饰器带 TTL 与负缓存 |
| `ITenantResolver` / `ITenantResolveContributor` | 按优先级收集候选、检测冲突，返回 `TenantResolutionResult` |
| `ITenantAccessValidator` | `CanAccessAsync(ClaimsPrincipal?, ITenantInfo, CancellationToken)` |
| `TenantEntryGuard` | 纯状态写入规则，供 EF/Mongo 适配层调用 |
| `TenantEntryContext` / `TenantEntryOperation` | 写入守卫的输入结构与操作枚举 |
| `TenantCacheKey` / `TenantKeyTransformer` / `TenantIdValidator` | 缓存键规范 `t:{tenantId}:{key}`、`IKeyTransformer` 实现、租户标识校验（最长 64） |
| `TenantFilterNames` | EF Core 命名过滤器：`Tenant`、`Tombstone` |
| `ITenantConnectionStringResolver<TContext>` | 按租户解析连接串（`DatabasePerTenant`） |
| `TenantMessageTrustPolicy` | 消息来源信任判定（`inmemory` / `outbox` / `inbox` / `deadletter` 恒可信） |
| `TenantException` 家族 | `TenantRequired` / `TenantInvalid` / `TenantNotFound` / `TenantSuspended` / `TenantAccessDenied` / `TenantStoreUnavailable` / `CrossTenantOperation` |

## 配置项

配置节常量：`MultiTenancyOptions.ConfigurationSection` = `Euonia:MultiTenancy`。

| 键 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | `bool` | `false` | 是否启用多租户；关闭时中间件与启动校验都不生效 |
| `RequireTenant` | `bool` | `true` | 无候选值时是否失败；`false` 表示允许无租户请求继续 |
| `ValidateModelCacheKeyFactory` | `bool` | `true` | `SharedSchema` 下是否校验模型缓存键工厂 |
| `HeaderName` | `string` | `X-Tenant-Id` | 租户请求头名称；同时用于回写响应头 |
| `ClaimType` | `string` | `tenant` | 承载活动租户的声明类型 |
| `HostFormat` | `string?` | `null` | 子域名格式，如 `{tenant}.example.com`；`null` 表示不启用 |
| `RouteKey` | `string?` | `tenant` | 路由租户参数名；`null` 表示不启用 |
| `DefaultTenantId` | `string?` | `null` | 单租户兼容模式的默认租户；无候选值时兜底 |
| `DefaultIsolation` | `TenantIsolation` | `SharedTable` | `SharedTable` / `SharedSchema` / `DatabasePerTenant` |
| `StoreCacheDuration` | `TimeSpan` | `00:01:00` | 租户存储缓存时长；未命中负缓存取 `min(5s, 该值)` |
| `TrustHeaderForAnonymous` | `bool` | `false` | 匿名请求是否信任租户请求头 |
| `EmitResponseHeader` | `bool` | `false` | 是否在响应中回写 `HeaderName` |
| `ExposeTenantExistence` | `bool` | `false` | 错误响应是否区分「不存在 / 停用 / 无权」 |
| `CrossTenantRoles` | `string[]` | `[]` | 可进入跨租户作用域的平台管理员角色 |
| `CrossTenantScopeAuthorizer` | `Func<string?, bool>?` | `null` | 跨租户授权回调；未配置时拒绝所有跨租户作用域 |
| `AllowSystemCrossTenant` | `bool` | `false` | 是否允许无操作人身份的系统作业进入全租户（target 为空）作用域 |
| `ExcludedPaths` | `string[]` | `/health`、`/metrics`、`/swagger` | 跳过租户要求的路径前缀（按路径段边界匹配） |
| `ConnectionStringTemplate` | `string?` | `null` | `DatabasePerTenant` 的连接串模板 |
| `TrustBrokerTenant` | `bool` | `false` | 是否信任 Broker 消息携带的租户元数据 |
| `TrustedTransports` | `string[]` | `[]` | 可信 Broker 传输名；仅 `TrustBrokerTenant=true` 时生效 |
| `InactiveTenantMessagePolicy` | `TenantInactiveMessagePolicy` | `DeadLetter` | 停用/归档租户的存量消息策略：`DeadLetter` / `Skip` / `Process` |
| `AllowInactiveTenantWrites` | `bool` | `false` | `Process` 策略下是否允许写停用租户 |
| `Tenants` | `List<TenantInfo>` | `[]` | 配置存储的租户列表（`ConfigurationTenantStore` 的数据源） |

`Tenants` 的元素字段：`Id`（必填）、`Identifier`、`Name`、`Status`（`Active` / `Suspended` / `Archived`）、`Isolation`、`ConnectionString`、`Schema`。

```json
{
  "Euonia": {
    "MultiTenancy": {
      "Enabled": true,
      "RequireTenant": true,
      "HeaderName": "X-Tenant-Id",
      "HostFormat": "{tenant}.example.com",
      "DefaultIsolation": "SharedTable",
      "StoreCacheDuration": "00:00:30",
      "CrossTenantRoles": [ "platform-admin" ],
      "Tenants": [
        { "Id": "t_acme", "Identifier": "acme", "Name": "Acme", "Status": "Active" }
      ]
    }
  }
}
```

```bash
# .NET 原生：双下划线表示层级
Euonia__MultiTenancy__Enabled=true
Euonia__MultiTenancy__RequireTenant=false
Euonia__MultiTenancy__Tenants__0__Id=t_acme
Euonia__MultiTenancy__Tenants__0__Identifier=acme

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_MULTITENANCY_ENABLED=true
EUONIA_MULTITENANCY_REQUIRETENANT=false
EUONIA_MULTITENANCY_TENANTS_0_ID=t_acme
```

## 使用示例

### 切换 / 隔离 / 跨租户作用域

```csharp
using Nerosoft.Euonia.Tenancy;

public sealed class ReportService(ITenantStore store, ITenantScopeFactory scopeFactory)
{
    // 同一异步流内切换租户：已有不同租户作用域时抛 TenantScopeConflictException
    public async Task<Report> BuildAsync(string tenantId, CancellationToken cancellationToken)
    {
        var tenant = await store.FindByIdAsync(tenantId, cancellationToken)
                     ?? throw new TenantNotFoundException(tenantId);

        using var scope = scopeFactory.Begin(tenant);
        return await BuildCoreAsync(scope.Context, cancellationToken);
    }

    // 后台任务：不继承当前流的身份信息
    public Task<Report> BuildIsolatedAsync(ITenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.BeginIsolated(tenant);
        return BuildCoreAsync(scope.Context, cancellationToken);
    }

    // 跨租户读取：必须由 CrossTenantScopeAuthorizer 或 AllowSystemCrossTenant 放行
    public Task<Report> BuildCrossTenantAsync(string targetTenantId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.BeginCrossTenant($"platform-report:{targetTenantId}", targetTenantId);
        return BuildCoreAsync(scope.Context, cancellationToken);
    }

    private Task<Report> BuildCoreAsync(TenantContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
}
```

### 写入守卫（供 EF Core / Mongo 适配层调用）

```csharp
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Tenancy;

// entry 为适配层拿到的 EntityEntry
var tenantId = TenantEntryGuard.Validate(new TenantEntryContext(
    operation: TenantEntryOperation.Added,
    entityTenantId: null,
    originalTenantId: null,
    scopeTenantId: AmbientExecutionContext.Current?.TenantId,
    targetTenantId: null,
    scopeTenantStatus: TenantStatus.Active,
    isCrossTenant: AmbientExecutionContext.Current?.IsCrossTenant ?? false,
    allowInactiveWrites: false));

if (tenantId != null)
{
    entry.Property("TenantId").CurrentValue = tenantId;
}
```

## 注意事项 / 限制

- 一个异步流最多一个开启的租户作用域；切换租户必须用 `BeginIsolated` 或 `BeginCrossTenant`，否则抛 `TenantScopeConflictException`。
- 全租户跨租户作用域（`targetTenantId` 为 `null`）**默认只读**；写入必须进入目标租户作用域。
- 租户存储必须是宿主级的，不能依赖当前租户的 `DbContext`；`Enabled=true` 但未注册 `ITenantStore` 时启动即失败。
- `Enabled=false` 不会静默返回空数据：宿主适配层应在启动期检测租户模型并快速失败。
- 跨租户作用域默认全部拒绝；未配置 `CrossTenantScopeAuthorizer` 且未开启 `AllowSystemCrossTenant` 时一定会抛 `UnauthorizedAccessException`。
- `TenantKeyTransformer` 仅在 `AmbientExecutionContext` 中存在租户时加 `t:{tenantId}:` 前缀，前缀抑制由 `KeyTransformSuppression` 控制。
- `CachedTenantStore` 的缓存有 1024 条上界与版本号失效机制，`Invalidate(tenantId)` 需由写租户的入口主动调用。
- `TenantStartupValidator` 只做静态校验（存储缺失、`DefaultTenantId` 非法、`CrossTenantRoles` 含空值、`DatabasePerTenant` 缺模板）；租户模型扫描由 EF 适配层在首次创建上下文时完成。
- `DatabasePerTenant` 依赖 `ConnectionStringTemplate` 或每个租户的 `ConnectionString`，两者都没有时启动失败。

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— `AmbientExecutionContext` 与执行上下文
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— `AddTenancyAspNetCore` / `UseTenancy` 与 HTTP 解析器
- [`Euonia.Security`](../Euonia.Security/README.md) —— 用户主体与角色判定
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— 过滤器与写入守卫的持久化适配
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— Mongo 侧租户适配
- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 消息租户元数据与信任策略
- [`Euonia.Telemetry`](../Euonia.Telemetry/README.md) —— 租户标签来源

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
