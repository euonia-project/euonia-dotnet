# Euonia.Tenancy API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

多租户内核：解析租户 → 安装作用域 → 用写入守卫保证行级隔离，跨租户访问必须显式授权。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Tenancy`（HTTP 适配见 `Euonia.Hosting`） |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Tenancy` |
| 依赖 | `Euonia.Core`（`AmbientExecutionContext`）+ `Microsoft.Extensions.*`；不引用 EF Core / ASP.NET Core |
| 完整参考 | [`Source/Euonia.Tenancy/README.md`](../../../Source/Euonia.Tenancy/README.md) |

## API 参考

| 类型 / 成员 | 说明 | 关键参数 / 默认值 |
|---|---|---|
| `AddTenancy(IServiceCollection, Action<MultiTenancyOptions>?)` | 注册内核服务 | 自动绑定 `Euonia:MultiTenancy` |
| `AddTenancyAspNetCore(...)`（`Euonia.Hosting`） | 额外注册 Claim/Host/Route/Header 解析器 | 内部先调 `AddTenancy` |
| `AddConfigurationTenantStore()` | 从 `Tenants` 列表建存储 | 带 `CachedTenantStore` 装饰 |
| `ITenantAccessor.Current` / `TenantId` | 读当前租户快照 | 未装作用域时为 `null` |
| `ITenantScopeFactory.Begin(ITenantInfo)` | 进入租户作用域 | 已存在不同租户时抛 `TenantScopeConflictException` |
| `ITenantScopeFactory.BeginIsolated(ITenantInfo)` | 全新快照，不继承身份 | 后台任务用 |
| `ITenantScopeFactory.BeginCrossTenant(string reason, string? targetTenantId = null)` | 跨租户作用域 | `targetTenantId=null` 即全租户只读 |
| `ITenantScopeFactory.Begin(ITenantInfo, string?, string?, string?)` | 消息信封语义 | 四参数必填；跨租户走授权回调 |
| `ITenantStore` | 宿主级查询 | `FindByIdAsync`/`FindByIdentifierAsync`/`ListAsync`/`IsActiveAsync` |
| `TenantEntryGuard.Validate(in TenantEntryContext)` | 纯状态写入校验 | 返回应写入的租户标识或 `null` |
| `TenantFailureCodes` | 错误码常量 | `Required`/`Invalid`/`NotFound`/`Conflict` 等 |

### 配置项

节名 `Euonia:MultiTenancy`，共 23 个可写属性；教程用到的关键项：

| 键 | 说明 | 默认值 |
|---|---|---|
| `Enabled` | 是否启用 | `false` |
| `RequireTenant` | 无候选值是否失败 | `true` |
| `HeaderName` / `ClaimType` | 请求头 / 声明类型 | `X-Tenant-Id` / `tenant` |
| `StoreCacheDuration` | 存储缓存 TTL | `00:01:00` |
| `TrustHeaderForAnonymous` | 匿名是否信任租户头 | `false` |
| `CrossTenantRoles` / `CrossTenantScopeAuthorizer` | 跨租户角色与授权回调 | `[]` / `null` |
| `Tenants` | 配置存储的租户列表 | `[]` |

## 相关文档

- [教程](TUTORIAL.md)
- [`Euonia.Tenancy` README](../../../Source/Euonia.Tenancy/README.md) —— 全部 23 个选项与完整语义
- [`Source/Euonia.Hosting/README.md`](../../../Source/Euonia.Hosting/README.md) —— `AddTenancyAspNetCore` / `UseTenancy` 与管道顺序
- [Telemetry 手册](../Euonia.Telemetry/API.md) —— 租户标签如何进入 Trace
- [`Source/Euonia.Security/README.md`](../../../Source/Euonia.Security/README.md) —— `UserPrincipal` 与角色判定
- [根 README](../../../README.md)
