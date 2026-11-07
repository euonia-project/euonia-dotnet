# Euonia.Caching.Runtime

Euonia 缓存的 `System.Runtime.Caching` 提供程序：把 `MemoryCache` 封装为缓存句柄，适用于仍在使用 `System.Runtime.Caching` 配置模型（内存限制、轮询间隔）的场景。

System.Runtime.Caching provider for Euonia Caching: wraps `MemoryCache` as a cache handle for scenarios that rely on the `System.Runtime.Caching` configuration model (memory limits, polling interval).

- **目标框架**：`netstandard2.1`、`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Runtime`
- **NuGet 包**：`Euonia.Caching.Runtime`

## 安装

```bash
dotnet add package Euonia.Caching.Runtime
```

## 主要功能

- **缓存句柄**：`WithRuntimeCacheHandle(...)` 将 `System.Runtime.Caching.MemoryCache` 加入缓存链。
- **`ICacheService` 实现**：`RuntimeCacheService` 提供与内存/Redis 提供程序一致的 API。
- **资源限制**：`CacheMemoryLimitMegabytes`、`PhysicalMemoryLimitPercentage` 与 `PollingInterval`。
- **键前缀与过期**：`KeyPrefix`、`Expires` 与更新/重试策略。

## 快速开始

作为多级缓存句柄：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Runtime;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithRuntimeCacheHandle("runtime");
});
```

作为应用级 `ICacheService`：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Runtime;

services.Configure<RuntimeCacheOptions>(options =>
{
    options.InstanceName = "app";
    options.KeyPrefix = "app";
    options.CacheMemoryLimitMegabytes = 256;
    options.PollingInterval = TimeSpan.FromMinutes(1);
});

services.AddSingleton<ICacheService, RuntimeCacheService>();
```

## 配置项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `InstanceName` | `default` | 缓存实例名 |
| `IsBackplaneSource` | `false` | 是否作为背板同步源 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式 |
| `KeyPrefix` | `null` | 键前缀 |
| `Expires` | `null` | 默认绝对过期时间 |
| `CacheMemoryLimitMegabytes` | `0` | 缓存内存上限（MB），`0` 表示不限制 |
| `PhysicalMemoryLimitPercentage` | `0` | 物理内存占用上限（百分比） |
| `PollingInterval` | `2 分钟` | 过期扫描间隔 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `RuntimeCacheHandle<T>` | 基于 `System.Runtime.Caching` 的缓存句柄 |
| `RuntimeCacheService` | `ICacheService` 实现 |
| `RuntimeCacheOptions` | 提供程序选项 |
| `WithRuntimeCacheHandle(...)` | 将运行时缓存句柄加入配置 |

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— `Microsoft.Extensions.Caching.Memory` 提供程序
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
