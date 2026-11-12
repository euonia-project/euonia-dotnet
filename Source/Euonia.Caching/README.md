# Euonia.Caching

Euonia 的多级缓存框架：以缓存句柄（cache handle）分层组织内存与分布式缓存，由 `ICacheManager<TValue>` 编排，并支持背板（backplane）同步、过期策略与流式配置。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching`
- **NuGet 包**：`Euonia.Caching`
- **提供程序包**：`Euonia.Caching.Memory`、`Euonia.Caching.Redis`、`Euonia.Caching.Runtime`

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

## 快速开始

应用层最简入口是 `AddDefaultCacheManager<TComponent>`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;

services.AddDefaultCacheManager<OrdersComponent>();

public sealed class OrdersComponent { }

public sealed class OrderLookup(ICacheManager cache)
{
    public Order? Find(long id) => cache.Get<long, Order>(id);

    public Order Load(long id)
        => cache.GetOrAdd(id, _ => QueryOrderFromDatabase(id));
}
```

需要过期、统计、事件或多级句柄时，用 `CacheFactory` 构建泛型缓存管理器：

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
cache.Remove("greeting");
```

也可先构建配置再复用：

```csharp
var configuration = ConfigurationBuilder.BuildConfiguration("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithDictionaryHandle();
});

var manager = CacheFactory.FromConfiguration<string>(configuration);
```

## 主要功能

- **多级缓存**：多个句柄按配置顺序组合，读取命中上层后按 `CacheUpdateMode` 回填其他句柄。
- **提供程序可插拔**：字典句柄、`Microsoft.Extensions.Caching.Memory`、`System.Runtime.Caching`、Redis。
- **背板同步**：分布式节点通过 `CacheBackplane` 广播失效或更新，由提供程序包实现。
- **过期与统计**：绝对/滑动过期、命中/移除统计（需按句柄启用）以及 `OnAdd`/`OnGet`/`OnRemove` 等事件。
- **流式配置**：`CacheFactory.Build` 与 `ConfigurationBuilder` 组合句柄、重试、更新模式与过期策略。
- **遥测**：`CacheTelemetry` 暴露 `ActivitySource`/`Meter`（名称 `Nerosoft.Euonia.Caching`），标签只含操作、提供程序与结果。

## 核心类型

| 类型 | 作用 |
|------|------|
| `ICache<TValue>` | 基础缓存操作：Add、Get、Put、Remove、Clear、Exists、`RemoveByPrefixAsync` |
| `ICacheManager<TValue>` | 泛型管理器：在上述基础上增加 `GetOrAdd`、`AddOrUpdate`、`Update`、`Expire`、统计与事件 |
| `ICacheManager`（非泛型） | 组件级简化契约：`Get`、`TryGet`、`GetOrAdd`、`AddOrUpdate` |
| `BaseCacheManager<TValue>` | 多级编排器：遍历句柄、提升条目、背板同步与重试 |
| `CacheItem<TValue>` / `CacheExpirationMode` | 缓存条目与过期模式（`Default`、`None`、`Sliding`、`Absolute`） |
| `CacheUpdateMode` | 更新模式：`None`、`Up` |
| `CacheBackplane` | 分布式缓存同步抽象 |
| `ConfigurationBuilder` / `CacheFactory` | 流式配置与实例创建入口 |
| `ICacheService` / `BaseCacheService` | 面向应用层的简化缓存服务契约（由提供程序包实现） |
| `AddDefaultCacheManager<TComponent>` | 注册组件级 `ICacheManager`（`DefaultCacheManager<TComponent>`） |

## 选项（代码配置）

本包不读取 `IConfiguration`，也没有配置节；缓存行为通过 `CacheManagerConfiguration` 与流式构建器在代码中设置。

| 属性 / 方法 | 默认值 | 说明 |
|-------------|--------|------|
| `Name` | 随机 GUID | 缓存管理器名称 |
| `UpdateMode` / `WithUpdateMode` | `Up` | 命中后是否向其他句柄回填 |
| `MaxRetries` / `WithMaxRetries` | `50` | 更新冲突重试上限 |
| `RetryTimeout` / `WithRetryTimeout` | `100`（毫秒） | 重试前等待时间 |
| `BackplaneType` / `WithBackplane` | `null` | 背板类型、配置键与通道名 |
| `BackplaneConfigurationKey` / `BackplaneChannelName` | `null` | 背板使用的配置键与通道 |
| `CacheHandleConfigurations` | 空列表 | 通过 `With*Handle` 追加的句柄配置 |
| `WithExpiration(mode, timeout)` | 不设置 | 单句柄的默认过期策略 |
| `EnableStatistics()` / `DisableStatistics()` | 关闭 | 单句柄统计开关 |

> 不要为本包编写 `appsettings` 配置节；提供程序包的 `*Options` 同样是代码配置。

## 使用示例

两级缓存（内存 + Redis 背板），由宿主在启动时构建一次并注册为单例：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithRedisConfiguration("redis", "localhost:6379,defaultDatabase=0");

    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMaxRetries(3)
            .WithRetryTimeout(200)
            .WithMemoryCacheHandle("memory");

    settings.WithRedisCacheHandle("redis");
    settings.WithRedisBackplane("redis");
});

services.AddSingleton(cache);
```

业务侧注入泛型管理器，读穿并写回；单条过期通过 `CacheItem<TValue>` 的 `GetOrAdd` 重载表达：

```csharp
using Nerosoft.Euonia.Caching;

public sealed class UserProfileService(ICacheManager<UserProfile> cache)
{
    public UserProfile Get(long userId)
    {
        var item = cache.GetOrAdd(
            $"user:{userId}",
            key => new CacheItem<UserProfile>(
                key,
                LoadProfile(userId),
                CacheExpirationMode.Absolute,
                TimeSpan.FromMinutes(10)));

        return item.Value;
    }

    public void Invalidate(long userId)
        => cache.Remove($"user:{userId}");
}
```

## 注意事项 / 限制

- **两个 `ICacheManager` 契约不同**：`AddDefaultCacheManager<TComponent>` 注册的是非泛型 `ICacheManager`，只有 Get/TryGet/GetOrAdd/AddOrUpdate；过期、统计、事件和句柄列表只在 `ICacheManager<TValue>` 上提供。
- **不是跨进程锁**：`GetOrAdd` 通过“读取—添加—重试”实现，只在单个缓存管理器内避免重复写入；多节点并发写同一个键需配合分布式锁。
- **背板不是强一致**：背板只广播失效/更新消息，网络分区或订阅丢失期间各节点可能短暂不一致。
- **生命周期**：`ICache<TValue>` 实现 `IDisposable`；缓存管理器应作为单例长期持有，避免每次请求 `CacheFactory.Build`。
- **`Expire` 非原子**：`Expire`/`RemoveExpiration` 通过重新 `Put` 实现，分布式场景下不要依赖其严格原子性。
- **键前缀与租户**：`MemoryCacheService`/`RedisCacheService` 会先经 `IKeyTransformer`（多租户包提供租户前缀），再拼 `{KeyPrefix}.Cache.{key}`；`RuntimeCacheService` 与直接用 `ICacheManager<TValue>` 没有这层前缀。

## 相关包

- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— 进程内 `Microsoft.Extensions.Caching.Memory` 提供程序
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序与背板
- [`Euonia.Caching.Runtime`](../Euonia.Caching.Runtime/README.md) —— `System.Runtime.Caching` 提供程序
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[Cache]` / `[CacheEvict]` 拦截器
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— `IKeyTransformer` 租户键前缀

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
