# Euonia.Caching.Runtime API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

用 `System.Runtime.Caching.MemoryCache` 作为 Euonia 缓存提供程序，适合已有该配置模型（内存上限、物理内存百分比、轮询间隔）的宿主。本页用报表缓存演示命名实例、限额与失效。

- **NuGet 包**：`Euonia.Caching.Runtime`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Runtime`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching.Runtime/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `RuntimeCacheService(IOptions<RuntimeCacheOptions>)` | `ICacheService` 实现 | 不接收 `IKeyTransformer` |
| `RuntimeCacheOptions.InstanceName` | 命名缓存实例 | `"default"` |
| `RuntimeCacheOptions.IsBackplaneSource` | 背板源标记 | `false`（当前未读取） |
| `RuntimeCacheOptions.MaxRetries` | 更新冲突重试 | `5` |
| `RuntimeCacheOptions.RetryTimeout` | 重试毫秒数 | `3000` |
| `RuntimeCacheOptions.UpdateMode` | 更新模式 | `Up` |
| `RuntimeCacheOptions.KeyPrefix` | 键前缀 | `null`；服务层拼 `.Cache.` |
| `RuntimeCacheOptions.Expires` | 默认绝对过期 | `null` |
| `RuntimeCacheOptions.CacheMemoryLimitMegabytes` | 缓存内存上限 | `0`（运行时启发式） |
| `RuntimeCacheOptions.PhysicalMemoryLimitPercentage` | 物理内存百分比上限 | `0`（启发式） |
| `RuntimeCacheOptions.PollingInterval` | 内存压力轮询间隔 | `2 分钟` |
| `RuntimeCacheOptions.AsNameValueCollection()` | 转为 `MemoryCache` 构造参数 | 含三个限额键 |
| `WithRuntimeCacheHandle(part[, instanceName[, options]])` | 加入运行时句柄 | 默认实例名 `default` |
| `RuntimeCacheHandle<TValue>.Clear/ClearRegion` | 清空实例/区域 | 基于变更监视器 |
| `ICacheService.GetOrAdd<TValue>(key, factory, timeout)` | 读穿写入 | `timeout` 可为空 |
| `ICacheService.Remove<TValue>(key)` | 按键删除 | 与泛型参数无关 |

### 选项（代码配置）

本包没有配置节；`RuntimeCacheOptions` 通过 `services.Configure<RuntimeCacheOptions>(...)` 或 `WithRuntimeCacheHandle(..., options)` 设置。

> 默认实例的限额只能由 `app.config`/`web.config` 的历史配置管理；不要为本包新增 `appsettings` 节。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Caching.Runtime/README.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/API.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/API.md)
- [根 README](../../../README.md)
