# Euonia.Caching.Redis

Euonia 缓存的 Redis 提供程序：基于 `StackExchange.Redis` 实现分布式缓存句柄、发布/订阅背板与连接管理，也可直接作为 `ICacheService` 使用。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Redis`（`RedisCacheService` 位于 `Nerosoft.Euonia.Caching`）
- **NuGet 包**：`Euonia.Caching.Redis`
- **依赖**：`Euonia.Caching`、`StackExchange.Redis`、`Microsoft.Extensions.Options`

## 安装

```bash
dotnet add package Euonia.Caching.Redis
```

## 快速开始

作为多级缓存中的分布式句柄与背板：

```csharp
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithRedisConfiguration("redis", "localhost:6379,defaultDatabase=0");

    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMemoryCacheHandle("memory");

    settings.WithRedisCacheHandle("redis");
    settings.WithRedisBackplane("redis");
});
```

作为应用级 `ICacheService`：

```csharp
using Microsoft.Extensions.DependencyInjection;
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

## 主要功能

- **分布式缓存句柄**：`WithRedisCacheHandle` 把 Redis 加入多级缓存链，默认作为背板数据源。
- **多种连接配置**：连接字符串、现有 `IConnectionMultiplexer` 实例或 `RedisConfigurationBuilder`。
- **背板同步**：`WithRedisBackplane` 在节点间通过发布/订阅广播缓存变更。
- **`ICacheService` 实现**：`RedisCacheService` 提供同步/异步读写与 `GetOrAdd`、`AddOrUpdate`，内部自动装配句柄与背板。
- **连接复用**：内部连接管理器按连接字符串缓存 `IConnectionMultiplexer`。
- **前缀删除**：`RemoveByPrefixAsync` 使用 `SCAN` 分批删除匹配前缀的键。

## 核心类型

| 类型 | 作用 |
|------|------|
| `RedisCacheHandle<TValue>` | 基于 Redis 的缓存句柄 |
| `RedisCacheService` | `ICacheService` 实现（命名空间 `Nerosoft.Euonia.Caching`） |
| `RedisCacheOptions` | `RedisCacheService` 的代码选项 |
| `RedisConfigurationBuilder` / `RedisConfigurations` | 连接配置构建与注册入口 |
| `RedisCacheBackplane` | 基于 Redis 发布/订阅的背板 |
| `WithRedisConfiguration(...)` | 注册连接配置（字符串、`IConnectionMultiplexer` 或构建器回调） |
| `WithRedisCacheHandle(...)` / `WithRedisBackplane(...)` | 加入句柄链 / 启用背板 |

## 选项（代码配置）

本包不读取 `IConfiguration`，没有配置节；`RedisCacheService` 的选项通过 `services.Configure<RedisCacheOptions>(...)` 设置，多级缓存则通过 `WithRedisConfiguration(...)` / `WithRedisCacheHandle(...)` 的代码参数设置。

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `ConnectionString` | 必填 | StackExchange.Redis 连接字符串；为空时构建配置即抛异常 |
| `Database` | `0` | 数据库编号 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式（`None`/`Up`） |
| `KeyPrefix` | `null` | 键前缀；服务层再拼 `.Cache.` |
| `Expires` | `null` | 默认绝对过期时间；仅在设置时以 `Absolute` 应用 |

> 不要为本包编写 `appsettings` 配置节。连接串可由宿主从 `ConnectionStrings` 读取后赋给 `RedisCacheOptions.ConnectionString`。

## 使用示例

多级缓存：内存一级、Redis 二级，并用 Redis 背板广播失效。

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;
using Nerosoft.Euonia.Caching.Redis;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithRedisConfiguration("redis", "localhost:6379,defaultDatabase=0");

    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMaxRetries(3)
            .WithMemoryCacheHandle("memory");

    settings.WithRedisCacheHandle("redis", isBackplaneSource: true);
    settings.WithRedisBackplane("redis");
});

services.AddSingleton(cache);
```

应用级服务：

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Redis;

public static void AddRedisCache(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

    services.Configure<RedisCacheOptions>(options =>
    {
        options.ConnectionString = connectionString;
        options.Database = 1;
        options.KeyPrefix = "app";
    });

    services.AddSingleton<ICacheService, RedisCacheService>();
}
```

> 使用 `WithRedisConfiguration(..., enableKeyspaceNotifications: true)` 时，服务端 `notify-keyspace-events` 至少要包含 `Exe`，否则收不到逐出/过期事件。

## 注意事项 / 限制

- **`ConnectionString` 必填**：为空时 `WithRedisConfiguration` 直接抛参数异常；连接串不可达时在首次建立多路复用器时失败。
- **序列化约束**：常用基元类型原生存储，其它类型使用 `System.Text.Json` 并以类型名还原；值必须可 JSON 序列化，类型需能被反序列化端解析。
- **背板需要数据源**：启用背板时至少要有一个句柄作为背板源（`WithRedisCacheHandle` 默认 `isBackplaneSource: true`）。
- **前缀删除是扫描**：`RemoveByPrefixAsync` 对每个已连接服务器执行 `SCAN`，复杂度与键数量相关，不要放在高频路径。
- **服务层自动装配背板**：`RedisCacheService` 每次构造都会重新构建句柄与背板配置；不要按请求创建，应注册为单例。
- **键空间通知非默认**：只有显式开启且服务端配置正确时才感知逐出/过期事件；否则跨节点失效依赖显式删除或背板消息。

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— 进程内内存提供程序
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序
- [`Euonia.Concurrency.Redis`](../Euonia.Concurrency.Redis/README.md) —— Redis 分布式锁与信号量

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
