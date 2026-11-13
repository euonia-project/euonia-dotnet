# Euonia.Caching.Redis 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

把缓存放到 Redis，让多个实例共享同一份数据，并用 Redis 发布/订阅背板广播失效。本页用“购物车缓存”演示连接配置、读穿、按前缀清理与运行验证。

- **NuGet 包**：`Euonia.Caching.Redis`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Redis`（`RedisCacheService` 在 `Nerosoft.Euonia.Caching`）
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching.Redis/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

购物车服务部署多个副本，购物车数据必须跨副本共享：加入商品后 30 分钟内有效，结算后立即删除；运营清理时按键前缀批量删除。这里使用独立 database（`1`），避免误清其他数据。

### 前置条件

- .NET 10 或 11 SDK。
- 可连接的 Redis；示例使用 database `1`。
- 安装 `Euonia.Caching.Redis`（会带入 `Euonia.Caching` 与 `StackExchange.Redis`）。

### 1. 安装

```bash
dotnet add package Euonia.Caching.Redis
```

### 2. 接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing.");

var cache = CacheFactory.Build<ShoppingCart>("cart", settings =>
{
    settings.WithRedisConfiguration("cart-redis", redisConnection, database: 1);

    var handle = settings.WithUpdateMode(CacheUpdateMode.Up)
                         .WithMaxRetries(3)
                         .WithRetryTimeout(200)
                         .WithRedisCacheHandle("cart-redis");
    handle?.WithExpiration(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(30));

    settings.WithRedisBackplane("cart-redis");
});

builder.Services.AddSingleton(cache);
builder.Services.AddSingleton<CartService>();

var app = builder.Build();
```

### 3. 读写与失效

```csharp
using Nerosoft.Euonia.Caching;

public sealed record CartLine(string Sku, int Quantity);

public sealed record ShoppingCart(string Id, IReadOnlyList<CartLine> Lines)
{
    public ShoppingCart Add(string sku, int quantity)
        => this with { Lines = [.. Lines, new CartLine(sku, quantity)] };
}

public sealed class CartService(ICacheManager<ShoppingCart> cache)
{
    public ShoppingCart Get(string cartId)
        => cache.GetOrAdd(
            $"cart:{cartId}",
            key => new CacheItem<ShoppingCart>(
                key,
                new ShoppingCart(cartId, []),
                CacheExpirationMode.Absolute,
                TimeSpan.FromMinutes(30))).Value;

    public ShoppingCart Add(string cartId, string sku, int quantity)
    {
        var cart = Get(cartId).Add(sku, quantity);
        cache.Put(new CacheItem<ShoppingCart>(
            $"cart:{cartId}",
            cart,
            CacheExpirationMode.Absolute,
            TimeSpan.FromMinutes(30)));
        return cart;
    }

    public bool Checkout(string cartId) => cache.Remove($"cart:{cartId}");

    public Task<long> ClearAllAsync() => cache.RemoveByPrefixAsync("cart:");
}
```

### 4. 暴露端点并验证

```csharp
app.MapGet("/carts/{id}", (string id, CartService service) => Results.Ok(service.Get(id)));

app.MapPost("/carts/{id}/items", (string id, string sku, int quantity, CartService service)
    => Results.Ok(service.Add(id, sku, quantity)));

app.MapPost("/carts/{id}/checkout", (string id, CartService service)
    => service.Checkout(id) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/carts", async (CartService service)
    => Results.Ok(new { Removed = await service.ClearAllAsync() }));
```

```bash
ConnectionStrings__Redis="localhost:6379" \
  dotnet run --urls http://localhost:5080

curl -s -X POST "http://localhost:5080/carts/c-1/items?sku=sku-1&quantity=2"
curl -s http://localhost:5080/carts/c-1
```

核对键、TTL 与清理：

```bash
redis-cli -n 1 --scan --pattern '*cart*'
redis-cli -n 1 TTL "<上一步返回的键>"
curl -X DELETE http://localhost:5080/carts
redis-cli -n 1 --scan --pattern '*cart*'
```

另开一个应用实例（同连接串）读取 `c-1`，应命中 Redis；结算后两个实例都应读不到旧购物车。

### 常见失败点

- **启动时连接失败**：检查 `ConnectionStrings:Redis`、端口、密码/TLS；只在副本节点上写会报 `No writeable endpoint found`。
- **反序列化失败或类型不对**：值以程序集限定类型名记录；DTO 改名、换程序集或改变形状后，清理旧键或注册 `TypeCache.RegisterResolveType`。
- **`Clear()` 抛 `NotSupportedException`**：实现使用 `FLUSHDB`，连接需 `allowAdmin=true`，且会清空整个 logical database；生产优先用前缀删除。
- **前缀删除变慢**：`RemoveByPrefixAsync` 使用 `SCAN`，键多时是线性操作，不要放在高频请求路径。

## 相关文档

- [API 参考](API.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Caching.Redis/README.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/TUTORIAL.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/TUTORIAL.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/TUTORIAL.md)
- [根 README](../../../README.md)
