# Euonia.Tenancy 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

多租户内核：解析租户 → 安装作用域 → 用写入守卫保证行级隔离，跨租户访问必须显式授权。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Tenancy`（HTTP 适配见 `Euonia.Hosting`） |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Tenancy` |
| 依赖 | `Euonia.Core`（`AmbientExecutionContext`）+ `Microsoft.Extensions.*`；不引用 EF Core / ASP.NET Core |
| 完整参考 | [`Source/Euonia.Tenancy/README.md`](../../../Source/Euonia.Tenancy/README.md) |

## 教程

### 场景

一个多租户库存 API：每个请求解析出租户并安装作用域；EF Core 保存时由 `TenantEntryGuard` 保证只能写自己租户的行；平台管理员通过一次显式跨租户作用域出汇总报表。

前置条件：.NET SDK 支持 `net10.0` / `net11.0`；一个可用的 EF Core 数据库（示例用 SQLite）。租户身份保存在 `AmbientExecutionContext` 中，不是私有 AsyncLocal。

### 1. 安装

```bash
dotnet add package Euonia.Tenancy
dotnet add package Euonia.Hosting        # 需要 HTTP 解析器与 UseTenancy 时
```

### 2. 接线：解析 + 作用域

```csharp
using Nerosoft.Euonia.Hosting;   // AddTenancyAspNetCore / UseTenancy
using Nerosoft.Euonia.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTenancyAspNetCore(options =>
    {
        options.Enabled = true;
        options.RequireTenant = true;
        options.HeaderName = "X-Tenant-Id";
        // 仅开发环境信任匿名租户头；生产改用 ClaimType / HostFormat。
        options.TrustHeaderForAnonymous = builder.Environment.IsDevelopment();
        options.StoreCacheDuration = TimeSpan.FromSeconds(30);
    })
    .AddConfigurationTenantStore();          // 读 Euonia:MultiTenancy:Tenants

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.UseAuthentication();
app.UseRouting();
app.UseTenancy();          // 必须在 UseAuthentication 之后
app.UseAuthorization();

app.MapGet("/stock", (ITenantAccessor tenant) => Results.Ok(new { tenant = tenant.TenantId }));
app.Run();
```

`appsettings.json`：

```json
{
  "ConnectionStrings": { "Default": "Data Source=inventory.db" },
  "Euonia": {
    "MultiTenancy": {
      "Enabled": true,
      "RequireTenant": true,
      "HeaderName": "X-Tenant-Id",
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
Euonia__MultiTenancy__RequireTenant=true

# 仓库 alias：大写单下划线（Euonia.Modularity）
EUONIA_MULTITENANCY_ENABLED=true
EUONIA_MULTITENANCY_REQUIRETENANT=true
```

### 3. 写入隔离：把守卫接进 `SaveChanges`

```csharp
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Tenancy;

public interface ITenantOwned
{
    string? TenantId { get; set; }
}

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantAccessor tenant)
    : DbContext(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ApplyTenantRules()
    {
        var context = tenant.Current;
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            var operation = entry.State switch
            {
                EntityState.Added => TenantEntryOperation.Added,
                EntityState.Modified => TenantEntryOperation.Modified,
                EntityState.Deleted => TenantEntryOperation.Deleted,
                _ => TenantEntryOperation.None
            };

            var tenantId = TenantEntryGuard.Validate(new TenantEntryContext(
                operation,
                entityTenantId: entry.Entity.TenantId,
                originalTenantId: entry.OriginalValues.TryGetValue(nameof(ITenantOwned.TenantId), out var original)
                    ? original as string
                    : null,
                scopeTenantId: context?.TenantId,
                targetTenantId: context?.TargetTenantId,
                scopeTenantStatus: context?.Tenant?.Status ?? TenantStatus.Active,
                isCrossTenant: context?.IsCrossTenant ?? false,
                allowInactiveWrites: false));

            if (tenantId != null)
            {
                entry.Entity.TenantId = tenantId;   // 新增行自动打上当前租户
            }
        }
    }
}
```

守卫规则：跨租户写入抛 `CrossTenantOperationException`；全租户作用域只读；停用租户写入抛 `TenantSuspendedException`（`TenantEntryGuard.cs:15-54`）。

### 4. 跨租户报表：显式授权

```csharp
// 授权回调在 DI 里装配，避免在选项委托里写死 true。
builder.Services.AddOptions<MultiTenancyOptions>()
       .Configure<PlatformAdminGate>((options, gate) =>
           options.CrossTenantScopeAuthorizer = gate.CanEnter);

public sealed class PlatformReportService(ITenantScopeFactory scopes)
{
    public Task<string> BuildAsync(string tenantId, CancellationToken cancellationToken)
    {
        // 目标租户非空：写入也被允许；传 null 则是全租户只读模式。
        using var scope = scopes.BeginCrossTenant($"platform-report:{tenantId}", tenantId);
        return Task.FromResult(scope.Context.TenantId ?? "-");
    }
}
```

### 5. 验证

```bash
curl -s -i http://localhost:5000/stock -H 'X-Tenant-Id: t_acme'    # 200 {"tenant":"t_acme"}
curl -s -i http://localhost:5000/stock                              # 400 TenantRequired
curl -s -i http://localhost:5000/stock -H 'X-Tenant-Id: t_unknown'  # 404 TenantNotFound
```

预期：第一条返回 `{"tenant":"t_acme"}`，第二条与第三条分别是 `400` / `404`，响应体带 `TenantRequired` / `TenantNotFound` 错误码。

### 常见失败点

- **启动即抛 `ConfigurationException`**：开了 `Enabled` 但没注册 `ITenantStore`（`TenantStartupValidator.cs:33-42`），补 `AddConfigurationTenantStore()`。
- **所有请求 400**：`UseTenancy` 放在 `UseAuthentication` 之前，匿名时租户头被忽略（`HeaderTenantResolver.cs:30-34`）。
- **400 TenantConflict**：多个来源给出不同租户；只保留一种来源（关掉 `HostFormat` / `RouteKey`）。
- **409 冲突**：同一异步流里已装另一个租户作用域，改用 `BeginIsolated` 或 `BeginCrossTenant`。
- **改了租户状态仍可访问**：`CachedTenantStore` 有 `StoreCacheDuration` 缓存（默认 60 秒），写入口要调 `Invalidate(id)`。

## 相关文档

- [API 参考](API.md)
- [`Euonia.Tenancy` README](../../../Source/Euonia.Tenancy/README.md) —— 全部 23 个选项与完整语义
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `AddTenancyAspNetCore` / `UseTenancy` 与管道顺序
- [Telemetry 手册](../Euonia.Telemetry/TUTORIAL.md) —— 租户标签如何进入 Trace
- [`Source/Euonia.Security/README.md`](../../../Source/Euonia.Security/README.md) —— `UserPrincipal` 与角色判定
- [根 README](../../../README.md)
