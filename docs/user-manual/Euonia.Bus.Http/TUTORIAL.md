# Euonia.Bus.Http 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

Euonia 总线的 HTTP 远程传输：客户端用 HTTP POST 执行 `CallAsync`，服务端用 `MapBusEndpoint()` 接收消息、执行处理器并回传结果。本页给出两个 ASP.NET Core 服务的端到端接线；完整选项见 [项目 README](../../../Source/Euonia.Bus.Http/README.md)。

- **NuGet 包**：`Euonia.Bus.Http`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Http`
- **完整选项**：[Source/Euonia.Bus.Http/README.md](../../../Source/Euonia.Bus.Http/README.md)

## 教程

### 场景：网关通过 HTTP 调用订单服务的请求-响应

订单服务暴露总线端点并注册 `GetOrderStatus` 处理器；网关只依赖同一份消息契约，通过 `Euonia:Bus:Http` 的端点表把 `orders` 通道指向订单服务，然后调用并返回结果。

### 前置条件

- .NET SDK 可构建 `net10.0`/`net11.0`；
- 两个 ASP.NET Core 项目（Orders、Gateway），共享消息契约项目；
- 本地演示关闭授权；生产必须开启。

### 1. 安装

```bash
dotnet add package Euonia.Bus
dotnet add package Euonia.Bus.Http
```

### 2. 订单服务：映射总线端点

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddHttpBus();   // 绑定 Euonia:Bus:Http 并注册 HttpBusOptions
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention =>
    {
        convention.Add<DefaultMessageConvention>();
        convention.Add<AnnotationMessageConvention>();
    })
    .RegisterChannel(typeof(Program).Assembly));

var app = builder.Build();
app.MapBusEndpoint();            // POST /bus/call
app.Run();
```

本地演示还需在订单服务的 `Euonia:Bus:Http` 中设置 `RequireAuthorization=false`；生产保持默认 `true` 并配置认证。

```csharp
using Nerosoft.Euonia.Bus;

[Channel("orders")]
public sealed class GetOrderStatus : IRequest<OrderStatus>
{
    public long OrderId { get; init; }
}

public sealed class OrderStatus
{
    public long OrderId { get; init; }
    public string State { get; init; } = null!;
}

public sealed class GetOrderStatusHandler : IHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> HandleAsync(GetOrderStatus message, IMessageContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderStatus { OrderId = message.OrderId, State = "Shipped" });
}
```

### 3. 网关：按通道路由并调用

```json
{
  "Euonia": {
    "Bus": {
      "DefaultTransporter": "http",
      "Http": {
        "Endpoint": "https://orders.local",
        "Route": "/bus/call",
        "RequireAuthorization": false,
        "Endpoints": { "orders": "http://localhost:5100" }
      }
    }
  }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEuoniaBus();
builder.Services.AddOptions<MessageBusOptions>().BindConfiguration("Euonia:Bus");
builder.Services.AddHttpBus("http");

var app = builder.Build();

app.MapGet("/orders/{id:long}/status", async (long id, IBus bus, CancellationToken cancellationToken) =>
{
    var status = await bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = id })
                          .WithTimeout(TimeSpan.FromSeconds(5))
                          .ExecuteAsync(cancellationToken);

    return Results.Ok(status);
});

app.Run();
```

### 4. 运行与验证

```bash
# 终端 1：订单服务（默认 http://localhost:5100）
dotnet run --project Orders

# 终端 2：网关
dotnet run --project Gateway

# 终端 3：调用网关
curl http://localhost:5200/orders/42/status
# 期望：{"orderId":42,"state":"Shipped"}
```

### 常见失败点

- **404**：订单服务未 `MapBusEndpoint()`，或两端 `Route` 不一致。
- **401/403**：`RequireAuthorization` 默认 `true`；本地演示在两端都设为 `false`，生产不要关闭。
- **配置不生效**：服务端只调用了 `AddEuoniaBus()`，没有注册 `HttpBusOptions`；需调用 `AddHttpBus()`。
- **远端异常变成 `MessageDeliverException`**：自定义异常不会被还原，只有系统异常或 `MessageBusException` 子类可还原。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Bus.Http/README.md)
- [Euonia.Bus 用户手册](../Euonia.Bus/TUTORIAL.md)
- [Euonia.Bus.Grpc 用户手册](../Euonia.Bus.Grpc/TUTORIAL.md)
- [Euonia.Bus.HealthChecks 用户手册](../Euonia.Bus.HealthChecks/TUTORIAL.md)
- [仓库根 README](../../../README.md)
