# Euonia.Caching.Runtime

Euonia 缓存的 `System.Runtime.Caching` 提供程序：把 `MemoryCache` 封装为缓存句柄，适合沿用该配置模型（内存上限、物理内存百分比、轮询间隔）的场景。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Runtime`
- **NuGet 包**：`Euonia.Caching.Runtime`
- **依赖**：`Euonia.Caching`、`System.Runtime.Caching`、`Microsoft.Extensions.Options`

## 安装

```bash
dotnet add package Euonia.Caching.Runtime
```

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
using Microsoft.Extensions.DependencyInjection;
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

```csharp
public sealed class ReportService(ICacheService cache)
{
    public Report Get(string id)
        => cache.GetOrAdd($"report:{id}", () => BuildReport(id), TimeSpan.FromMinutes(30));
}
```

## 主要功能

- **缓存句柄**：`WithRuntimeCacheHandle(...)` 把 `System.Runtime.Caching.MemoryCache` 加入缓存链。
- **`ICacheService` 实现**：`RuntimeCacheService` 提供与内存/Redis 提供程序一致的读写、`GetOrAdd` 与 `AddOrUpdate` API。
- **资源限制**：非默认实例名下，`CacheMemoryLimitMegabytes`、`PhysicalMemoryLimitPercentage` 与 `PollingInterval` 直接传入 `MemoryCache` 构造。
- **区域模拟**：通过缓存项变更监视器（`ChangeMonitor`）实现 `ClearRegion` 与实例级清空。
- **键前缀与过期**：`KeyPrefix`、`Expires` 与统一的更新/重试策略。

## 核心类型

| 类型 | 作用 |
|------|------|
| `RuntimeCacheHandle<TValue>` | 基于 `System.Runtime.Caching` 的缓存句柄 |
| `RuntimeCacheService` | `ICacheService` 实现 |
| `RuntimeCacheOptions` | 提供程序选项 |
| `WithRuntimeCacheHandle(...)` | 将运行时缓存句柄加入配置的扩展方法 |
| `RuntimeCacheOptions.AsNameValueCollection()` | 把内存限制/轮询参数转成 `MemoryCache` 构造参数 |

## 选项（代码配置）

本包不读取 `IConfiguration`，没有配置节；`RuntimeCacheOptions` 通过 `services.Configure<RuntimeCacheOptions>(...)` 或 `WithRuntimeCacheHandle(..., options)` 在代码中设置。

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `InstanceName` | `"default"` | 缓存实例名；默认实例使用 `MemoryCache.Default` |
| `IsBackplaneSource` | `false` | 是否作为背板数据源；当前实现未读取 |
| `MaxRetries` | `5` | 更新冲突重试次数 |
| `RetryTimeout` | `3000` | 重试超时（毫秒） |
| `UpdateMode` | `Up` | 更新模式（`None`/`Up`） |
| `KeyPrefix` | `null` | 键前缀；服务层再拼 `.Cache.` |
| `Expires` | `null` | 默认绝对过期时间；仅在设置时以 `Absolute` 应用 |
| `CacheMemoryLimitMegabytes` | `0` | 缓存内存上限（MB）；`0` 表示使用运行时启发式 |
| `PhysicalMemoryLimitPercentage` | `0` | 物理内存占用上限百分比；`0` 表示启发式 |
| `PollingInterval` | `2 分钟` | 内存压力检查间隔 |

> 不要为本包编写 `appsettings` 配置节。`System.Runtime.Caching` 的历史配置位于 `app.config`/`web.config`，与本包选项无关。

## 使用示例

非默认实例名才能应用代码中的内存限制：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Runtime;

var cache = CacheFactory.Build<string>("app", settings =>
{
    settings.WithUpdateMode(CacheUpdateMode.Up)
            .WithMaxRetries(3)
            .WithRuntimeCacheHandle("app-memory", new RuntimeCacheOptions
            {
                InstanceName = "app-memory",
                CacheMemoryLimitMegabytes = 512,
                PhysicalMemoryLimitPercentage = 20,
                PollingInterval = TimeSpan.FromSeconds(30),
                Expires = TimeSpan.FromMinutes(10)
            });
});

services.AddSingleton(cache);
```

服务级注册与依赖注入：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Runtime;

services.Configure<RuntimeCacheOptions>(options =>
{
    options.InstanceName = "app";
    options.KeyPrefix = "app";
});

services.AddSingleton<ICacheService, RuntimeCacheService>();
```

## 注意事项 / 限制

- **默认实例不能代码配置**：`InstanceName = "default"` 时句柄使用 `MemoryCache.Default`，`CacheMemoryLimitMegabytes`/`PhysicalMemoryLimitPercentage`/`PollingInterval` 不会生效；需要限制时请使用非默认实例名。
- **进程内、不跨节点**：与所有 `MemoryCache` 实现一样，多实例部署不共享缓存。
- **不支持前缀删除**：运行时句柄未重写 `RemoveByPrefixAsync`，通过 `ICacheManager<TValue>` 调用时返回 `0`（不删除任何键）。
- **没有租户键前缀**：`RuntimeCacheService` 构造只接收 `IOptions<RuntimeCacheOptions>`，不会注入 `IKeyTransformer`；多租户键前缀需由调用方自行拼装。
- **滑动过期依赖访问时重置**：句柄在读取滑动过期项时会重新 `Set` 以续期，但 `System.Runtime.Caching` 默认的过期扫描粒度较粗，细微时间窗口不要依赖精确过期。
- **区域是模拟实现**：`ClearRegion` 通过变更监视器移除区域令牌，与真正支持区域的提供程序语义不完全相同。

## 相关包

- [`Euonia.Caching`](../Euonia.Caching/README.md) —— 多级缓存框架
- [`Euonia.Caching.Memory`](../Euonia.Caching.Memory/README.md) —— `Microsoft.Extensions.Caching.Memory` 提供程序
- [`Euonia.Caching.Redis`](../Euonia.Caching.Redis/README.md) —— Redis 分布式提供程序

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。

## 常见问题与故障排查

### 设置了 CacheMemoryLimitMegabytes 但内存限制没有生效
原因：`InstanceName = "default"` 时句柄使用全局 `MemoryCache.Default`，`RuntimeCacheManager` 明确不传代码选项（默认实例只能由历史配置管理）。
解决：改用非默认 `InstanceName`（例如 `app-memory`），代码选项才会传给新建的 `MemoryCache`。

### RemoveByPrefixAsync 总是返回 0
原因：`RuntimeCacheHandle` 没有重写前缀删除，基类默认实现返回 0；`ICacheService` 本身也不暴露该方法。
解决：维护键索引并逐个 `Remove`，或改用支持前缀删除的 `Euonia.Caching.Memory`/`Euonia.Caching.Redis`。

### 多租户键没有自动加租户前缀
原因：`RuntimeCacheService` 的构造函数只接收 `IOptions<RuntimeCacheOptions>`，不会注入 `IKeyTransformer`。
解决：调用方自行拼租户前缀，或改用会注入 `IKeyTransformer` 的 Memory/Redis 服务实现。

### 多个服务使用默认实例时键互相覆盖
原因：默认实例共享全局 `MemoryCache.Default`，所有使用 `"default"` 的代码看到同一键空间。
解决：为每个逻辑缓存分配唯一 `InstanceName`，并统一 `KeyPrefix`；不要依赖默认实例做隔离。

### 滑动过期时间看起来不精确
原因：句柄在访问时重新 `Set` 续期，但 `System.Runtime.Caching` 的过期扫描粒度较粗。
解决：不要依赖秒级精确过期；对强时效数据使用绝对过期或缩短 TTL，并在读取时自行校验时间戳。

## 最佳实践

- **非默认实例 + 显式上限**：生产环境使用唯一 `InstanceName` 并设置 `CacheMemoryLimitMegabytes`/`PhysicalMemoryLimitPercentage`/`PollingInterval`。
- **只用于进程内场景**：跨节点共享请选 Redis；Runtime 更适合兼容既有 `System.Runtime.Caching` 配置的宿主。
- **TTL 优先**：所有键设置 `Expires` 或写入时 `timeout`，避免依赖粗粒度扫描回收。
- **前缀删除需自建索引**：需要按租户清理时记录键集合，或切换提供程序；不要假设基类支持。
- **单例注册并释放**：`RuntimeCacheService` 注册为单例；非默认实例的 `MemoryCache` 由句柄释放，避免重复创建。

## 版本与兼容

- **目标框架**：`net10.0`、`net11.0`；仓库统一包版本 `2026.5.0`（`project.props`）。
- **关键依赖**：`System.Runtime.Caching`、`Microsoft.Extensions.Options`、`Euonia.Caching`。
- **已知边界**：默认实例不能通过代码配置限制；无租户键转换与前缀删除；进程内、不跨节点。
- **升级注意**：缓存不持久化，升级重启即清空；命名实例的历史 `app.config`/`web.config` 配置不会自动迁移到 `RuntimeCacheOptions`，升级时需显式迁移限额与轮询参数。
