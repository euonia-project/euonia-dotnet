# Euonia.Caching

Euonia 的多级缓存框架：以缓存句柄（cache handle）分层组织内存与分布式缓存，由 `ICacheManager<T>` 统一编排，支持背板（backplane）同步、过期策略与流式配置。

Multi-level caching framework for Euonia: layered cache handles for in-process and distributed caches, orchestrated by `ICacheManager<T>`, with backplane synchronization, expiration policies and fluent configuration.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Caching`
- **NuGet 包**：`Euonia.Caching`

## 安装

```bash
dotnet add package Euonia.Caching
```

按需添加提供程序：

```bash
dotnet add package Euonia.Caching.Memory
dotnet add package Euonia.Caching.Redis
dotnet add package Euonia.Caching.Runtime
```

## 主要功能

- **多级缓存**：多个句柄按顺序组合，读取时向上提升条目（`CacheUpdateMode.Up`）。
- **提供程序可插拔**：字典、`Microsoft.Extensions.Caching.Memory`、`System.Runtime.Caching`、Redis。
- **背板同步**：分布式节点通过 `CacheBackplane` 通知失效或更新。
- **统计与事件**：`OnAdd`、`OnGet`、`OnRemove`、`OnRemoveByHandle` 等事件。
- **流式配置**：`CacheFactory.Build` / `ConfigurationBuilder` 组合句柄、重试、更新模式与过期策略。

## 快速开始

```csharp
using Nerosoft.Euonia.Caching;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMaxRetries(3)
            .WithDictionaryHandle();
});

cache.Add("greeting", "hello");
cache.Put("user:1", "Alice");

var greeting = cache.Get("greeting");
var user = cache.GetOrAdd("user:2", _ => LoadUserName(2));

cache.Remove("greeting");
```

使用构建器预先生成配置：

```csharp
var configuration = ConfigurationBuilder.BuildConfiguration("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithDictionaryHandle();
});

var manager = CacheFactory.FromConfiguration<string>("app", configuration);
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ICache<TValue>` | 基础缓存操作：Add、Get、Put、Remove、Clear、Exists |
| `ICacheManager<TValue>` | 扩展操作：`GetOrAdd`、`AddOrUpdate`、过期、统计与事件 |
| `BaseCacheManager<TValue>` | 多级编排器：遍历句柄、提升条目、背板同步 |
| `CacheItem<TValue>` / `CacheExpirationMode` | 缓存条目与过期模式 |
| `CacheUpdateMode` | 更新模式（`Up`、`None` 等） |
| `CacheBackplane` | 分布式缓存同步抽象 |
| `ConfigurationBuilder` / `CacheFactory` | 流式配置与实例创建入口 |
| `ICacheService` / `BaseCacheService` | 面向应用层的简化缓存服务契约 |

## 相关包

- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— 进程内内存提供程序
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[Cache]` / `[CacheEvict]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
