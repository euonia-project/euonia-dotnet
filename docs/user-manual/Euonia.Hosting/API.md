# Euonia.Hosting API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Hosting` 负责 ASP.NET Core 宿主启动、模块初始化、请求上下文与 JWT/IdentityServer 认证，并提供租户、审计、遥测中间件的接入扩展。

- **包名**：`Euonia.Hosting`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting`（扩展位于 `Microsoft.Extensions.DependencyInjection`、`Microsoft.AspNetCore.Builder` 等）
- **完整选项/边界**：[Source/Euonia.Hosting/README.md](../../../Source/Euonia.Hosting/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `HostUtility.Run<TStartup>` / `RunAsync<TStartup>` | 构建并运行宿主；内部使用 `UseStartup<TStartup>` | `TStartup` 为类；异步版返回 `Task` |
| `HostBuilderOptions.EnableHttp2` | 是否启用 HTTP/2 并设置相关开关 | 默认 `true` |
| `HostBuilderOptions.CaptureStartupErrors` | 启动异常是否捕获后继续 | 默认 `true` |
| `HostBuilderOptions.ConfigureWebHostBuilder` / `ConfigureHostBuilder` | 自定义 Web/主机构建器 | 默认 `null`；后者在 `Build()` 前执行 |
| `HostingModule` | 注册执行上下文、请求上下文访问器、用户主体、请求追踪 | 通过 `[DependsOn]` 引入 |
| `AddJwtAuthentication(...)` | 注册 JWT Bearer：Action、配置节名、选项实例、无参四种重载 | 最推荐 `Action<JwtAuthenticationOptions>`；`SigningKey` 必填 |
| `JwtAuthenticationOptions.SigningKey` | 对称签名密钥 | 默认 `null`，必填 |
| `JwtAuthenticationOptions.Authority` / `Audience` | 颁发机构 / 受众 | 默认 `null` |
| `JwtAuthenticationOptions.UsePolicy` | 是否注册名为 `Bearer` 的策略（要求 `sub`/`name`） | 默认 `true` |
| `AddIdentityServerAuthentication(...)` | 注册 JWT + OAuth2 内省组合方案 | 可配置 `SupportedTokens` |
| `AddUserPrincipal()` | 注册当前请求的 `UserPrincipal`（Scoped） | 依赖 `IHttpContextAccessor` |
| `AddScopePolicy(policyName, params string[] scopes)` | 注册基于 scope 的授权策略 | scope 满足其一即可 |
| `RequireScope(params string[] scopes)` | 给 `AuthorizationPolicyBuilder` 添加 scope 要求 | 用于自定义策略 |
| `[AuthorizeRoles(params string[] roles)]` | 基于角色的授权特性 | 多角色逗号连接 |
| `InitializeApplication()` | 设置 `IApplicationBuilder` 并执行模块初始化 | 放在管道最前面 |
| `UseTenancy()` / `UseAuditing()` / `UseEuoniaTelemetry()` | 按选项启用租户/审计/遥测中间件 | 选项未启用时为空操作 |
| `UseExecutionContext()` | 安装执行上下文中间件 | 总是安装，放在认证之后 |
| `TenantHttpKeys` | 租户上下文/失败代码/候选值的 `HttpContext.Items` 键 | 常量字符串 |

### 配置示例：Euonia:MultiTenancy

`UseTenancy()` 读取 `Euonia:MultiTenancy`；启用多租户时可在宿主配置中提供：

```json
{
  "Euonia": {
    "MultiTenancy": {
      "Enabled": true,
      "RequireTenant": false
    }
  }
}
```

```bash
Euonia__MultiTenancy__Enabled=true
EUONIA_MULTITENANCY_ENABLED=true
```

第二种写法依赖模块化的环境变量别名规则。`RequireTenant` 默认 `true`，本示例为便于匿名访问设为 `false`；生产环境应按租户策略收紧。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、认证选项与中间件顺序](../../../Source/Euonia.Hosting/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 模块加载与初始化
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 应用服务与拦截器
- [Euonia.Validation 用户手册](../Euonia.Validation/API.md) —— 异常中的校验错误
- [仓库根 README](../../../README.md)
