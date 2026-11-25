# Euonia.Hosting.Azfunc 教程

> 仅目标 `net10.0`；Azure Functions Worker 当前未发布 net11 目标。

## 1. 安装

```bash
dotnet add package Euonia.Hosting.Azfunc
```

## 2. 配置函数宿主

```csharp
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddEuoniaFunctions();
builder.UseEuoniaFunctions();

await builder.Build().RunAsync();
```

`AddEuoniaFunctions` 注册选项与 `IHttpContextAccessor`；`UseEuoniaFunctions` 依次加入请求上下文中转与异常处理中间件。

## 3. 接入 Security 体系

`AddEuoniaFunctions` 已从 `HttpContext.User` 注册 `UserPrincipal`；需要权限判定时追加：

```csharp
builder.Services.AddEuoniaFunctionsSecurity(options => options.NoModels());
```

然后注入 `IPermissionChecker` 做显式判定：

```csharp
public sealed class OrdersFunction(IPermissionChecker permissions)
{
    public async Task<bool> CanReadAsync()
    {
        return await permissions.IsGrantedAsync("orders.read");
    }
}
```

Azure Functions Worker 不会自动执行 ASP.NET Core 的 `[RequiresPermission]` / `[Authorize]`；认证由宿主或网关完成，函数内使用 `IPermissionChecker` 或读取 `FunctionContext.Items["User"]`。

## 4. JWT 认证

`UseEuoniaFunctions` 可在函数管道中校验 Bearer JWT，并按函数上的授权特性拦截请求：

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
public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request)
{
    return ExecuteAsync(() => Task.FromResult(new[] { "order-1" }));
}
```

未认证返回 401，角色不匹配返回 403；`[AllowAnonymous]` 跳过认证。基于策略的 `[Authorize(Policy = ...)]` 不会执行，需要权限判定时使用 `IPermissionChecker`。

## 5. 请求追踪与流控

`UseEuoniaFunctions` 默认写入 `x-request-trace-id`。函数级限流默认关闭，需要时启用：

```csharp
builder.Services.AddEuoniaFunctions(options =>
{
    options.EnableRateLimiting = true;
    options.RateLimiting.MinutePermitLimit = 5;
    options.RateLimiting.HourPermitLimit = 10;
});
```

限流按“函数名 + 客户端 IP”分区，超限返回 429、`Retry-After` 与 JSON 响应体；可用 `RateLimitPartitionKeySelector` 自定义分区。

异常响应与 Web API 共用状态码映射：`ArgumentException` 400、认证失败 401、禁止访问 403、未实现 501、超时 504，其余 500；验证异常与聚合异常会解包并附带 `details`。

## 6. 函数基类

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

## 7. 验证

部署或本地运行后调用 HTTP 触发器；抛出 `ArgumentException` 时应返回 400，其余未处理异常返回 500 JSON。

## 8. 常见失败点

- **中间件顺序错误**：`AddEuoniaFunctions` 必须在 `UseEuoniaFunctions` 之前调用，否则解析不到选项。
- **非 HTTP 触发器异常**：没有 HTTP 上下文时异常中间件会重新抛出，由 Functions 运行时记录。
- **请求上下文为空**：未注册 `DefaultRequestContextAccessor` 时只注入 `IHttpContextAccessor`，不会创建 `RequestContext`。
