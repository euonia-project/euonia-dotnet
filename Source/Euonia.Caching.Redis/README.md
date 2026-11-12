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

## 常见问题与故障排查

### 启动时报 InvalidOperationException: Connection to '...' failed
原因：连接串不可达、密码/TLS 不匹配、防火墙拦截，或只连到副本（会抛 `No writeable endpoint found`）。
解决：先用 `redis-cli -u <connection-string> PING` 验证；检查 `ssl`、`password`、端点与网络；多节点至少保留一个可写主节点。

### 调用 Clear() 抛 NotSupportedException，提示需要 allowAdmin
原因：`Clear` 的实现是对当前逻辑库执行 `FLUSHDB`，连接未启用 `allowAdmin` 时 Redis 拒绝；而且 `FLUSHDB` 会清空整个库，不只是本缓存。
解决：为缓存使用独立数据库；确需 `Clear` 时在连接串加 `allowAdmin=true`（或配置构建器 `WithAllowAdmin()`），否则改用按前缀删除。

### 升级/重构后读取缓存报 Could not load type 或类型转换失败
原因：Redis 中的值以 `AssemblyQualifiedName` 记录原类型，DTO 改名、换命名空间或迁移程序集后无法还原。
解决：上线前清理相关键或使用新前缀；无法立即清键时用 `TypeCache.RegisterResolveType` 注册旧名到新类型的解析器。

### 启动时报 No Redis configuration has been registered for the key 'x'
原因：`WithRedisCacheHandle`/`WithRedisBackplane` 之前没有用同一配置键调用 `WithRedisConfiguration`。
解决：先注册连接配置，键名保持一致；`RedisCacheService` 路径由内部固定键自动注册，不需要手工调用。

### 开启键空间通知后仍收不到逐出/过期事件
原因：只配置了客户端开关，Redis 服务端 `notify-keyspace-events` 未包含 `Exe`；或背板缺少数据源句柄。
解决：服务端执行 `CONFIG SET notify-keyspace-events Exe`（并持久化配置），客户端使用 `enableKeyspaceNotifications: true` 或 `EnableKeyspaceEvents()`。

## 最佳实践

- **默认带 TTL**：所有写入使用 `Expires` 或 `CacheItem` 过期；永久键必须有明确的失效责任方。
- **独立数据库与前缀**：缓存使用独立 logical database；键前缀包含应用与租户，避免与其他系统或环境冲突。
- **谨慎使用 Clear**：优先 `RemoveByPrefixAsync` 定向失效；必须 `Clear` 时限制 `allowAdmin` 的作用域并确认库是专用的。
- **连接串显式化**：设置合理的 `connectTimeout`/`syncTimeout`，并为连接失败与重连建立告警；同一连接串在进程内复用多路复用器。
- **DTO 保持可序列化**：使用公开属性的简单 DTO，避免依赖运行时多态；类型移动时同步规划键迁移。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：`StackExchange.Redis`、`Euonia.Caching`；按 Redis 服务端能力自动选择 LUA 或兼容路径。
- **已知边界**：`RedisCacheService` 构造时固定装配句柄与背板；`Clear` 依赖 `allowAdmin`；键空间通知需要服务端配置；前缀删除使用 `SCAN`，不是 O(1)。
- **升级注意**：缓存值含程序集限定类型名，升级前评估 DTO 兼容性；滚动升级期间新旧实例共享同一数据库时，建议先灰度并保留回滚窗口。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
