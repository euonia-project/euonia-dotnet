# Euonia.Caching 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

Euonia 多级缓存核心：用缓存句柄组合进程内与分布式缓存，由 `ICacheManager<TValue>` 统一读取、写入、过期与失效。本页用一个商品目录读穿缓存场景串起构建、注册、失效与验证。

- **NuGet 包**：`Euonia.Caching`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

商品目录服务同时使用内存（L1）与 Redis（L2）：读取时优先命中内存，未命中则回源并写入两层；价格变更时删除单键，批量下架时按前缀清理。Redis 背板让其他节点收到失效消息。

### 前置条件

- .NET 10 或 11 SDK。
- 可连接的 Redis（本地 `localhost:6379` 即可）。
- 安装 `Euonia.Caching`、`Euonia.Caching.Memory`、`Euonia.Caching.Redis`。

### 1. 安装

```bash
dotnet add package Euonia.Caching
dotnet add package Euonia.Caching.Memory
dotnet add package Euonia.Caching.Redis
```

### 2. 构建一次缓存管理器并注册

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;
using Nerosoft.Euonia.Caching.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing.");

var cache = CacheFactory.Build<CatalogItem>("catalog", settings =>
{
    settings.WithRedisConfiguration("redis", redisConnection);

    var memory = settings.WithUpdateMode(CacheUpdateMode.Up)
                         .WithMaxRetries(5)
                         .WithRetryTimeout(200)
                         .WithMemoryCacheHandle("catalog-memory");
    memory?.WithExpiration(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(2));

    var distributed = settings.WithRedisCacheHandle("redis");
    distributed?.WithExpiration(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(30));

    settings.WithRedisBackplane("redis");
});

builder.Services.AddSingleton(cache);
builder.Services.AddSingleton<CatalogService>();

var app = builder.Build();
```

### 3. 读穿、写入与失效

```csharp
using Nerosoft.Euonia.Caching;

public sealed record CatalogItem(string Id, decimal Price);

public sealed class CatalogService(ICacheManager<CatalogItem> cache)
{
    public CatalogItem Get(string id)
        => cache.GetOrAdd(
            $"catalog:{id}",
            key => new CacheItem<CatalogItem>(
                key,
                Load(id),
                CacheExpirationMode.Absolute,
                TimeSpan.FromMinutes(30))).Value;

    public void SetPrice(string id, decimal price)
        => cache.Put(new CacheItem<CatalogItem>(
            $"catalog:{id}",
            new CatalogItem(id, price),
            CacheExpirationMode.Absolute,
            TimeSpan.FromMinutes(30)));

    public bool Invalidate(string id) => cache.Remove($"catalog:{id}");

    public Task<long> InvalidateAllAsync() => cache.RemoveByPrefixAsync("catalog:");

    // 数据库/远端服务读取，示例返回固定值。
    private static CatalogItem Load(string id) => new(id, 19.9m);
}
```

### 4. 暴露端点

```csharp
app.MapGet("/catalog/{id}", (string id, CatalogService service) => Results.Ok(service.Get(id)));

app.MapPost("/catalog/{id}/price", (string id, decimal price, CatalogService service) =>
{
    service.SetPrice(id, price);
    service.Invalidate(id);
    return Results.NoContent();
});

app.MapDelete("/catalog", async (CatalogService service)
    => Results.Ok(new { Removed = await service.InvalidateAllAsync() }));
```

### 运行与验证

```bash
ConnectionStrings__Redis="localhost:6379" \
  dotnet run --urls http://localhost:5080

curl -s http://localhost:5080/catalog/sku-1
curl -s http://localhost:5080/catalog/sku-1
```

两次请求中第二次应直接命中 L1（可在日志/指标中观察回源次数）。查看 Redis 键与 TTL：

```bash
redis-cli --scan --pattern '*catalog*'
redis-cli TTL "<上一步返回的键>"
```

触发批量失效后，键应被删除：

```bash
curl -X DELETE http://localhost:5080/catalog
```

### 常见失败点

- **启动时提示没有 Redis 配置**：必须在 `WithRedisCacheHandle`/`WithRedisBackplane` 之前用同一配置键调用 `WithRedisConfiguration`。
- **另一个节点仍返回旧值**：确认已配置背板，且至少一个句柄是背板源（Redis 句柄默认 `isBackplaneSource: true`）。
- **读不到刚写入的值**：读写必须使用同一 `TValue`；`CacheItem<TValue>` 不协变，跨类型读取会未命中。
- **每个请求都回源**：检查是否误在请求作用域内 `CacheFactory.Build`，管理器应注册为单例。

## 相关文档

- [API 参考](API.md)
- [项目 README：安装、核心类型与行为边界](../../../Source/Euonia.Caching/README.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/TUTORIAL.md)
- [Euonia.Caching.Redis 用户手册](../Euonia.Caching.Redis/TUTORIAL.md)
- [Euonia.Caching.Runtime 用户手册](../Euonia.Caching.Runtime/TUTORIAL.md)
- [根 README](../../../README.md)
