# Euonia.Security API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

操作权限与行级数据权限的策略引擎：预定义判定语义，授权值由使用方从数据实时解析；读侧下推与单行判定共用同一份编译结果。本页是 API 速查；设计取舍见 [项目 README](../../../Source/Euonia.Security/README.md)。

- **NuGet 包**：`Euonia.Security`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Security`（`AddPermission` / `PermissionOptions` 在 `Microsoft.Extensions.DependencyInjection`）
- **依赖**：`Euonia.Core`、`Microsoft.Extensions.DependencyInjection.Abstractions`（不依赖 EF Core）
- **完整参考**：[Source/Euonia.Security/README.md](../../../Source/Euonia.Security/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Security/README.md) —— 策略代数、下推、启动期校验与故障排查
- [Euonia.Core 用户手册](../Euonia.Core/API.md) —— `UserPrincipal` / `IPermissionChecker` / `[Permission]` 契约
- [Euonia.Osba 用户手册](../Euonia.Osba/API.md) —— 用 `AddPermission` 把行级策略接到业务对象边界
- [Euonia.Endpoint.Http 用户手册](../Euonia.Endpoint.Http/API.md) —— 在 HTTP 边界调用判定
- [仓库根 README](../../../README.md)
