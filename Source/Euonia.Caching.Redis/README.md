# Euonia.Caching.Redis

Euonia 缓存的 Redis 提供程序：基于 `StackExchange.Redis` 实现分布式缓存句柄、背板同步与连接管理，也可直接作为 `ICacheService` 使用。

Redis provider for Euonia Caching: a StackExchange.Redis-backed distributed cache handle with backplane synchronization and connection management, also usable directly as an `ICacheService`.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Redis`
- **NuGet 包**：`Euonia.Caching.Redis`

## 安装

```bash
dotnet add package Euonia.Caching.Redis
```

## 主要功能

- **分布式缓存句柄**：`WithRedisCacheHandle` 将 Redis 加入多级缓存链。
- **多种配置方式**：连接字符串、`IConnectionMultiplexer` 实例或 `RedisConfigurationBuilder`。
- **背板同步**：`WithRedisBackplane` 在节点间广播缓存变更。
- **`ICacheService` 实现**：`RedisCacheService` 提供同步/异步读写与 `GetOrAdd`、`AddOrUpdate`。
- **重试与键前缀**：`MaxRetries`、`RetryTimeout`、`KeyPrefix` 与 `Expires`。

## 快速开始

作为多级缓存中的分布式句柄：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithRedisConfiguration("redis", "localhost:6379,defaultDatabase=0")
            .WithMemoryCacheHandle("memory")
            .And.WithRedisCacheHandle("redis")
            .And.WithRedisBackplane("redis");
});
```

作为应用级 `ICacheService`：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;

services.Configure<RedisCacheOptions>(options =>
{
    options.ConnectionString = "localhost:6379";
    options.Database = 0;
    options.KeyPrefix = "app";
    options.Expires = TimeSpan.FromHours(1);
});

services.AddSingleton<ICacheService, RedisCacheService>();
```

## 配置项（`RedisCacheOptions`）

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `ConnectionString` | 必填 | StackExchange.Redis 连接字符串 |
| `Database` | `0` | 数据库编号 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式 |
| `KeyPrefix` | `null` | 键前缀 |
| `Expires` | `null` | 默认绝对过期时间 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `RedisCacheHandle<T>` | 基于 Redis 的缓存句柄 |
| `RedisCacheService` | `ICacheService` 实现 |
| `RedisConfigurationBuilder` | Redis 连接配置构建器 |
| `RedisConfigurations` | 连接与配置注册入口 |
| `RedisCacheBackplane` | 基于 Redis 发布/订阅的背板 |
| `WithRedisConfiguration(...)` | 注册 Redis 连接配置 |
| `WithRedisCacheHandle(...)` / `WithRedisBackplane(...)` | 加入句柄链 / 启用背板 |

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— 进程内内存提供程序
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
