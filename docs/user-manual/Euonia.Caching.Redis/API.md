# Euonia.Caching.Redis API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

把缓存放到 Redis，让多个实例共享同一份数据，并用 Redis 发布/订阅背板广播失效。本页用“购物车缓存”演示连接配置、读穿、按前缀清理与运行验证。

- **NuGet 包**：`Euonia.Caching.Redis`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Redis`（`RedisCacheService` 在 `Nerosoft.Euonia.Caching`）
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Caching.Redis/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `WithRedisConfiguration(part, key, connectionString, database, enableKeyspaceNotifications)` | 注册 Redis 连接配置 | `database` 默认 0；通知默认关闭 |
| `WithRedisConfiguration(part, key, Action<RedisConfigurationBuilder>)` | 用构建器配置连接 | 可链式设置端点/密码/TLS |
| `WithRedisConfiguration(part, key, IConnectionMultiplexer, ...)` | 复用现有连接 | 连接按字符串登记 |
| `WithRedisCacheHandle(part, key, isBackplaneSource)` | 加入 Redis 句柄 | `isBackplaneSource` 默认 `true` |
| `WithRedisBackplane(part, key[, channel])` | 启用发布/订阅背板 | 需要已注册配置 |
| `RedisCacheOptions.ConnectionString` | `RedisCacheService` 连接串 | 必填 |
| `RedisCacheOptions.Database` | 逻辑数据库 | `0` |
| `RedisCacheOptions.MaxRetries` | 更新冲突重试 | `5` |
| `RedisCacheOptions.RetryTimeout` | 重试毫秒数 | `3000` |
| `RedisCacheOptions.UpdateMode` | 更新模式 | `Up` |
| `RedisCacheOptions.KeyPrefix` | 服务层键前缀 | `null` |
| `RedisCacheOptions.Expires` | 默认绝对过期 | `null` |
| `RedisCacheService(IOptions<RedisCacheOptions>, IKeyTransformer?)` | `ICacheService` 实现 | 自动装配句柄与背板 |
| `RedisConfigurationBuilder.WithAllowAdmin()` | 允许 `Clear` 等管理操作 | 默认关闭 |
| `RedisConfigurationBuilder.EnableKeyspaceEvents()` | 接收逐出/过期事件 | 服务端需 `notify-keyspace-events Exe` |
| `RedisConfigurationBuilder.WithEndpoint/WithPassword/WithSsl` | 结构化连接配置 | 至少一个端点 |
| `RedisCacheHandle<TValue>.RemoveByPrefixAsync` | 按前缀删除 | 使用 `SCAN` 分批 |
| `TypeCache.RegisterResolveType(Func<string, Type?>)` | 自定义类型解析 | 用于旧缓存键兼容 |

### 选项（代码配置）

本包不读取配置节。`RedisCacheService` 的选项通过 `services.Configure<RedisCacheOptions>(...)` 设置；多级缓存通过 `WithRedisConfiguration`/`WithRedisCacheHandle` 的代码参数设置。连接串可由宿主从 `ConnectionStrings` 读取。

> 不要为 `Euonia.Caching.Redis` 编写 `appsettings` 节。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Caching.Redis/README.md)
- [Euonia.Caching 用户手册](../Euonia.Caching/API.md)
- [Euonia.Caching.Memory 用户手册](../Euonia.Caching.Memory/API.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/API.md)
- [根 README](../../../README.md)
