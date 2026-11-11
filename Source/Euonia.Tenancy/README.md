# Euonia.Tenancy

Euonia 多租户能力内核：租户上下文、解析链、存储、隔离模式，以及 EF Core / Mongo / 消息 / 后台任务的租户适配契约。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Tenancy`
- **NuGet 包**：`Euonia.Tenancy`

## 定位

`Euonia.Tenancy` 只依赖 `Euonia.Core` 与少量 `Microsoft.Extensions.*` 抽象包，不引用 EF Core、ASP.NET Core、MongoDB 或任何 Euonia 上层项目。持久化与宿主适配由各上层项目反向引用本包完成。

## 快速开始

```csharp
services.AddTenancy(options =>
{
    options.RequireTenant = true;
    options.HostFormat = "{tenant}.example.com";
})
.AddConfigurationTenantStore();
```

`Euonia:MultiTenancy:Tenants` 配置示例：

```json
{
  "Euonia": {
    "MultiTenancy": {
      "Enabled": true,
      "RequireTenant": true,
      "Tenants": [
        { "Id": "t_acme", "Identifier": "acme", "Name": "Acme", "Status": "Active" }
      ]
    }
  }
}
```

## 核心契约

| 类型 | 说明 |
|------|------|
| `ITenantInfo` / `TenantInfo` | 租户信息；标识不可变，短标识仅用于解析 |
| `ITenantAccessor` | 读取当前租户快照（来自 `Nerosoft.Euonia.Execution`） |
| `ITenantScopeFactory` | 安装普通 / 隔离 / 跨租户作用域 |
| `ITenantResolver` / `ITenantResolveContributor` | 按优先级收集候选并检测冲突 |
| `ITenantStore` | 宿主级租户查询；配置 / 内存 / 缓存实现随包提供 |
| `ITenantAccessValidator` | 用户是否有权访问租户 |
| `TenantEntryGuard` | 纯状态写入规则，供 EF/Mongo 适配层调用 |
| `TenantCacheKey` / `TenantKeyTransformer` | 租户缓存键规范与 `IKeyTransformer` 实现 |

## 注意事项

- 租户存储必须是宿主级的，不能依赖当前租户的 `DbContext`。
- 一个异步流最多一个开启的租户作用域；切换租户必须使用 `BeginCrossTenant` 或 `BeginIsolated`。
- 全租户跨租户作用域默认只读；写入必须使用目标租户模式。
- `Enabled=false` 不会静默返回空数据：宿主适配层应在启动期检测租户模型并快速失败。
