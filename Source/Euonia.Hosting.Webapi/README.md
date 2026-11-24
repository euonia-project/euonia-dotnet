# Euonia.Hosting.Webapi

ASP.NET Core Web API 宿主组合包，在 `Euonia.Hosting` 之上提供控制器、统一异常响应、OpenAPI、健康检查、CORS、权限授权与双窗口流控。

## 安装

```bash
dotnet add package Euonia.Hosting.Webapi
```

## 快速开始

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

## 选项

| 属性 | 默认值 | 说明 |
|---|---|---|
| `EnableExceptionHandling` | `true` | 注册统一异常中间件并写入 JSON 错误响应 |
| `EnableOpenApi` | `true` | 注册并映射 OpenAPI 文档（`/openapi/{documentName}.json`） |
| `EnableHealthChecks` | `true` | 注册并映射健康检查端点 |
| `HealthCheckPath` | `/health` | 健康检查端点路径 |
| `EnableHttpsRedirection` | `true` | 启用 `UseHttpsRedirection` |
| `AllowedOrigins` | 空数组 | 非空时注册并启用默认 CORS 策略 |

## 权限控制

`AddEuoniaWebApi` 不自动启用权限授权；需要时显式注册：

```csharp
builder.Services.AddPermissionGuard(options => options.NoModels());
```

控制器使用 `[RequiresPermission("orders.read")]` 声明权限码；多个权限码为“任一满足”，权限数据由宿主注册的 `IScopeSubjectResolver` 经 `IScopeGuard` 提供。

## 流控

```csharp
using Nerosoft.Euonia.Hosting;

builder.Services.AddEuoniaRateLimiting(options =>
{
    options.PolicyName = "euonia";
    options.DefaultLimiter.MinutePermitLimit = 5;
    options.DefaultLimiter.HourPermitLimit = 10;
});

app.UseEuoniaRateLimiting();
```

端点通过 `[EnableRateLimiting("euonia")]` 引用默认策略；超限返回 429、`Retry-After` 与 JSON 响应体。

## 边界

- 不自动注册认证、权限授权或流控；认证由 `Euonia.Hosting` 配置，权限与流控需显式调用 `AddPermissionGuard` / `AddEuoniaRateLimiting`，租户与工作单元由 `Euonia.Tenancy` 等包按需配置。
- 不引入 Swagger UI、Serilog 或 Swashbuckle；OpenAPI 使用 ASP.NET Core 内置的 `AddOpenApi` / `MapOpenApi`。
- 异常响应使用 ASP.NET Core 内置 `UseExceptionHandler()` + `EuoniaExceptionHandler`，并复用 `Euonia.Hosting` 的异常映射（含验证异常与聚合异常解包）。

## 相关文档

- [教程](TUTORIAL.md)
- [API](API.md)
- [完整 API](API-FULL.md)
