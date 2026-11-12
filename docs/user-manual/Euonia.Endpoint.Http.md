# Euonia.Endpoint.Http 用户手册

把 `Euonia.Endpoint` 标记的接口方法映射为 ASP.NET Core Minimal API 端点：生成优先 + 运行时兜底、参数绑定、认证元数据与扫描范围控制。本页是端到端教程与 API 速查；完整规则见 [项目 README](../../Source/Euonia.Endpoint.Http/README.md)。

- **NuGet 包**：`Euonia.Endpoint.Http`
- **目标框架**：`net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Http`（`AddDynamicHttpEndpoint` 在 `Microsoft.Extensions.DependencyInjection`，`MapDynamicHttpEndpoint` 在 `Microsoft.AspNetCore.Builder`）
- **依赖**：`Euonia.Endpoint`、`Microsoft.Extensions.Logging.Abstractions`、`Microsoft.Extensions.Options.ConfigurationExtensions`
- **完整参考**：[Source/Euonia.Endpoint.Http/README.md](../../Source/Euonia.Endpoint.Http/README.md)

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

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddDynamicHttpEndpoint(IConfiguration, params Assembly[])` | 绑定 `Euonia:Endpoint:Http` 并注册运行时 | 程序集为空时用配置扫描范围 |
| `AddDynamicHttpEndpoint(Action<HttpExposureOptions>, params Assembly[])` | 回调注册 | 显式程序集后不再回退配置 |
| `MapDynamicHttpEndpoint()` | 先跑生成映射，再跑运行时兜底 | 开关关闭时不映射 |
| `HttpExposureOptions.EnableExposeApplicationServiceAsWebApi` | 总开关 | `false` |
| `HttpExposureOptions.DefaultRoutePrefix` | `Prefix` 为空时的默认前缀 | `"api"` |
| `HttpExposureOptions.RequireAuthorization` | 每个端点追加默认授权要求 | `true` |
| `HttpExposureOptions.ThrowOnUnregisteredService` | 服务未注册时抛异常 | `true` |
| `HttpExposureOptions.ThrowOnRouteConflict` | 路由冲突时抛异常 | `true` |
| `HttpExposureOptions.ScanAssemblies` / `ScanNamespaces` / `ScanTypes` | 扫描范围 | 均为空数组；命名空间按 `.` 段边界匹配 |
| `HttpExposureOptions.ScanEntryAssembly` | 未配置程序集时是否扫入口程序集 | `true` |
| `HttpRegistry.TryReserve(routePattern, methods, generated)` | 「路由 + 动词」占用与冲突检测 | 返回 `HttpReservation` |
| `HttpReservation` | 占用结果 | `Reserved` / `AlreadyGenerated` / `Conflict` |
| `HttpGeneratedMapper.Map(...)` / `ReadBodyAsync<T>(...)` | 生成代码调用的映射入口 | 由 SourceGenerator 生成 |
| `HttpGeneratedMappingAttribute` | 程序集级标记，指向 `HttpMappings` | 生成器写入 |

## 相关文档

- [项目 README](../../Source/Euonia.Endpoint.Http/README.md) —— 参数绑定、路由推导、认证元数据与限制
- [Euonia.Endpoint 用户手册](Euonia.Endpoint.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.SourceGenerator 用户手册](Euonia.Endpoint.SourceGenerator.md) —— 生成强类型映射
- [Euonia.Endpoint.Grpc 用户手册](Euonia.Endpoint.Grpc.md) —— gRPC 侧运行时
- [仓库根 README](../../README.md)
