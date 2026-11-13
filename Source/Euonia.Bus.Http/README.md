# Euonia.Bus.Http

Euonia 总线的 HTTP 远程传输：客户端通过 HTTP POST 执行请求-响应调用（`CallAsync`），服务端端点接收消息、执行处理器并回传结果。与 gRPC 传输共用 `RemoteReply<TResult>` 线路协议。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Bus.Http`
- **NuGet 包**：`Euonia.Bus.Http`

## 安装

```bash
dotnet add package Euonia.Bus.Http
```

## 主要功能

- **请求-响应**：客户端 `CallAsync` → HTTP POST，服务端 `MapBusEndpoint()` 处理并回传。
- **仅支持远程调用**：`Send` / `Publish` 会抛出 `NotSupportedException`，避免误用语义。
- **错误还原**：`RemoteReply<T>` 与 `RemoteError` 在客户端还原原始异常类型，失败时回退为 `MessageDeliverException`。
- **请求头透传**：`x-*` 消息头随请求传递。
- **授权与超时**：`RequireAuthorization`、`RequestTimeout` 与自定义 `MessageHandlerFactory`。

## 快速开始

客户端：

```csharp
using Nerosoft.Euonia.Bus;

services.AddHttpBus("http", options =>
{
    options.Endpoint = "https://service.example.com";
    options.RequestTimeout = TimeSpan.FromSeconds(30);
});

services.Configure<MessageBusOptions>(options =>
{
    options.DefaultTransporter = "http";
});
```

服务端（ASP.NET Core）：

```csharp
services.AddEuoniaBus();
services.AddHttpBus();   // 绑定 Euonia:Bus:Http，并注册 HttpBusOptions

var app = builder.Build();
app.MapBusEndpoint();    // POST /bus/call
```

调用远程处理器：

```csharp
var status = await bus.CallAsync<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = 42 });
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `Enabled` | `true` | 预留开关；`AddHttpBus` 不读取该值，注册与否取决于是否调用该方法 |
| `Name` | `http` | 传输器名称 |
| `Endpoint` | `null` | 服务端基础地址 |
| `Route` | `/bus/call` | 服务端端点路由 |
| `SerializerProvider` | `SystemTestJson` | 序列化器名称 |
| `RequestTimeout` | `null` | 请求超时 |
| `RequireAuthorization` | `true` | 服务端端点是否要求授权 |
| `MessageHandlerFactory` | `null` | 自定义 `HttpMessageHandler` 工厂，主要用于测试 |
| `Endpoints` | 空 | 通道 → 端点路由表；键可为精确通道名或含 `*`/`?` 的通配符，值可含 `{channel}`/`{1}`…`{9}` 占位符 |

`HttpBusOptions` 同时绑定旧节 `HttpBus` 与新节 `Euonia:Bus:Http`，新节优先，显式 `Configure` 回调最高（`Source/Euonia.Bus.Http/HttpBusOptions.cs:8-20`、`ServiceCollectionExtensions.cs:32-40`）。

```json
{
  "Euonia": {
    "Bus": {
      "Http": {
        "Endpoint": "https://orders.local",
        "Route": "/bus/call",
        "RequestTimeout": "00:00:30",
        "RequireAuthorization": true,
        "Endpoints": {
          "payments": "https://payments.local",
          "orders-*": "https://orders.local"
        }
      }
    }
  }
}
```

```bash
Euonia__Bus__Http__Endpoint=https://orders.local     # .NET 原生：双下划线表示层级
EUONIA_BUS_HTTP_ENDPOINT=https://orders.local        # 仓库 alias：大写单下划线
Euonia__Bus__Http__RequireAuthorization=false
EUONIA_BUS_HTTP_REQUIREAUTHORIZATION=false
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `AddHttpBus(name, configure)` | 注册 HTTP 传输器与键控 `ITransporter` |
| `MapBusEndpoint()` | 映射服务端请求端点 |
| `HttpBusOptions` | 传输配置 |
| `HttpTransporter` | `ITransporter` 实现（仅调用）；`internal`，通过 `AddHttpBus` 注册 |
| `RemoteReply<TResult>` / `RemoteError` | 线路协议与错误还原（定义于 `Euonia.Bus`） |
| `RemoteReceiver` | 服务端接收、执行与回复（定义于 `Euonia.Bus`） |

## 使用示例

按通道把请求路由到不同服务，并为单次调用设置超时：

```csharp
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;

services.AddHttpBus("http", options =>
{
    options.Endpoint = "https://gateway.local";     // 默认端点
    options.Endpoints["orders"] = "https://orders.local";
    options.Endpoints["payments"] = "https://payments.local";
    options.Endpoints["report-*"] = "https://report-{channel}.local";
    options.RequireAuthorization = false;           // 仅受控内网/测试环境
});
```

```csharp
using Nerosoft.Euonia.Bus;

// [Channel] 同时决定服务端处理器注册的通道与客户端路由键
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

public sealed class OrderClient(IBus bus)
{
    public Task<OrderStatus> GetStatusAsync(long orderId, CancellationToken cancellationToken)
        => bus.Call<GetOrderStatus, OrderStatus>(new GetOrderStatus { OrderId = orderId })
              .WithTimeout(TimeSpan.FromSeconds(5))
              .ExecuteAsync(cancellationToken);
}
```

服务端最小宿主：

```csharp
using Nerosoft.Euonia.Bus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEuoniaBus();
builder.Services.AddHttpBus();
builder.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(Program).Assembly);
builder.Services.AddConfiguratorBuilder(config => config
    .SetConvention(convention => convention.Add<DefaultMessageConvention>())
    .RegisterChannel(typeof(Program).Assembly));

var app = builder.Build();
app.MapBusEndpoint();
app.Run();
```

## 注意事项 / 限制

1. **只支持请求-响应**：`PublishAsync` 与 `SendAsync` 返回失败任务并抛 `NotSupportedException`，必须使用 `CallAsync`（`Source/Euonia.Bus.Http/HttpTransporter.cs:106-128`）。
2. **服务端也需要绑定选项**：`MapBusEndpoint()` 从 `IOptions<HttpBusOptions>` 读取 `Route` 与 `RequireAuthorization`；只调用 `AddEuoniaBus()` 时使用默认值，配置节不会生效。
3. **`RequestTimeout` 是传输器级超时**：它直接设置单例 `HttpClient.Timeout`，影响该传输器的所有调用；单次调用请用 `WithTimeout`（走取消令牌）。
4. **端点解析有固定顺序**：精确键（忽略大小写）→ 通配符键（按声明顺序，首个匹配生效）→ `Endpoint` 默认端点（`HttpTransporter.cs:138-185`）。
5. **授权不会自动携带**：只有信封上的 `Authorization` 会被写成 `Authorization` 请求头；后台任务等无请求作用域的调用需要自行写入。
6. **`Enabled` 不控制注册**：是否注册传输器取决于是否调用 `AddHttpBus`，配置里的 `Enabled` 目前不被读取。

## 常见问题与故障排查

### 客户端调用返回 404

**原因**：服务端没有调用 `app.MapBusEndpoint()`，或客户端 `Route` 与服务端映射的路由不一致。

**解决**：服务端映射端点；两端共用同一 `Route`（默认 `/bus/call`），并通过 `Euonia:Bus:Http:Route` 统一覆盖（`BusEndpointExtensions.cs:23-32`）。

### 调用返回 401 / 403

**原因**：`RequireAuthorization` 默认 `true`，端点要求授权；后台任务等无请求作用域的调用没有信封 `Authorization`。

**解决**：客户端在请求作用域内调用以自动携带令牌，或显式写入信封 `Authorization`；受控内网可设 `RequireAuthorization=false`，但不要在生产关闭。

### 配置了 `Euonia:Bus:Http` 但 `Route` / `RequireAuthorization` 没变化

**原因**：服务端只调用了 `AddEuoniaBus()`，没有注册 `HttpBusOptions`；`MapBusEndpoint` 读取的是 `IOptions<HttpBusOptions>`。

**解决**：服务端也调用 `AddHttpBus()`，或显式 `services.AddOptions<HttpBusOptions>().BindConfiguration(HttpBusOptions.SectionName)`。

### 远端异常类型没有还原

**原因**：`RemoteError.ToException` 只还原系统程序集异常或 `MessageBusException` 子类，并要求类型有 `(string)` 构造函数；应用自定义异常一律回退为 `MessageDeliverException`（`RemoteError.cs:49-78`）。

**解决**：业务错误用 `MessageBusException` 子类或返回结果对象表达；客户端按 `MessageDeliverException` 兜底。

### 多服务场景请求发到了错误端点

**原因**：`Endpoints` 解析顺序为「精确键 → 通配符键（按声明顺序，首个匹配）→ 默认 `Endpoint`」，顺序或键写错会命中错误地址。

**解决**：精确通道优先；通配符按从具体到宽泛排列，并在启动日志中核对实际请求地址（`HttpTransporter.cs:138-185`）。

## 最佳实践

1. **共享消息契约程序集**：服务端处理器注册的通道与客户端路由键依赖同一份消息类型定义；异常还原只覆盖系统异常与 `MessageBusException`。
2. **单次调用用 `WithTimeout`**：`RequestTimeout` 会改单例 `HttpClient.Timeout`，影响所有调用；按调用设置更安全。
3. **生产保持 `RequireAuthorization=true` 并使用 TLS**：仅测试/受控内网显式关闭，且不要暴露 `/bus/call` 到公网。
4. **端点表集中配置**：`Endpoints` 通过 `Euonia:Bus:Http:Endpoints` 或环境变量下发，避免硬编码多套地址。
5. **为总线端点单独做健康检查**：用 `Euonia.Bus.HealthChecks` 监控积压，再对 `/bus/call` 做连通性探测。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`。
- **关键依赖**：`Microsoft.AspNetCore.App` 框架引用、`Euonia.Bus`。
- **版本策略**：随仓库 `project.props` 统一管理，本文不固定版本号。
- **已知边界**：只支持请求-响应；`Enabled` 选项当前不控制注册；旧节 `HttpBus` 仍绑定但新节 `Euonia:Bus:Http` 优先。

## 相关包

- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 总线引擎
- [`Euonia.Bus.Abstract`](../Euonia.Bus.Abstract/README.md) —— 传输契约
- [`Euonia.Bus.Grpc`](../Euonia.Bus.Grpc/README.md) —— 共享协议的 gRPC 传输

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
