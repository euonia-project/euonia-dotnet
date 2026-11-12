# Euonia.Security 用户手册

操作权限与行级数据权限的策略引擎：预定义判定语义，授权值由使用方从数据实时解析；读侧下推与单行判定共用同一份编译结果。本页是端到端教程与 API 速查；设计取舍见 [项目 README](../../Source/Euonia.Security/README.md)。

- **NuGet 包**：`Euonia.Security`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Security`（`AddPermission` / `PermissionOptions` 在 `Microsoft.Extensions.DependencyInjection`）
- **依赖**：`Euonia.Core`、`Microsoft.Extensions.DependencyInjection.Abstractions`（不依赖 EF Core）
- **完整参考**：[Source/Euonia.Security/README.md](../../Source/Euonia.Security/README.md)

## 教程

### 场景：订单列表的行级可见性

订单表要按「部门」做行级隔离：用户只能看到本部门订单；删除操作再叠加一条「按订单标识」的授予。教程用内存数组代替数据库，验证 `Apply`（下推）与 `Allows`（单行）给出同一结论。

### 前置条件

.NET 10 SDK；控制台项目；无需数据库——`Apply` 只生成表达式，`AsQueryable()` 即可验证。

### 1. 安装

```bash
dotnet new console -n OrderScope && cd OrderScope
dotnet add package Euonia.Security
```

### 2. 声明资源与权限模型

```csharp
using Nerosoft.Euonia.Security;

public sealed class Order
{
    public string Id { get; init; } = string.Empty;
    public string DeptId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
}

// 模型与策略必须写在同一个声明类型里
public sealed class OrderScope : ScopeModel<Order>
{
    public override void Define(ScopeModelBuilder<Order> builder)
        => builder.Map(ScopeDimensions.Dept, x => x.DeptId)   // 维度取值来自资源行
                  .Map("order", x => x.Id);

    // 默认策略：部门可见（读取时未声明标识 → 回落到这里）
    public override ScopePolicy<Order> Policy => ScopePolicy<Order>.Grant(ScopeDimensions.Dept);

    // 删除：按订单标识授予
    public override void Declare(ScopePolicySet<Order> policies)
        => policies.ForOperation(BusinessOperation.Delete, ScopePolicy<Order>.Grant("order"), "order:delete");
}
```

### 3. 实现授权数据来源并接线

```csharp
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

// 授权值必须实时解析：这里用内存数据模拟「用户属于哪些部门」
public sealed class DeptSubjectResolver : IScopeSubjectResolver
{
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken ct = default)
        => ValueTask.FromResult(ScopeSubjectSet.CreateBuilder()
                                                .AddSelf("u-1")
                                                .Add(ScopeDimensions.Dept, "d-1")
                                                .AddGrant("order:delete", "order", "o-1")
                                                .Build());
}

var services = new ServiceCollection();

services.AddPermission(permission => permission
        .Scan(typeof(Order).Assembly)
        .OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute)));

services.AddSingleton(new UserPrincipal(
    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u-1")], "Test"))));

services.AddScoped<IScopeSubjectResolver, DeptSubjectResolver>();

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

// 首次解析守卫时执行启动期校验：缺 resolver / UserPrincipal 会在这里失败
var guard = scope.ServiceProvider.GetRequiredService<IScopeGuard>();
await guard.EnsureResolvedAsync();

var orders = new[]
{
    new Order { Id = "o-1", DeptId = "d-1", OwnerId = "u-1" },
    new Order { Id = "o-2", DeptId = "d-2", OwnerId = "u-2" },
}.AsQueryable();

var visible = guard.Apply(orders).ToList();          // 表达式下推，SQL 由 ORM 翻译
Console.WriteLine($"visible={string.Join(",", visible.Select(o => o.Id))}");
Console.WriteLine($"allows(o-1)={guard.Allows(orders.First(o => o.Id == "o-1"))}");
Console.WriteLine($"allows(o-2)={guard.Allows(orders.First(o => o.Id == "o-2"))}");
Console.WriteLine($"delete(o-1)={guard.Allows(orders.First(o => o.Id == "o-1"), "order:delete")}");
```

> `FetchAttribute` 是应用自定义的入口特性（`[AttributeUsage(AttributeTargets.Method)]`）；引擎只按规则收集 `[Permission]`，不生成权限码。

### 4. 运行与验证

```bash
dotnet run
# 期望输出：
# visible=o-1
# allows(o-1)=True
# allows(o-2)=False
# delete(o-1)=True
```

把 `.Add(ScopeDimensions.Dept, "d-1")` 删掉，`visible` 会变成空——授权数据缺失即 fail-closed，不会回退成「全放行」。

### 常见失败点

- **「未注册 `IScopeSubjectResolver`」**：声明了模型或权限码却没接授权数据源；`AddScoped<IScopeSubjectResolver, ...>()` 必须在解析守卫前完成。
- **「权限配置是空的」**：`AddPermission` 回调什么都没表达；给入口规则、`Source`、`Scan` 或 `NoOperationCodes()` 之一。
- **「策略引用了未映射的维度」**：策略里的 `Grant(d)` 必须在 `Define` 里 `Map(d, ...)`。
- **同步判定抛 `IDS_SCOPE_NOT_RESOLVED`**：先 `await guard.EnsureResolvedAsync(ct)`，同步读不会隐式等待。
- **`Apply` 后 SQL 没有 WHERE**：在投影成 DTO 之后才调用，或对 `IEnumerable` 而非 `IQueryable` 调用。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddPermission(Action<PermissionOptions>)` | 唯一注册入口，注册期完成模型扫描与校验 | 可多次调用，贡献取并集 |
| `PermissionOptions.Scan(params Assembly[])` | 追加扫描程序集 | 可多次，程序集取并集 |
| `PermissionOptions.OnAttributeOrName(string operation, string namePrefix, params Type[])` | 声明方法入口规则 | 按特性名或约定名匹配 |
| `PermissionOptions.OnMethod(string operation, Func<MethodInfo,bool>)` | 任意谓词的逃生舱 | 与 `OnAttributeOrName` 可混用 |
| `PermissionOptions.Source(IPermissionCodeSource)` | 自定义权限码来源 | 与入口规则互斥 |
| `PermissionOptions.NoOperationCodes()` / `NoModels()` | 显式断言「没有方法级权限码 / 没有模型」 | 不能与相反配置同时出现 |
| `ScopeModel<T>` | 资源模型：`Define` / `Policy` / `Declare` | 必须公共无参构造 |
| `ScopeModelBuilder<T>.Map(string, Expression<Func<T,string>>)` | 维度取值来自资源行 | 至少声明一个维度 |
| `ScopeModelBuilder<T>.MapMany(string, Expression<Func<T,IEnumerable<string>>>)` | 子表维度（下推为 `EXISTS`） | 仅接受「导航集合再取字符串」 |
| `ScopePolicy<T>.Grant(string dimension)` / `Self()` | 授予维度 / 本人 | `Self()` 等价于 `Grant(owner)` |
| `ScopePolicy<T>.Any` / `All` / `Deny` / `Where` | 策略代数 | 结论为 `Allow && !Deny` |
| `ScopePolicySet<T>.ForOperation(string, ScopePolicy<T>, string? scopeKey)` | 按操作声明策略 | `For(code, ...)` 按权限码声明 |
| `IScopeGuard` | `Apply` / `Allows` / `AllowsObject` / `Explain` / `EnsureResolvedAsync` / `RefreshAsync` / `GetSubjects` | Scoped，一次请求解析一次 |
| `IPermissionChecker` | `IsGranted(string)` / `IsInRole(string)` / `EnsureResolvedAsync(ct)` | 默认实现 `SubjectPermissionChecker` |
| `IScopeSubjectResolver.ResolveAsync(ClaimsPrincipal?, ct)` | 授权值唯一来源 | 返回 `ValueTask<ScopeSubjectSet>` |
| `ScopeSubjectSet.CreateBuilder()` | `AddSelf` / `Add` / `AddGrant` / `AddCodes` / `Build` | 维度名大小写不敏感 |
| `ScopeDimensions` | 内置维度名常量 | `Owner`、`Dept`、`Team`、`Member`、`Region`、`Project` |

本包**没有配置节**（不读取 `IConfiguration`）：`PermissionOptions` 全部在 `AddPermission` 回调里以代码声明，不需要 `appsettings` 与环境变量。

## 相关文档

- [项目 README](../../Source/Euonia.Security/README.md) —— 策略代数、下推、启动期校验与故障排查
- [Euonia.Core 用户手册](Euonia.Core.md) —— `UserPrincipal` / `IPermissionChecker` / `[Permission]` 契约
- [Euonia.Osba 用户手册](Euonia.Osba.md) —— 用 `AddPermission` 把行级策略接到业务对象边界
- [Euonia.Endpoint.Http 用户手册](Euonia.Endpoint.Http.md) —— 在 HTTP 边界调用判定
- [仓库根 README](../../README.md)
