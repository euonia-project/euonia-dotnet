# Euonia.Caching.Memory

Euonia 缓存的 `Microsoft.Extensions.Caching.Memory` 提供程序：把进程内 `IMemoryCache` 封装为缓存句柄，既可用于多级缓存配置，也可直接作为 `ICacheService` 注入应用。

Microsoft.Extensions.Caching.Memory provider for Euonia Caching: wraps an in-process `IMemoryCache` as a cache handle for multi-level configurations or exposes it directly as an `ICacheService`.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Memory`
- **NuGet 包**：`Euonia.Caching.Memory`

## 安装

```bash
dotnet add package Euonia.Caching.Memory
```

## 主要功能

- **缓存句柄**：`WithMemoryCacheHandle(...)` 将内存缓存加入 `Euonia.Caching` 的句柄链。
- **`ICacheService` 实现**：`MemoryCacheService` 支持同步/异步读写、`GetOrAdd`、`AddOrUpdate` 与按类型隔离。
- **键前缀**：`KeyPrefix` 统一为所有键添加 `{prefix}.Cache.` 前缀。
- **过期与容量**：继承 `Microsoft.Extensions.Caching.Memory.MemoryCacheOptions` 的容量限制，并额外支持 `Expires`。

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
    public Task<User> GetAsync(long id)
        => Task.FromResult(cache.GetOrAdd($"user:{id}", () => LoadUser(id), TimeSpan.FromMinutes(5)));
}
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `InstanceName` | `default` | 缓存句柄实例名 |
| `IsBackplaneSource` | `false` | 是否作为背板同步源 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式 |
| `KeyPrefix` | `null` | 键前缀 |
| `Expires` | `null` | 默认绝对过期时间 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `MemoryCacheManager` | 按选项构建并复用 `ICacheManager<T>`（内部） |
| `MemoryCacheHandle<T>` | 基于 `IMemoryCache` 的缓存句柄 |
| `MemoryCacheService` | `ICacheService` 实现 |
| `MemoryCacheOptions` | 提供程序选项 |
| `WithMemoryCacheHandle(...)` | 将内存句柄加入缓存配置 |

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
