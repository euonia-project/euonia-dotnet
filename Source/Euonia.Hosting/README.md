# Euonia.Hosting

Euonia 的 ASP.NET Core 托管基础设施：`HostUtility` 负责构建并运行宿主，`HostingModule` 注册请求上下文、用户主体、请求追踪，并提供 JWT 与 IdentityServer（JWT + 内省）认证、角色与 scope 授权，以及租户、审计、遥测中间件扩展。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting`（服务注册与授权扩展位于 `Microsoft.Extensions.DependencyInjection`、`Microsoft.AspNetCore.Builder`、`Microsoft.AspNetCore.Authorization`、`Microsoft.AspNetCore.Authentication`）
- **NuGet 包**：`Euonia.Hosting`

## 安装

```bash
dotnet add package Euonia.Hosting
```

## 快速开始

`Program.cs`：

```csharp
using Nerosoft.Euonia.Hosting;

HostUtility.Run<Startup>(args, options =>
{
    options.EnableHttp2 = true;
    options.CaptureStartupErrors = true;
});
```

`Startup.cs`：

```csharp
using Nerosoft.Euonia.Modularity;

public sealed class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddModularityApplication<HostModule>(configuration);
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.InitializeApplication();

        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

模块与认证：

```csharp
using Microsoft.AspNetCore.Authorization;
using Nerosoft.Euonia.Hosting;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(HostingModule))]
public sealed class HostModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddJwtAuthentication(options =>
        {
            options.Authority = "https://identity.example.com";
            options.Audience = "orders-api";
            options.SigningKey = "replace-with-a-strong-secret";
        });

        context.Services.AddAuthorization(options =>
        {
            options.AddScopePolicy("orders.read", "orders:read");
        });
    }
}
```

控制器授权：

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/orders")]
[AuthorizeRoles("admin", "operator")]
[Authorize(Policy = "orders.read")]
public sealed class OrdersController : ControllerBase
{
}
```

启用租户、审计与遥测时，按下面的顺序接入（来自示例应用的已验证顺序）：

```csharp
public void Configure(IApplicationBuilder app)
{
    app.InitializeApplication();
    app.UseAuthentication();
    app.UseExecutionContext();
    app.UseAuditing();

    app.UseRouting();

    app.UseTenancy();
    app.UseEuoniaTelemetry();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

## 主要功能

- **宿主引导**：`HostUtility.Run<TStartup>` / `RunAsync<TStartup>`，接受 `HostBuilderOptions` 或配置委托。
- **模块集成**：`[DependsOn(typeof(HostingModule))]` 注册执行上下文、请求上下文访问器、用户主体与 `IApplicationBuilder` 访问器。
- **认证**：`AddJwtAuthentication(...)` 注册 JWT Bearer；`AddIdentityServerAuthentication(...)` 同时注册 JWT 与 OAuth2 内省子方案。
- **授权**：`[AuthorizeRoles]`、`AddScopePolicy(...)`、`RequireScope(...)` 与 `ScopePolicy.Create(...)`。
- **请求管道**：`InitializeApplication()` 触发模块初始化；`UseCulture()`、`UseJwt()`、`UseDefaultRequestContextAccessor()` 提供区域性、认证回退与请求上下文安装。
- **平台中间件扩展**：`UseTenancy()`、`UseAuditing()`、`UseEuoniaTelemetry()` 按对应 `*Options.Enabled` 决定是否安装；`UseExecutionContext()` 总是安装。

## 核心类型

| 类型 | 作用 |
|------|------|
| `HostUtility` | `Run` / `RunAsync<TStartup>` 构建并运行宿主 |
| `HostBuilderOptions` | `EnableHttp2`、`CaptureStartupErrors`、`ConfigureWebHostBuilder`、`ConfigureHostBuilder` 等 |
| `HostingModule` | 注册执行上下文、请求上下文访问器、用户主体与请求追踪中间件 |
| `AddJwtAuthentication(...)` | 注册 JWT Bearer 认证（代码配置、配置节名、选项实例与无参四种重载） |
| `AddIdentityServerAuthentication(...)` | 注册 JWT + OAuth2 内省组合认证方案 |
| `JwtAuthenticationOptions` / `IdentityServerAuthenticationOptions` | 两类认证的选项 |
| `AddUserPrincipal(...)` | 注册当前请求的 `UserPrincipal` |
| `AddScopePolicy(...)` / `RequireScope(...)` | 基于 scope 的授权策略 |
| `AuthorizeRolesAttribute` | 基于角色的授权特性 |
| `UseTenancy()` / `UseAuditing()` / `UseEuoniaTelemetry()` / `UseExecutionContext()` | 租户、审计、遥测与执行上下文中间件 |
| `TenantHttpKeys` | 租户上下文、失败代码与候选值的 `HttpContext.Items` 键 |

## 选项（代码配置）

`Euonia.Hosting` 没有框架自带的配置节。`JwtAuthenticationOptions` 与 `IdentityServerAuthenticationOptions` 都由代码直接配置；`AddJwtAuthentication(string sectionName)` 只是读取**宿主已有**的配置节（名称由调用方提供，例如示例应用中的 `JwtAuthenticationOptions`），不会自动创建该节，也不会定义新的框架配置节。

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `JwtAuthenticationOptions.SigningKey` | `string` | `null` | 必填；对称签名密钥 |
| `JwtAuthenticationOptions.Authority` | `string?` | `null` | 颁发机构 URL |
| `JwtAuthenticationOptions.Audience` | `string?` | `null` | 受众 |
| `JwtAuthenticationOptions.Issuer` | `IEnumerable<string>?` | `null` | 写入 `ValidIssuers` |
| `JwtAuthenticationOptions.RequireHttpsMetadata` | `bool` | `false` | 是否要求 HTTPS 元数据 |
| `JwtAuthenticationOptions.UsePolicy` | `bool` | `true` | 是否额外注册名为 `Bearer` 的授权策略（要求 `sub` 与 `name`） |
| `JwtAuthenticationOptions.ValidateIssuer` / `ValidateAudience` | `bool` | `true` | 是否校验颁发者 / 受众 |
| `JwtAuthenticationOptions.NameClaimType` / `RoleClaimType` | `string` | `name` / `role` | 名称与角色声明类型 |
| `JwtAuthenticationOptions.Scheme` | `string` | `null` | 保留属性；当前 JWT 注册固定使用 `JwtBearerDefaults.AuthenticationScheme` |
| `IdentityServerAuthenticationOptions.SupportedTokens` | `SupportedTokens` | `Both` | `Jwt`、`Reference` 或 `Both` |
| `IdentityServerAuthenticationOptions.ApiName` / `ApiSecret` | `string?` | `null` | 内省客户端凭据；设置 `ApiSecret` 时必须同时设置 `ApiName` |
| `IdentityServerAuthenticationOptions.CacheDuration` | `TimeSpan` | 10 分钟 | 内省缓存时长 |

## 注意事项 / 限制

- `HostUtility.Run<TStartup>` 通过 `UseStartup<TStartup>` 启动，`TStartup` 必须是可构造的类；`ConfigureHostBuilder` 在 `createHostBuilder` 返回后、`Build()` 之前执行。
- `AddJwtAuthentication(string sectionName)` 依赖调用时已注册的 `IConfiguration`；`IConfiguration` 本身缺失时会在 `GetConfiguration()!` 处抛 `NullReferenceException`，配置节不存在或未提供 `SigningKey` 时会在注册阶段因 `SigningKey` 为 `null` 抛出 `ArgumentNullException`。需要完全代码配置时请用 `Action<JwtAuthenticationOptions>` 重载。
- `AddJwtAuthentication` 会清空 `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap`，该设置是进程级的；依赖默认声明映射的代码需要显式配置映射。
- `HostingModule` 会自动插入 `RequestTraceMiddleware`（响应头 `x-request-trace-id`）和请求上下文访问器；`ExceptionHandlingMiddleware` 是内部类型，仅以 Transient 注册，不会自动加入管道，应用需自行提供异常映射中间件（示例应用使用 `ApiExceptionMiddleware`）。
- `UseTenancy()` 应位于 `UseAuthentication()` 与 `UseRouting()` 之后、`UseAuthorization()` 之前，否则 Header 解析拿不到已认证身份；`UseAuditing()` 应位于租户中间件之前，`UseEuoniaTelemetry()` 应位于租户中间件之后。对应 `*Options.Enabled` 为 `false` 或选项未注册时，这些扩展是空操作。
- `HostBuilderOptions.ApplicationName` 保存入口程序集名，默认构建路径未消费该值；实际应用名以宿主 `HostingEnvironment.ApplicationName` 为准，并写入 `SERVICE_NAME` 环境变量。

## 常见问题与故障排查

### 调用 `AddJwtAuthentication(string)` 时抛 `ArgumentNullException`
**原因**：该重载从已注册的 `IConfiguration` 读取调用方指定的配置节；节不存在或未提供 `SigningKey` 时，绑定结果缺少签名密钥（可能为空对象或 `null`），注册阶段会失败：前者在 `ArgumentNullException.ThrowIfNull`，后者在 `Encoding.UTF8.GetBytes`。若 `IConfiguration` 本身未注册，还会先在 `GetConfiguration()!` 处抛 `NullReferenceException`。
**解决**：确认模块执行 `ConfigureServices` 时配置已注册；优先用 `AddJwtAuthentication(options => options.SigningKey = ...)` 显式赋值，或先 `services.AddSingleton<IConfiguration>(configuration)` 再使用字符串重载。

### 租户请求头或子域名解析不到租户
**原因**：`UseTenancy()` 在 `MultiTenancyOptions` 未注册或 `Enabled = false` 时直接返回；中间件顺序错误（位于 `UseAuthentication` 之前）会让 Header 解析拿不到已认证身份；`RequireTenant = true` 时解析失败会由租户中间件写出错误。
**解决**：先 `AddTenancyAspNetCore(options => options.Enabled = true)`；按 `UseAuthentication` → `UseRouting` → `UseTenancy` → `UseAuthorization` 排序；匿名请求需要租户头时设置 `TrustHeaderForAnonymous = true`，并确认 `HeaderName` 与客户端一致。

### 模块初始化没有执行，或 `RequestTraceMiddleware` 不生效
**原因**：`AddModularityApplication<T>` 只注册模块与服务；不调用 `app.InitializeApplication()` 就不会执行 `OnApplicationInitialization`。模块未通过 `[DependsOn(typeof(HostingModule))]` 引入时，请求上下文与 `x-request-trace-id` 也不会安装。
**解决**：在 `Startup.Configure` 最前面调用 `app.InitializeApplication()`；检查模块依赖链包含 `HostingModule`，并用 `application.IsModuleLoaded<HostingModule>()` 或启动日志确认。

### 异常响应仍是 HTML 或没有统一 JSON
**原因**：`ExceptionHandlingMiddleware` 是内部类型，`HostingModule` 只把它注册为 Transient，**不会**自动插入请求管道；自动插入的只有 `RequestTraceMiddleware`。
**解决**：在应用侧实现异常中间件并放在管道最外层（示例应用使用 `ApiExceptionMiddleware`），按需把 `HttpStatusException`、校验异常、认证/授权异常与超时异常映射为 400/401/403/504 等状态码；不要依赖 `UseMiddleware<ExceptionHandlingMiddleware>()`，该类型无法从应用程序集引用。

## 最佳实践

- JWT 使用代码配置或宿主已有的密钥源，避免把 `SigningKey` 写进仓库；注册前先确保 `IConfiguration` 单例存在，再按需使用字符串重载。
- 固定管道顺序：`InitializeApplication` → `UseAuthentication` → `UseExecutionContext`/`UseAuditing` → `UseRouting` → `UseTenancy` → `UseEuoniaTelemetry` → `UseAuthorization`。
- 生产环境将 `HostBuilderOptions.CaptureStartupErrors` 设为 `false`，让启动配置错误直接失败，而不是带病运行。
- 用 `Microsoft.AspNetCore.TestHost` 或 `WebApplicationFactory` 写集成测试，断言认证、租户与追踪头在真实管道中的顺序。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：`Microsoft.AspNetCore.App` 共享框架、`Duende.IdentityModel` 8.1.0、`Duende.AspNetCore.Authentication.OAuth2Introspection` 7.0.2；JwtBearer/OpenIdConnect 随目标框架版本。
- 已知边界：`JwtAuthenticationOptions` 没有框架默认配置节；`ExceptionHandlingMiddleware` 为内部类型；`AddJwtAuthentication` 会清空进程级入站声明映射。
- 升级提示：升级 ASP.NET Core 或 Duende 包后，重新验证 `AddIdentityServerAuthentication` 的 JWT/内省子方案与 `TokenValidationParameters` 行为。

## 相关包

- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与应用生命周期
- [`Euonia.Security`](../Euonia.Security/README.md) —— 用户主体与权限
- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计记录与中间件选项
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 多租户解析与选项
- [`Euonia.Telemetry`](../Euonia.Telemetry/README.md) —— 遥测选项与 HTTP 标签
- [`Euonia.Validation`](../Euonia.Validation/README.md) —— 异常映射使用的校验异常

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
