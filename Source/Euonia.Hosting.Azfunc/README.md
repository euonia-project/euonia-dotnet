# Euonia.Hosting.Azfunc

Azure Functions 隔离工作进程宿主包，依赖 `Euonia.Hosting`，提供函数基类、统一异常响应、JWT 认证、请求追踪、HTTP 请求上下文中转、Security 体系与双窗口流控。

> 目标框架为 `net10.0`；Azure Functions Worker 当前不作为 net11 目标发布。

## 安装

```bash
dotnet add package Euonia.Hosting.Azfunc
```

## 快速开始

```csharp
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddEuoniaFunctions();
builder.Services.AddEuoniaFunctionsSecurity(options => options.NoModels());
builder.UseEuoniaFunctions();

await builder.Build().RunAsync();
```

## 选项

| 属性 | 默认值 | 说明 |
|---|---|---|
| `EnableExceptionHandling` | `true` | 捕获函数异常并写出与 Web API 一致的状态码/JSON 错误响应 |
| `EnableHttpContext` | `true` | 将函数 HTTP 上下文注入 `IHttpContextAccessor` 与请求上下文访问器 |
| `EnableRequestTrace` | `true` | 写入 `x-request-trace-id` 响应头 |
| `EnableJwtAuthentication` | `false` | 校验 Bearer JWT 并按 `[Authorize]` / `[AllowAnonymous]` 执行认证 |
| `JwtAuthentication` | - | `JwtAuthenticationOptions`：签名密钥、Issuer、Audience 等 |
| `EnableRateLimiting` | `false` | 按分区启用函数级双窗口限流 |
| `RateLimiting` | 5 次/分钟、10 次/小时 | 双窗口限流参数 |
| `RateLimitPartitionKeySelector` | 客户端 IP | 自定义限流分区键 |

## JWT 认证

启用后，函数请求会先校验 `Authorization: Bearer <token>`，再按函数或类型上的 `[Authorize]` / `[AllowAnonymous]` 执行认证与角色检查：

```csharp
builder.Services.AddEuoniaFunctions(options =>
{
    options.EnableJwtAuthentication = true;
    options.JwtAuthentication.SigningKey = "replace-with-a-strong-secret";
    options.JwtAuthentication.Issuer = ["https://identity.example.com"];
    options.JwtAuthentication.Audience = "orders-api";
});
```

```csharp
[Function("GetOrders")]
[Authorize(Roles = "admin,operator")]
public Task<IActionResult> Run(...)
```

未认证返回 401，角色不匹配返回 403；`[AllowAnonymous]` 跳过认证。Azure Functions 不执行基于策略的 `[Authorize(Policy = ...)]`，需要权限判定时注入 `IPermissionChecker`。

## Security 体系

`AddEuoniaFunctions` 默认从 `HttpContext.User` 注册 `ClaimsPrincipal` 与 `UserPrincipal`；需要权限判定时再调用 `AddEuoniaFunctionsSecurity`：

```csharp
builder.Services.AddEuoniaFunctionsSecurity(options =>
{
    // 无权限模型/声明时：
    options.NoModels();
    // 有权限模型时改用 options.Scan(typeof(Program).Assembly);
});
```

函数可注入 `IPermissionChecker` 显式判定；权限数据仍由宿主注册的 `IScopeSubjectResolver` 提供：

```csharp
public sealed class OrdersFunction(IPermissionChecker permissions, ILoggerFactory loggerFactory)
    : FunctionBase<OrdersFunction>(loggerFactory)
{
    public async Task<IActionResult> Run(...)
    {
        if (!await permissions.IsGrantedAsync("orders.read"))
        {
            return new UnauthorizedResult();
        }

        return await ExecuteAsync(() => Task.FromResult(new[] { "order-1" }));
    }
}
```

中间件还会把 `UserPrincipal` 写入 `FunctionContext.Items["User"]`。

## 请求追踪与流控

`UseEuoniaFunctions` 默认写入请求追踪头；需要函数级限流时启用：

```csharp
builder.Services.AddEuoniaFunctions(options =>
{
    options.EnableRateLimiting = true;
    options.RateLimiting.MinutePermitLimit = 5;
    options.RateLimiting.HourPermitLimit = 10;
});
```

限流按“函数名 + 客户端 IP”分区，超限返回 429、`Retry-After` 与 JSON 响应体；可用 `RateLimitPartitionKeySelector` 改为按用户、租户或自定义头分区。

异常响应复用 `Euonia.Hosting` 的异常映射：`ArgumentException` 400、认证失败 401、禁止访问 403、未实现 501、超时 504，其余 500；验证异常与聚合异常会解包并附带 `details`。

## 函数基类

```csharp
public sealed class OrdersFunction(ILoggerFactory loggerFactory) : FunctionBase<OrdersFunction>(loggerFactory)
{
    [Function("GetOrders")]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request)
    {
        return ExecuteAsync(() => Task.FromResult(new[] { "order-1" }));
    }
}
```

## 边界

- 依赖 `Euonia.Hosting`，复用其 `AddUserPrincipal`、权限与流控基础设施；不直接依赖 `Euonia.Security`。
- 启用 JWT 后自行校验令牌并执行 `[Authorize]` 角色规则；不执行 `[Authorize(Policy = ...)]` 或 `[RequiresPermission]`，权限判定使用 `IPermissionChecker`。
- 不注册功能管理或 Application Insights。
- 异常中间件仅在存在 HTTP 上下文时写响应；非 HTTP 触发器会重新抛出异常。
- 不包含函数扫描容器或模块化宿主启动；这些属于应用层。

## 相关文档

- [教程](TUTORIAL.md)
- [API](API.md)
- [完整 API](API-FULL.md)
