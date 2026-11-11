# Euonia.Hosting

Euonia 的 ASP.NET Core 托管基础设施：以 `HostUtility` 构建并运行宿主，`HostingModule` 统一注册请求上下文、用户主体、异常处理与请求追踪中间件，并提供 JWT/引用令牌认证与基于角色、scope 的授权扩展。

ASP.NET Core hosting infrastructure for Euonia: `HostUtility` builds and runs the host, `HostingModule` wires request context, user principal, exception handling and request-trace middleware, plus JWT/introspection authentication and role/scope-based authorization extensions.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting`
- **NuGet 包**：`Euonia.Hosting`

## 安装

```bash
dotnet add package Euonia.Hosting
```

## 主要功能

- **宿主引导**：`HostUtility.Run<TStartup>` / `RunAsync<TStartup>`，支持自定义 `HostBuilderOptions`。
- **模块集成**：`[DependsOn(typeof(HostingModule))]` 注册请求上下文、用户主体与中间件。
- **认证**：`AddJwtAuthentication(...)` 支持 JWT 与 introspection 双模；`ScopeClaimsTransformer` 标准化 scope 声明。
- **授权**：`[AuthorizeRoles]`、`AddScopePolicy(...)`、`RequireScope(...)`。
- **中间件**：`ExceptionHandlingMiddleware`（异常 → HTTP 状态码 + JSON）、`RequestTraceMiddleware`（`x-request-trace-id`）。
- **应用初始化**：`app.InitializeApplication()` 将模块初始化挂到请求管道。

## 快速开始

`Program.cs`：

```csharp
using Nerosoft.Euonia.Hosting;

HostUtility.Run<Startup>(args, options =>
{
    options.EnableHttp2 = true;
    options.ConfigureHostBuilder = builder => builder.UseSerilog();
});
```

模块上下文：

```csharp
using Nerosoft.Euonia.Hosting;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(HostingModule))]
public sealed class HostModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddControllers();
        context.Services.AddJwtAuthentication("JwtAuthenticationOptions");
        context.Services.AddUserPrincipal();

        context.Services.AddAuthorization(options =>
        {
            options.AddScopePolicy("orders.read", "orders:read");
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseSerilogRequestLogging();
    }
}
```

`Startup.cs`：

```csharp
public sealed class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddModularityApplication<HostModule>(configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.InitializeApplication();

        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

控制器授权：

```csharp
[AuthorizeRoles("admin", "operator")]
[Authorize(Policy = "orders.read")]
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `HostUtility` | `Run` / `RunAsync<TStartup>` 构建并运行宿主 |
| `HostBuilderOptions` | `EnableHttp2`、`ApplicationName`、`ConfigureWebHostBuilder`、`ConfigureHostBuilder` 等 |
| `HostingModule` | 注册请求上下文访问器、用户主体、中间件 |
| `AddJwtAuthentication(...)` | 注册 JWT 或引用令牌认证 |
| `JwtAuthenticationOptions` | Issuer、SigningKey、Audience、NameClaimType、RoleClaimType |
| `AddUserPrincipal(...)` | 注册当前请求的 `UserPrincipal` |
| `AddScopePolicy(...)` / `RequireScope(...)` | 基于 scope 的授权策略 |
| `AuthorizeRolesAttribute` | 基于角色的授权特性 |
| `ExceptionHandlingMiddleware` / `RequestTraceMiddleware` | 异常映射与请求追踪 |

## 配置项

```json
{
  "JwtAuthenticationOptions": {
    "Issuer": "https://identity.example.com",
    "Audience": "orders-api",
    "SigningKey": "your-signing-key"
  }
}
```

`AddJwtAuthentication("JwtAuthenticationOptions")` 会从该配置节绑定选项。

## 相关包

- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与应用生命周期
- [`Euonia.Security`](../Euonia.Security/README.md) —— 用户主体与权限
- [`Euonia.Osba`](../Euonia.Osba/README.md) —— 业务对象与权限接线
- [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— `MapGrpcServices()` 等集成

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
