# Euonia.Auditing 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Auditing` 提供审计作用域、动作模型、异步写入队列、脱敏序列化与 InMemory 存储；持久化由 EF Core / Mongo 适配包完成。

- **包名**：`Euonia.Auditing`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing`
- **完整选项与边界**：[Source/Euonia.Auditing/README.md](../../../Source/Euonia.Auditing/README.md)

## 教程：HTTP 请求审计落库与查询

这个场景用 ASP.NET Core + EF Core 审计库：安装审计内核与 EF 存储，给应用服务方法标注 `[Audited]`，在管道中启用 `UseAuditing`，最后通过 `IAuditingStoreQuery` 查询失败记录。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已安装 `Euonia.Auditing`、`Euonia.Auditing.EfCore`、`Euonia.Hosting`、`Euonia.Application` 与 `Microsoft.EntityFrameworkCore.Sqlite`。
- 示例用 `EnsureCreated` 建审计库；生产环境改用迁移。

### 1. 创建项目并安装

```bash
dotnet new web -n OrdersApi
cd OrdersApi
dotnet add package Euonia.Auditing
dotnet add package Euonia.Auditing.EfCore
dotnet add package Euonia.Hosting
dotnet add package Euonia.Application
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

### 2. 模块接线

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(ApplicationModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAuditing(options =>
        {
            options.Enabled = true;
            options.ApplicationName = "Orders";
            options.RecordParameters = true;
            // 仅示例：允许跨租户查询；生产环境按合规策略实现。
            options.CrossTenantQueryAuthorizer = _ => true;
        });

        context.Services.AddAuditingEfCore<AuditingDbContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("Audit")));

        context.Services.AddProxiedScoped<IOrderAppService, OrderAppService>();
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

### 3. 被审计的应用服务

```csharp
using Nerosoft.Euonia.Auditing;

public interface IOrderAppService
{
    Task<long> CreateAsync(string number, CancellationToken cancellationToken);
}

public sealed class OrderAppService : IOrderAppService
{
    [Audited(ActionName = "创建订单")]
    public Task<long> CreateAsync(string number, CancellationToken cancellationToken)
        => Task.FromResult((long)ObjectId.Snowflake());
}
```

### 4. 管道、端点与查询

```csharp
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Hosting;

var app = builder.Build();

app.UseExecutionContext();
app.UseAuditing();          // 必须在租户中间件之前（如使用 UseTenancy）

app.MapPost("/orders", (IOrderAppService service, CancellationToken cancellationToken) =>
    service.CreateAsync("SO-1", cancellationToken));

app.MapGet("/audit", async (
    IAuditingRecorder recorder,
    IAuditingStoreQuery store,
    CancellationToken cancellationToken) =>
{
    // 默认 SuccessDelivery=Batched，查询前先排空写入队列。
    await recorder.FlushAsync(cancellationToken);
    var records = await store.QueryAsync(new AuditingQuery
    {
        AllowCrossTenant = true,
        CrossTenantReason = "demo",
        Take = 20
    }, cancellationToken);
    return Results.Ok(records);
});

app.Run();
```

### 5. 运行与验证

```bash
dotnet run
curl -X POST http://localhost:5000/orders
curl http://localhost:5000/audit
```

预期 `/audit` 返回至少一条 `AuditingRecord`，其中 `Actions` 含“创建订单”动作，`ScopeId` 形如 `t1:req:...`（无租户时为 `-:req:...`）。把服务方法改成抛异常，再查询应看到 `IsSuccess=false` 的记录。

### 常见失败点

- **没有记录**：未注册 `IAuditingStore`、`Enabled=false`、请求未经过 `UseAuditing`，或查询前没有 `FlushAsync`。
- **特性不生效**：`[Audited]` 需要应用服务经过接口代理；模块依赖 `ApplicationModule` 并用 `AddProxiedScoped` 注册。
- **写入失败但接口 200**：审计失败开放，存储异常只记日志；检查审计库表、连接串与写入器日志。
- **跨租户查询被拒**：必须同时提供 `AllowCrossTenant` 与 `CrossTenantReason`，并由 `CrossTenantQueryAuthorizer` 放行。

## 相关文档

- [API 参考](API.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Auditing/README.md)
- [Euonia.Auditing.EfCore 用户手册](../Euonia.Auditing.EfCore/TUTORIAL.md) —— 审计表结构与幂等落库
- [Euonia.Uow 用户手册](../Euonia.Uow/TUTORIAL.md) —— 业务事务边界
- [Euonia.Domain 用户手册](../Euonia.Domain/TUTORIAL.md) —— 被审计的领域对象
- [仓库根 README](../../../README.md)
