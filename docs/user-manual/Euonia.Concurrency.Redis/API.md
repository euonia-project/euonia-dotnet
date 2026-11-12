# Euonia.Concurrency.Redis API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

用 Redis 实现跨节点分布式锁与信号量，支持自动续期与最短有效时间。本页用“订单发货互斥 + 报表导出限流”演示配置、模块、并发验证与故障定位。

- **NuGet 包**：`Euonia.Concurrency.Redis`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.Redis`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Concurrency.Redis/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `RedisLockModule` | 注册 `ILockFactory` | 依赖内部同步模块 |
| `RedisSemaphoreModule` | 注册 `ISemaphoreFactory` | 与锁模块共享单例 |
| `RedisSynchronizationFactory(IDatabase, Action<...>?)` | 同时实现锁/信号量工厂 | database 必填 |
| `RedisSynchronizationFactory(IEnumerable<IDatabase>, Action<...>?)` | 多库构造 | 信号量只用第一库 |
| `RedisSynchronizationFactory.Create(RedisKey)` | 创建锁 | 键不能为默认值 |
| `RedisSynchronizationFactory.Create(RedisKey, int maxCount)` | 创建信号量 | `maxCount >= 1` |
| `RedisLockProvider(RedisKey, IDatabase, Action<...>?)` | 单库锁 | 键与库必填 |
| `RedisLockProvider(RedisKey, IEnumerable<IDatabase>, Action<...>?)` | 多库锁 | 列表不能为空/含 null |
| `RedisSemaphoreProvider(RedisKey, int, IDatabase, Action<...>?)` | 信号量 | `maxCount` 必填 |
| `RedisSynchronizationOptionsBuilder.Expiry(TimeSpan)` | 锁过期时间 | 默认 30s，最小 100ms |
| `RedisSynchronizationOptionsBuilder.MinValidityTime(TimeSpan)` | 获取后最小有效时间 | 默认 90% × Expiry |
| `RedisSynchronizationOptionsBuilder.ExtensionCadence(TimeSpan)` | 续期节奏 | 默认 `MinValidity / 3`，必须更小 |
| `RedisSynchronizationOptionsBuilder.BusyWaitSleepTime(min, max)` | 忙等待区间 | 默认 `[10ms, 800ms]` |
| `RedisSynchronizationHandle` | 锁/票据句柄 | `HandleCancellationToken` 监视丢失 |
| `RedisLockProvider.Acquire/TryAcquire` | 同步获取 | 失败返回 `null`/超时抛异常 |
| `RedisSemaphoreProvider.MaxCount` | 最大并发票据 | 构造时指定 |
| `ISemaphoreFactory.Create(name, maxCount)` | 按名称创建 | 名称必填 |

### 配置项

| 配置键 | 默认值 | 说明 |
|---|---|---|
| `RedisSynchronization:ConnectionString` | 必填 | StackExchange.Redis 连接串 |
| `RedisSynchronization:Database` | `-1` | 逻辑库；`-1` 使用服务端默认 |
| `RedisSynchronization:Expiry` | `00:00:30` | 锁/票据过期时间，最小 100ms |
| `RedisSynchronization:MinValidityTime` | `90% × Expiry` | 必须小于 `Expiry` |
| `RedisSynchronization:ExtensionCadence` | `MinValidity / 3` | 必须小于 `MinValidityTime` |
| `RedisSynchronization:MinBusyWaitSleepTime` | `10ms` | 忙等待最小间隔 |
| `RedisSynchronization:MaxBusyWaitSleepTime` | `800ms` | 忙等待最大间隔 |

```json
{
  "RedisSynchronization": {
    "ConnectionString": "localhost:6379",
    "Database": 0
  }
}
```

```bash
RedisSynchronization__ConnectionString=localhost:6379   # .NET 原生
REDISSYNCHRONIZATION_CONNECTIONSTRING=localhost:6379    # 仓库 alias
```

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Concurrency.Redis/README.md)
- [Euonia.Concurrency 用户手册](../Euonia.Concurrency/API.md)
- [Euonia.Concurrency.Azure 用户手册](../Euonia.Concurrency.Azure/API.md)
- [Euonia.Caching.Redis 用户手册](../Euonia.Caching.Redis/API.md)
- [根 README](../../../README.md)
