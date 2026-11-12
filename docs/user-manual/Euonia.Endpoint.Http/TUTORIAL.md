# Euonia.Endpoint.Http 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

把 `Euonia.Endpoint` 标记的接口方法映射为 ASP.NET Core Minimal API 端点：生成优先 + 运行时兜底、参数绑定、认证元数据与扫描范围控制。本页是端到端教程；完整规则见 [项目 README](../../../Source/Euonia.Endpoint.Http/README.md)。

- **NuGet 包**：`Euonia.Endpoint.Http`
- **目标框架**：`net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Http`（`AddDynamicHttpEndpoint` 在 `Microsoft.Extensions.DependencyInjection`，`MapDynamicHttpEndpoint` 在 `Microsoft.AspNetCore.Builder`）
- **依赖**：`Euonia.Endpoint`、`Microsoft.Extensions.Logging.Abstractions`、`Microsoft.Extensions.Options.ConfigurationExtensions`
- **完整参考**：[Source/Euonia.Endpoint.Http/README.md](../../../Source/Euonia.Endpoint.Http/README.md)

## 教程

### 场景：把订单应用服务暴露成 REST 端点

订单服务已有接口 `IOrderService`，现在要暴露 `GET api/orders/{id}` 与 `POST api/orders/create`，并让插件程序集里的服务也被扫描到。

### 前置条件

.NET 10 SDK；ASP.NET Core 项目；可选：引用 SourceGenerator 以走生成路径（不引用则走运行时兜底）。

### 1. 安装

```bash
dotnet new web -n OrderRest && cd OrderRest
dotnet add package Euonia.Endpoint.Http
dotnet add package Euonia.Endpoint.SourceGenerator
```

### 2. 声明接口与实现

```csharp
using Nerosoft.Euonia.Endpoint;

public interface IOrderService
{
    [ExposeWebApi(Prefix = "api/orders", Route = "{id}", Method = new[] { "GET" })]
    Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default);

    [ExposeWebApi(Prefix = "api/orders", Route = "create", Method = new[] { "POST" })]
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
}

public sealed class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
}

public sealed class OrderDto
{
    public string Id { get; set; } = string.Empty;
}

public sealed class OrderService : IOrderService
{
    public Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = id });

    public Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = Guid.NewGuid().ToString("N") });
}
```

### 3. 宿主接线（含扫描范围）

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddDynamicHttpEndpoint(options =>
{
    options.EnableExposeApplicationServiceAsWebApi = true;
    options.DefaultRoutePrefix = "api";
    options.ScanAssemblies = new[] { "OrderRest", "OrderRest.Plugins" };   // 插件程序集也要扫描
    options.ScanEntryAssembly = false;
});

var app = builder.Build();
app.MapDynamicHttpEndpoint();
app.Run();
```

等价的配置方式（程序集为空时按配置扫描）：

```json
{
  "Euonia": {
    "Endpoint": {
      "Http": {
        "EnableExposeApplicationServiceAsWebApi": true,
        "DefaultRoutePrefix": "api",
        "ScanAssemblies": [ "OrderRest", "OrderRest.Plugins" ],
        "ScanEntryAssembly": false
      }
    }
  }
}
```

```bash
Euonia__Endpoint__Http__EnableExposeApplicationServiceAsWebApi=true   # .NET 原生
EUONIA_ENDPOINT_HTTP_ENABLEEXPOSEAPPLICATIONSERVICEASWEBAPI=true      # 仓库 alias
```

### 4. 运行与验证

```bash
dotnet run
curl -s http://localhost:5000/api/orders/A-1
# {"id":"A-1"}

curl -s -X POST http://localhost:5000/api/orders/create \
     -H 'Content-Type: application/json' \
     -d '{"customerId":"C-1"}'
# {"id":"..."}
```

再验证错误语义：不带 `Content-Type` 发送 body 会返回 `400`；缺少必填 body 也是 `400`（绑定内部异常才是 `500`）。

### 常见失败点

- `HttpRegistry is not registered. Call AddDynamicHttpEndpoint first.`：手工先跑了生成映射；只调用 `MapDynamicHttpEndpoint()`，且注册要在 `Build()` 之前。
- `Service 'X' is marked with ExposeWebApiAttribute but is not registered.`：忘 `AddScoped`；或临时设 `ThrowOnUnregisteredService = false` 降级为 Warning。
- `Duplicate HTTP endpoint 'route' for ...`：两个方法映射到同一「路由 + 动词」；显式给 `Prefix` / `Route`。
- 插件端点没出现：默认只扫入口程序集，需配置 `ScanAssemblies`（或给 `AddDynamicHttpEndpoint(options, assemblies)` 传程序集）。
- 请求 401：`RequireAuthorization` 默认 `true`，匿名端点显式 `[AllowAnonymous]`。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Endpoint.Http/README.md) —— 参数绑定、路由推导、认证元数据与限制
- [Euonia.Endpoint 用户手册](../Euonia.Endpoint/TUTORIAL.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.SourceGenerator 用户手册](../Euonia.Endpoint.SourceGenerator/TUTORIAL.md) —— 生成强类型映射
- [Euonia.Endpoint.Grpc 用户手册](../Euonia.Endpoint.Grpc/TUTORIAL.md) —— gRPC 侧运行时
- [仓库根 README](../../../README.md)
