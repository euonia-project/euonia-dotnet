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

## 常见问题与故障排查

### 注入 ICacheManager<T> 失败，只能拿到非泛型 ICacheManager
原因：`AddDefaultCacheManager<TComponent>` 注册的是组件级非泛型 `ICacheManager`，不会注册 `ICacheManager<TValue>`。
解决：需要泛型契约时用 `CacheFactory.Build<TValue>` 构建并注册实例；只需组件级简化 API 时注入非泛型 `ICacheManager`。

### GetOrAdd 偶发 InvalidOperationException: Could not get or add the item
原因：`GetOrAdd` 以“读取—添加—重试”实现，达到 `MaxRetries`（默认 50）后仍无法写入同一键时抛出。
解决：降低同一键的并发写、提高 `WithMaxRetries` 或 `WithRetryTimeout`，跨节点场景配合分布式锁串行化重建。

### 一个节点更新后，另一个节点仍读到旧值
原因：没有配置背板，或没有任何句柄被标记为背板源（`isBackplaneSource`）；背板只广播失效/更新，不保证强一致。
解决：为提供程序配置背板并至少保留一个背板源句柄（例如 Redis 的 `WithRedisBackplane` 加默认 `isBackplaneSource: true` 的句柄）。

### 写入成功却读不到
原因：读取的 `TValue` 与写入时不一致（`CacheItem<TValue>` 不协变），或键被 `KeyPrefix`/租户 `IKeyTransformer` 改写。
解决：读写使用同一泛型实参；按 `{KeyPrefix}.Cache.{key}` 与租户前缀核对实际键；失效可用 `Remove<object>(key)`。

### Expire 在并发下表现不一致
原因：`Expire`/`RemoveExpiration` 通过重新 `Put` 实现，不是原子操作。
解决：不要用它做分布式协调；需要严格 TTL 时在写入时用 `CacheItem` 指定 `Absolute`/`Sliding`。

## 最佳实践

- **单例构建**：每个进程只 `CacheFactory.Build` 一次并注册为单例，禁止按请求构建管理器或句柄。
- **按数据分类设 TTL**：外部可重建数据用绝对过期，热点数据用滑动过期；无 TTL 的键必须有显式失效路径。
- **统一键规范**：固定 `{app}:{tenant}:{entity}:{id}` 前缀，配合 `RemoveByPrefixAsync`（Memory/Redis）做批量失效。
- **监控命中与耗时**：需要统计时对句柄调用 `EnableStatistics()`；用 `CacheTelemetry` 的 `ActivitySource`/`Meter` 接入观测。
- **缓存不承担正确性**：把缓存当加速层，数据库/服务仍是事实来源；分布式场景用锁与幂等保护重建。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：抽象层仅依赖 `Microsoft.Extensions.DependencyInjection.Abstractions` 与 `Euonia.Core`；具体能力由 Memory/Redis/Runtime 提供程序包决定。
- **已知边界**：字典/内存句柄只在进程内共享，只有配置了分布式提供程序与背板才跨节点；`CacheUpdateMode.Up` 只回填句柄链，不代表写穿所有节点。
- **升级注意**：Redis 缓存值含程序集限定类型名，DTO 改名或迁移程序集后旧键可能无法反序列化；滚动升级期间新旧实例可能同时消费背板，建议先小流量验证并按提供程序清理不兼容键。
