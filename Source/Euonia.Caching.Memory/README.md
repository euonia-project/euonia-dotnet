# Euonia.Caching.Memory

Euonia 缓存的 `Microsoft.Extensions.Caching.Memory` 提供程序：把进程内 `IMemoryCache` 封装为缓存句柄，也可直接实现 `ICacheService` 供应用注入。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Memory`
- **NuGet 包**：`Euonia.Caching.Memory`
- **依赖**：`Euonia.Caching`、`Microsoft.Extensions.Caching.Memory`、`Microsoft.Extensions.Options`

## 安装

```bash
dotnet add package Euonia.Caching.Memory
```

## 快速开始

作为多级缓存句柄：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMemoryCacheHandle("memory");
});
```

作为应用级 `ICacheService`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

services.Configure<MemoryCacheOptions>(options =>
{
    options.InstanceName = "app";
    options.KeyPrefix = "app";
    options.Expires = TimeSpan.FromMinutes(10);
});

services.AddSingleton<ICacheService, MemoryCacheService>();
```

```csharp
public sealed class UserService(ICacheService cache)
{
    public User Get(long id)
        => cache.GetOrAdd($"user:{id}", () => LoadUser(id), TimeSpan.FromMinutes(5));
}
```

## 主要功能

- **缓存句柄**：`WithMemoryCacheHandle(...)` 把内存缓存加入 `Euonia.Caching` 的句柄链。
- **`ICacheService` 实现**：`MemoryCacheService` 提供同步/异步读写、`GetOrAdd`、`AddOrUpdate` 与 `Remove`。
- **键前缀**：`KeyPrefix` 统一把键改写为 `{KeyPrefix}.Cache.{key}`（已带该前缀时不重复添加）。
- **按前缀删除**：`RemoveByPrefixAsync` 删除通过本句柄写入且匹配前缀的键。
- **过期与容量**：既支持 `Microsoft.Extensions.Caching.Memory` 的容量/压缩选项，也支持 `Expires` 默认绝对过期。

## 核心类型

| 类型 | 作用 |
|------|------|
| `MemoryCacheHandle<TValue>` | 基于 `IMemoryCache` 的缓存句柄，支持区域与前缀删除 |
| `MemoryCacheService` | `ICacheService` 实现，内部按值类型复用缓存管理器 |
| `MemoryCacheOptions` | 提供程序选项，继承 `Microsoft.Extensions.Caching.Memory.MemoryCacheOptions` |
| `WithMemoryCacheHandle(...)` | 将内存句柄加入缓存配置的扩展方法 |

## 选项（代码配置）

本包不读取 `IConfiguration`，没有配置节；选项通过 `services.Configure<MemoryCacheOptions>(...)` 或 `WithMemoryCacheHandle(..., options)` 在代码中设置。

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `InstanceName` | `"default"` | 缓存实例名，也是句柄标识 |
| `IsBackplaneSource` | `false` | 是否作为背板数据源；当前实现未读取 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式（`None`/`Up`） |
| `KeyPrefix` | `null` | 键前缀；服务层再拼 `.Cache.` |
| `Expires` | `null` | 默认绝对过期时间；仅在设置时以 `Absolute` 应用 |
| `SizeLimit` 等基类选项 | 由基类决定 | `CompactionPercentage`、`ExpirationScanFrequency`、`Tracking` 等由 `Microsoft.Extensions.Caching.Memory` 提供 |

> 不要为本包编写 `appsettings` 配置节。需要从配置读取前缀时，由宿主读取后赋给 `MemoryCacheOptions`。

## 使用示例

多级缓存中作为一级（L1），并显式传入选项：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMemoryCacheHandle("memory", new MemoryCacheOptions
            {
                InstanceName = "memory",
                KeyPrefix = "app",
                Expires = TimeSpan.FromMinutes(5)
            });
});

services.AddSingleton(cache);
```

应用级服务与多租户键前缀：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;
using Nerosoft.Euonia.Tenancy;

services.AddTenancy();               // 注册 IKeyTransformer，为键加租户前缀
services.Configure<MemoryCacheOptions>(options => options.KeyPrefix = "app");
services.AddSingleton<ICacheService, MemoryCacheService>();
```

## 注意事项 / 限制

- **进程内、不跨节点**：每个进程各自持有内存缓存；多实例部署需要 `Euonia.Caching.Redis` 等分布式提供程序。
- **存储按选项实例共享**：同一 `MemoryCacheOptions` 实例下，不同 `TValue` 的句柄共享同一键空间，因此 `Remove<object>(key)` 可以跨类型删除；不要指望值类型之间天然隔离。
- **前缀删除只覆盖登记过的键**：`RemoveByPrefixAsync` 依赖句柄写入时登记的键集合；外部直接写入同一 `IMemoryCache` 的键不会被删除。
- **过期优先级**：`ICacheService` 的 `timeout` 参数优先于 `Expires`；`Expires` 以绝对过期写入，不会滑动续期。
- **读取要求泛型一致**：`Get<TValue>`/`TryGet<TValue>` 按写入时的 `CacheItem<TValue>` 类型存取，用不同类型读取会视为未命中；`Remove<TValue>` 只按 key 匹配。
- **进程/应用池回收即丢失**：不要存放无法从数据源重建的数据。

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— `IKeyTransformer` 租户键前缀

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
