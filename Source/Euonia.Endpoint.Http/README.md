# Euonia.Endpoint.Http

把 `Euonia.Endpoint` 标记的接口方法映射为 ASP.NET Core Minimal API 端点。

## 目标框架

- `net10.0`
- `net11.0`

依赖 ASP.NET Core 共享框架。

## 工作方式

采用“生成优先 + 运行时兜底”：

1. `Euonia.Endpoint.SourceGenerator` 在编译期生成强类型 lambda，并写入 `[assembly: HttpGeneratedMapping(...)]` 标记；
2. `MapDynamicHttpEndpoint` 先调用生成的 `MapGenerated`；
3. 运行时扫描器再扫描配置范围内的程序集，为生成器遗漏的接口/方法补映射；
4. `HttpRegistry` 以“路由 + HTTP 方法”为粒度去重，避免重复注册。

生成路径在请求执行时没有反射调用，性能更好；运行时路径用于插件、动态加载或生成器无法处理的签名。

## 安装

```bash
dotnet add package Euonia.Endpoint
dotnet add package Euonia.Endpoint.Http
dotnet add package Euonia.Endpoint.SourceGenerator
```

## 注册

### 从配置注册

```csharp
builder.Services.AddDynamicHttpEndpoint(builder.Configuration);
```

对应配置节：

```json
{
  "Euonia": {
    "Endpoint": {
      "Http": {
        "EnableExposeApplicationServiceAsWebApi": true,
        "DefaultRoutePrefix": "api",
        "ScanAssemblies": [ "MyApp.Application" ],
        "ScanNamespaces": [ "MyApp.Application.Services" ],
        "ScanTypes": [],
        "ScanEntryAssembly": false
      }
    }
  }
}
```

### 使用回调注册

```csharp
builder.Services.AddDynamicHttpEndpoint(options =>
{
    options.EnableExposeApplicationServiceAsWebApi = true;
    options.DefaultRoutePrefix = "api";
});
```

### 显式指定扫描程序集

```csharp
builder.Services.AddDynamicHttpEndpoint(
    options => options.EnableExposeApplicationServiceAsWebApi = true,
    typeof(IOrderService).Assembly);
```

显式传入程序集后，不再使用配置中的 `ScanAssemblies` / 入口程序集回退。

## 映射

```csharp
var app = builder.Build();

app.MapDynamicHttpEndpoint();

app.Run();
```

也可以放在 `UseEndpoints` 中：

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapDynamicHttpEndpoint();
});
```

## 选项

| 选项 | 默认值 | 说明 |
|---|---|---|
| `EnableExposeApplicationServiceAsWebApi` | `false` | 总开关；关闭时不注册/不映射 |
| `DefaultRoutePrefix` | `"api"` | `Prefix` 为空时使用的默认前缀 |
| `RequireAuthorization` | `true` | 对每个映射端点调用 `.RequireAuthorization()` |
| `ThrowOnUnregisteredService` | `true` | 标记了特性但服务未注册时抛异常；`false` 时跳过并记录警告 |
| `ThrowOnRouteConflict` | `true` | 路由冲突时抛异常；`false` 时跳过并记录警告 |
| `ScanAssemblies` | 空 | 要扫描的程序集名称；配置后不再回退入口程序集 |
| `ScanNamespaces` | 空 | 命名空间前缀过滤；按 `.` 段边界匹配 |
| `ScanTypes` | 空 | 类型完整名称过滤 |
| `ScanEntryAssembly` | `true` | 未配置 `ScanAssemblies` 时是否扫描入口程序集 |

`ScanNamespaces` / `ScanTypes` 同时作用于生成路径与运行时兜底，不会出现“配置收窄了但生成端点仍全部暴露”的情况。

## 路由规则

```csharp
public interface IOrderService
{
    // api/order/{id}
    [ExposeWebApi(Prefix = "api/orders", Route = "{id}", Method = new[] { "GET" })]
    Task<OrderDto> GetAsync(string id);

    // api/order/create（Prefix/Route 都为空时按服务名 + 方法名推导）
    [ExposeWebApi(Method = new[] { "POST" })]
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
}
```

推导规则：

- 服务名：去掉接口前缀 `I`，去掉 `ApplicationService` / `AppService` / `Service` 后缀，再转 kebab-case；
- 方法名：去掉 `Async` 后缀，再转 kebab-case；
- `Prefix` 为空：`DefaultRoutePrefix` + 服务名；
- `Route` 为空：方法名；
- 前缀/模板首尾的 `/` 会被去掉。

HTTP 方法只接受：

```text
GET POST PUT DELETE PATCH HEAD OPTIONS TRACE CONNECT
```

数组为空或包含未知方法时，生成/运行时映射会失败并按配置抛异常或跳过。

## 参数绑定

| 参数 | 来源 | 说明 |
|---|---|---|
| 与路由模板同名的参数 | Route | 支持基础类型、枚举、`Guid`、日期时间类型、`Uri` |
| 基础类型 / 枚举 / `Guid` / 日期时间 / `Uri` | Query | 集合参数按重复 query 读取 |
| 复杂类型 / `byte[]` | JSON Body | 最多一个 body 参数 |
| `CancellationToken` | `RequestAborted` | 自动注入 |
| `HttpContext` | 当前上下文 | 自动注入 |
| `HttpRequest` / `HttpResponse` | 当前请求/响应 | 自动注入 |
| `ClaimsPrincipal` | `HttpContext.User` | 自动注入 |
| `Stream` / `PipeReader` | 请求体 | 自动注入 |

HTTP DTO 使用 `System.Text.Json`，不需要 `[DataContract]` / `[DataMember]`；这些标注只用于 gRPC/protobuf-net。

请求体规则：

- 必填 body 为空 → `400`；
- JSON 字面量 `null` → `400`；
- 非法 JSON → `400`；
- Content-Type 非 JSON → `400`；
- 绑定内部异常 → `500`，原始异常消息只写日志，不返回给客户端；
- `Task` / `ValueTask` → `204 No Content`；
- `Task<T>` / `ValueTask<T>` → `200 OK` + JSON；
- `IResult` → 原样返回。

缺失的可选集合参数（数组、`List<T>`、`ISet<T>`、`IReadOnlySet<T>` 等）返回空集合，不注入 `null`。

## 返回类型

```csharp
Task DoAsync();                       // 204
Task<OrderDto> GetAsync();            // 200 + JSON
ValueTask<OrderDto> GetAsync();       // 200 + JSON
Task<IResult> HandleAsync();          // IResult 透传
```

## 认证与元数据

端点元数据合并以下位置的 `[Authorize]` / `[AllowAnonymous]`：

1. 接口方法；
2. 实现类；
3. 实现方法。

多实现场景下无法确定 DI 实际解析哪个实现，因此只使用接口方法元数据，不合并任何实现类/方法特性，避免任一实现的 `[AllowAnonymous]` 误放开端点。

```csharp
[Authorize(Roles = "admin")]
public sealed class OrderService : IOrderService
{
    [Authorize(Policy = "orders:read")]
    public Task<OrderDto> GetAsync(string id) => ...;
}
```

`RequireAuthorization = true` 会再追加默认授权要求；`[AllowAnonymous]` 按 ASP.NET Core 元数据语义覆盖授权。

其它自定义特性不会自动写入 Minimal API 元数据。

## 生成路径与运行时兜底

- 生成路径：编译期 lambda + 精确参数类型匹配，避免反射和重载歧义；
- 运行时路径：反射调用，覆盖插件程序集和生成器跳过的签名；
- 冲突：同一路由 + 方法只保留一个；运行时可以补充生成路径没有占用的 HTTP 动词；
- 开关关闭时两者都不映射。

## 扫描范围

默认只扫描入口程序集：

```csharp
builder.Services.AddDynamicHttpEndpoint(options =>
{
    options.EnableExposeApplicationServiceAsWebApi = true;
    options.ScanEntryAssembly = true; // 默认
});
```

插件/多程序集场景显式配置：

```csharp
builder.Services.AddDynamicHttpEndpoint(
    options =>
    {
        options.EnableExposeApplicationServiceAsWebApi = true;
        options.ScanAssemblies = new[] { "MyApp.Application", "MyPlugin" };
        options.ScanEntryAssembly = false;
    });
```

## 不支持或限制

- 文件上传（`IFormFile` / `IFormFileCollection`）；
- 流式返回（`IAsyncEnumerable<T>`）；
- 多个 body 参数；
- `ref` / `out` / `in` / `params` 参数；
- 泛型方法、静态方法；
- 生成器只处理接口方法；实现类上的同签名方法不会被单独扫描；
- 非 `Authorize` / `AllowAnonymous` 的自定义特性不会进入端点元数据。

这些签名在生成路径会被跳过，运行时路径可能支持一部分，也可能在启动/请求时拒绝。

## 完整示例

```csharp
using Microsoft.AspNetCore.Authorization;
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

[Authorize]
public sealed class OrderService : IOrderService
{
    public Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = id });

    public Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderDto { Id = Guid.NewGuid().ToString("N") });
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDynamicHttpEndpoint(builder.Configuration);

var app = builder.Build();
app.MapDynamicHttpEndpoint();
app.Run();
```

## 相关文档

- [Euonia.Endpoint](../Euonia.Endpoint/README.md)
- [Euonia.Endpoint.Grpc](../Euonia.Endpoint.Grpc/README.md)
- [Euonia.Endpoint.SourceGenerator](../Euonia.Endpoint.SourceGenerator/README.md)
