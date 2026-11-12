# Euonia.Hosting 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Hosting` 负责 ASP.NET Core 宿主启动、模块初始化、请求上下文与 JWT/IdentityServer 认证，并提供租户、审计、遥测中间件的接入扩展。

- **包名**：`Euonia.Hosting`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Hosting`（扩展位于 `Microsoft.Extensions.DependencyInjection`、`Microsoft.AspNetCore.Builder` 等）
- **完整选项/边界**：[Source/Euonia.Hosting/README.md](../../../Source/Euonia.Hosting/README.md)

## 教程：启动一个带认证与请求追踪的 Web API

这个场景用 `HostUtility` 启动宿主，通过 `HostingModule` 安装请求上下文与追踪，再用 `AddJwtAuthentication` 接入 JWT，最后验证 401 与公开端点。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 一个可用的 JWT 颁发者；本地验证阶段只需能返回 401 即可，不要求真实令牌。

### 1. 创建项目并安装

```bash
dotnet new web -n OrdersApi
cd OrdersApi
dotnet add package Euonia.Hosting
```

### 2. 用 HostUtility 启动

`Program.cs`：

```csharp
using Nerosoft.Euonia.Hosting;

HostUtility.Run<Startup>(args, options =>
{
    options.EnableHttp2 = true;
    options.CaptureStartupErrors = true;
});
```

### 3. 接入模块与认证

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

`HostModule.cs`：

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
            options.Issuer = new[] { "https://identity.example.com" };
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

### 4. 添加受保护与公开端点

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet]
    [AuthorizeRoles("admin", "operator")]
    public IActionResult Get() => Ok(new[] { "order-1" });

    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() => Ok("ok");
}
```

### 5. 运行与验证

```bash
dotnet run
```

```bash
# 公开端点应返回 200
curl -i http://localhost:5000/api/orders/public

# 未带令牌的受保护端点应返回 401，并带 x-request-trace-id
curl -i http://localhost:5000/api/orders
```

如果启动端口不是 5000，以 `dotnet run` 输出或 `launchSettings.json` 为准。观察响应头 `x-request-trace-id`；它由 `HostingModule` 自动安装的 `RequestTraceMiddleware` 写入。若启动日志显示模块已加载，但请求上下文仍为空，优先检查 `app.InitializeApplication()` 是否在管道最前面。

### 常见失败点

- **所有受保护端点都 401**：`Startup.Configure` 漏了 `UseAuthentication()`，或 `AddJwtAuthentication` 没执行；先确认模块依赖链包含 `HostingModule`。
- **启动时 `ArgumentNullException`**：使用了 `AddJwtAuthentication(string)` 但配置节不存在或 `SigningKey` 为空；本教程用 `Action<JwtAuthenticationOptions>` 直接配置。
- **没有 `x-request-trace-id`**：模块没有通过 `[DependsOn(typeof(HostingModule))]` 加载，或 `InitializeApplication` 没有执行。
- **异常返回 HTML**：`ExceptionHandlingMiddleware` 是内部类型，不会自动入管道；应用需自行提供异常映射中间件（示例应用使用 `ApiExceptionMiddleware`）。

## 相关文档

- [API 参考](API.md)
- [Source README：完整安装、认证选项与中间件顺序](../../../Source/Euonia.Hosting/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/TUTORIAL.md) —— 模块加载与初始化
- [Euonia.Application 用户手册](../Euonia.Application/TUTORIAL.md) —— 应用服务与拦截器
- [Euonia.Validation 用户手册](../Euonia.Validation/TUTORIAL.md) —— 异常中的校验错误
- [仓库根 README](../../../README.md)
