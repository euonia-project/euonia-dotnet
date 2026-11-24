# Euonia.Hosting.Webapi 教程

## 1. 安装

```bash
dotnet add package Euonia.Hosting.Webapi
```

## 2. 最小 Web API

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaWebApi(options =>
{
    options.HealthCheckPath = "/healthz";
    options.AllowedOrigins = ["https://app.example.com"];
});

var app = builder.Build();

app.UseEuoniaWebApi();
app.Run();
```

`AddEuoniaWebApi` 注册控制器、全局异常处理、OpenAPI、健康检查与默认 CORS；`UseEuoniaWebApi` 按相反顺序装配中间件并映射端点。

## 3. 使用 Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddEuoniaWebApi();
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEuoniaWebApi();
}
```

`IApplicationBuilder` 重载通过 `UseEndpoints` 映射控制器、OpenAPI 与健康检查；调用方需要先启用路由。

## 4. 权限与流控

权限授权按需启用：

```csharp
builder.Services.AddPermissionGuard(options => options.NoModels());
```

控制器用 `[RequiresPermission("orders.read")]` 声明权限码；权限数据由 `IScopeSubjectResolver` 经 `IScopeGuard` 提供。

流控默认按客户端 IP 分区：

```csharp
using Nerosoft.Euonia.Hosting;

builder.Services.AddEuoniaRateLimiting(options =>
{
    options.DefaultLimiter.MinutePermitLimit = 5;
    options.DefaultLimiter.HourPermitLimit = 10;
});

app.UseEuoniaRateLimiting();
```

端点通过 `[EnableRateLimiting("euonia")]` 引用策略；超限返回 429 与 `Retry-After`。

## 5. 验证

```bash
dotnet run
curl http://localhost:5000/openapi/v1.json
curl http://localhost:5000/health
```

## 6. 常见失败点

- **401/403 不生效**：本包不注册认证，也不会自动启用权限授权；需调用 `Euonia.Hosting` 的认证扩展、加入 `UseAuthentication` / `UseAuthorization`，并按需调用 `AddPermissionGuard`。
- **CORS 预检失败**：`AllowedOrigins` 必须包含完整来源（含协议和端口），不能写通配符 `*`。
- **健康检查 404**：确认 `EnableHealthChecks` 与 `HealthCheckPath`，并检查端点映射是否被其他中间件短路。
