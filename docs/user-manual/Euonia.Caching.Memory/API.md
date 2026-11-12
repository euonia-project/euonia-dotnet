# Euonia.Caching.Memory API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

用 `Microsoft.Extensions.Caching.Memory` 提供进程内缓存，可作为多级缓存的 L1，也可通过 `ICacheService` 直接注入业务服务。本页用“用户资料读穿缓存 + 更新失效”演示完整接线。

- **NuGet 包**：`Euonia.Caching.Memory`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Memory`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching.Memory/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `MemoryCacheService(IOptions<MemoryCacheOptions>, IKeyTransformer?)` | `ICacheService` 实现 | 选项应通过 `Configure` 注册 |
| `ICacheService.Get<TValue>/TryGet<TValue>` | 读取缓存 | 未命中返回默认值/`false` |
| `ICacheService.GetOrAdd<TValue>(key, factory, timeout)` | 读穿并写入 | `timeout` 为空时使用句柄默认过期 |
| `ICacheService.AddOrUpdate<TValue>(key, value, timeout)` | 添加或覆盖 | 支持 `DateTime` 绝对时间重载 |
| `ICacheService.Remove<TValue>(key)` | 按键删除 | 与泛型参数无关 |
| `ICacheService.GetOrAddAsync/AddOrUpdateAsync` | 异步版本 | 支持 `CancellationToken` |
| `WithMemoryCacheHandle(part)` | 加入内存句柄 | 实例名随机 GUID |
| `WithMemoryCacheHandle(part, instanceName)` | 指定实例名 | 名称必填 |
| `WithMemoryCacheHandle(part, instanceName, options)` | 带选项 | `MemoryCacheOptions` |
| `MemoryCacheHandle<TValue>.RemoveByPrefixAsync` | 按前缀删除 | 仅删除登记过的键 |
| `MemoryCacheOptions.InstanceName` | 实例/句柄名 | `"default"` |
| `MemoryCacheOptions.MaxRetries` | 更新冲突重试 | `5` |
| `MemoryCacheOptions.RetryTimeout` | 重试毫秒数 | `3000` |
| `MemoryCacheOptions.UpdateMode` | 更新模式 | `Up` |
| `MemoryCacheOptions.KeyPrefix` | 键前缀 | `null`；服务层拼 `.Cache.` |
| `MemoryCacheOptions.Expires` | 默认绝对过期 | `null` |

### 选项（代码配置）

本包没有配置节，全部通过 `services.Configure<MemoryCacheOptions>(...)` 或 `WithMemoryCacheHandle(..., options)` 设置；`ExpirationScanFrequency` 等基类选项继承自 `Microsoft.Extensions.Caching.Memory.MemoryCacheOptions`；本提供程序的条目不设置 `Size`，启用 `SizeLimit` 前需自行评估兼容性。

> 不要为本包编写 `appsettings` 节。需要从配置读取前缀时，由宿主读出后赋给 `MemoryCacheOptions.KeyPrefix`。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Caching.Memory/README.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/API.md)
- [Euonia.Caching.Redis 用户手册](../Euonia.Caching.Redis/API.md)
- [根 README](../../../README.md)
