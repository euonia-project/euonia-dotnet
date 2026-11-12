# Euonia.Endpoint 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

把应用服务接口方法同时暴露为 HTTP Minimal API 与 gRPC 服务的**契约包**：暴露特性、共享 `DataContract` 类型与方法签名约定。本页是端到端教程；运行时细节见 [项目 README](../../../Source/Euonia.Endpoint/README.md)。

- **NuGet 包**：`Euonia.Endpoint`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Endpoint`
- **依赖**：无（不引用 ASP.NET Core / protobuf-net；运行时由 `Euonia.Endpoint.Http` / `Euonia.Endpoint.Grpc` 提供）
- **完整参考**：[Source/Euonia.Endpoint/README.md](../../../Source/Euonia.Endpoint/README.md)

## 教程

### 场景：同一个订单接口同时提供 HTTP 与 gRPC

一个订单查询方法既要给浏览器/前端用 REST，又要给内部服务用 gRPC。用一份接口 + 两个特性声明两种暴露方式，宿主分别注册两套运行时。

### 前置条件

- .NET 10 SDK；ASP.NET Core 项目；本教程用 HTTP/1.1 + HTTP/2 同端口（Kestrel 默认支持 h2c 需显式配置，见步骤 5 的 h2c 配置）。

### 1. 安装

```bash
dotnet new web -n OrderApi && cd OrderApi
dotnet add package Euonia.Endpoint
dotnet add package Euonia.Endpoint.Http
dotnet add package Euonia.Endpoint.Grpc
dotnet add package Euonia.Endpoint.SourceGenerator
```

### 2. 声明接口与 DTO

```csharp
using System.Runtime.Serialization;
using Nerosoft.Euonia.Endpoint;

public interface IOrderService
{
    [ExposeWebApi(Prefix = "api/orders", Route = "{id}", Method = new[] { "GET" })]
    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Get")]
    Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default);
}

[DataContract]
public sealed class OrderDto
{
    [DataMember(Order = 1)]
    public string Id { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public decimal Amount { get; set; }
}

public sealed class OrderService : IOrderService
{
    public Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = id, Amount = 99.5m });
}
```

### 3. 宿主接线

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDynamicHttpEndpoint(builder.Configuration);
builder.Services.AddDynamicGrpcEndpoint(builder.Configuration);

var app = builder.Build();

app.MapDynamicHttpEndpoint();
app.MapDynamicGrpcEndpoint();

app.Run();
```

### 4. 配置开关

`appsettings.json`：

```json
{
  "Euonia": {
    "Endpoint": {
      "Http": { "EnableExposeApplicationServiceAsWebApi": true, "DefaultRoutePrefix": "api" },
      "Grpc": { "EnableExposeApplicationServiceAsGrpc": true, "EnableReflection": true }
    }
  }
}
```

```bash
# .NET 原生：双下划线表示层级
Euonia__Endpoint__Http__EnableExposeApplicationServiceAsWebApi=true
Euonia__Endpoint__Grpc__EnableExposeApplicationServiceAsGrpc=true
# 仓库 alias：大写单下划线（Euonia.Modularity 配置构建）
EUONIA_ENDPOINT_HTTP_ENABLEEXPOSEAPPLICATIONSERVICEASWEBAPI=true
EUONIA_ENDPOINT_GRPC_ENABLEEXPOSEAPPLICATIONSERVICEASGRPC=true
```

### 5. 运行与验证

```bash
dotnet run
# HTTP：路由来自 Prefix + Route
curl -s http://localhost:5000/api/orders/A-1
# {"id":"A-1","amount":99.5}

# gRPC：需要 HTTP/2；开发环境可用 h2c
grpcurl -plaintext -d '{"id":"A-1"}' localhost:5000 sample.OrderService/Get
```

若 gRPC 调用报协议错误，给 Kestrel 显式开启 h2c：

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000, listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
});
```

### 常见失败点

- **没有任何端点**：只装了契约包；必须加 `Euonia.Endpoint.Http` / `Euonia.Endpoint.Grpc` 并调用 `Map*`。
- **特性不生效**：`[ExposeWebApi]` / `[ExposeGrpc]` 只能标在**接口方法**上，实现类方法不会被扫描。
- **gRPC 方法缺失且日志只有 Warning**：该程序集没有生成映射，需在声明接口的项目引用 `Euonia.Endpoint.SourceGenerator`。
- **401 / `PermissionDenied`**：两个运行时的 `RequireAuthorization` 默认都是 `true`，匿名端点要显式 `[AllowAnonymous]`。
- **旧客户端解析失败**：`DataMember(Order = n)` 是 protobuf 字段号，发布后不能修改、复用或重排。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Endpoint/README.md) —— 签名约定、DTO 规则、行为说明
- [Euonia.Endpoint.Http 用户手册](../Euonia.Endpoint.Http/TUTORIAL.md) —— HTTP 运行时与 9 个选项
- [Euonia.Endpoint.Grpc 用户手册](../Euonia.Endpoint.Grpc/TUTORIAL.md) —— gRPC 运行时、流式与 proto 导出
- [Euonia.Endpoint.SourceGenerator 用户手册](../Euonia.Endpoint.SourceGenerator/TUTORIAL.md) —— 构建期生成器
- [仓库根 README](../../../README.md)
