# Euonia.Caching API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

Euonia 多级缓存核心：用缓存句柄组合进程内与分布式缓存，由 `ICacheManager<TValue>` 统一读取、写入、过期与失效。本页用一个商品目录读穿缓存场景串起构建、注册、失效与验证。

- **NuGet 包**：`Euonia.Caching`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `CacheFactory.Build<TValue>(name, settings)` | 创建并配置泛型缓存管理器 | 名称必填 |
| `CacheFactory.FromConfiguration<TValue>(config)` | 从已构建配置创建管理器 | `CacheManagerConfiguration` |
| `ConfigurationBuilder.BuildConfiguration(name, settings)` | 构建可复用的缓存配置 | 名称必填 |
| `ICache<TValue>` | 基础操作契约 | `Add/Get/Put/Remove/Clear/Exists` |
| `ICacheManager<TValue>` | 在基础操作上增加 `GetOrAdd/AddOrUpdate/Update/Expire` 与事件 | 继承 `ICache<TValue>` |
| `ICacheManager`（非泛型） | 组件级简化契约 | 仅 `Get/TryGet/GetOrAdd/AddOrUpdate` |
| `AddDefaultCacheManager<TComponent>()` | 注册非泛型 `ICacheManager` | `DefaultCacheManager<TComponent>` |
| `CacheManagerConfiguration.UpdateMode` | 命中后是否向其他句柄回填 | 默认 `Up` |
| `CacheManagerConfiguration.MaxRetries` | 更新冲突重试次数 | 默认 `50` |
| `CacheManagerConfiguration.RetryTimeout` | 重试等待毫秒数 | 默认 `100` |
| `WithMemoryCacheHandle(...)` | 加入内存句柄 | 提供程序包扩展，默认实例名随机 GUID |
| `WithRedisCacheHandle(key, isBackplaneSource)` | 加入 Redis 句柄 | `isBackplaneSource` 默认 `true` |
| `WithRedisBackplane(key[, channel])` | 启用 Redis 背板 | 需要已注册的 Redis 配置 |
| `WithExpiration(mode, timeout)` | 设置句柄默认过期 | `Absolute`/`Sliding` |
| `ICache.RemoveByPrefixAsync(prefix, ct)` | 按前缀删除 | Memory/Redis 有实现；返回删除数 |
| `CacheItem<TValue>(key, value, mode, timeout)` | 带过期信息的缓存项 | `timeout` 必须大于 0 |
| `CacheExpirationMode` | 过期模式 | `Default/None/Sliding/Absolute` |
| `CacheUpdateMode` | 更新模式 | `None/Up` |
| `CacheTelemetry` | ActivitySource/Meter 入口 | 名称 `Nerosoft.Euonia.Caching` |

### 配置选项（代码配置）

核心包没有 `IConfiguration` 读取点，也没有配置节；下表是代码中的默认值，可直接在 `CacheManagerConfiguration` 或构建器上覆盖。

| 选项 | 默认值 | 说明 |
|---|---|---|
| `Name` | 随机 GUID | 缓存管理器名称 |
| `UpdateMode` | `Up` | 从下层命中后是否回填上层 |
| `MaxRetries` | `50` | `GetOrAdd`/更新冲突的最大尝试次数 |
| `RetryTimeout` | `100` ms | 每次重试前等待 |
| `BackplaneType` | `null` | 由提供程序包设置 |
| `CacheHandleConfigurations` | 空 | 通过 `With*Handle` 追加 |

> 不要为 `Euonia.Caching` 编写 `appsettings` 节；Redis 连接串可由宿主从 `ConnectionStrings` 读取后传入 `WithRedisConfiguration`。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、核心类型与行为边界](../../../Source/Euonia.Caching/README.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/API.md)
- [Euonia.Caching.Redis 用户手册](../Euonia.Caching.Redis/API.md)
- [Euonia.Caching.Runtime 用户手册](../Euonia.Caching.Runtime/API.md)
- [根 README](../../../README.md)
