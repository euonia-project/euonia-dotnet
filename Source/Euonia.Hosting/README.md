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

## 相关包

- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与应用生命周期
- [`Euonia.Security`](../Euonia.Security/README.md) —— 用户主体与权限
- [`Euonia.Auditing`](../Euonia.Auditing/README.md) —— 审计记录与中间件选项
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 多租户解析与选项
- [`Euonia.Telemetry`](../Euonia.Telemetry/README.md) —— 遥测选项与 HTTP 标签
- [`Euonia.Validation`](../Euonia.Validation/README.md) —— 异常映射使用的校验异常

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
