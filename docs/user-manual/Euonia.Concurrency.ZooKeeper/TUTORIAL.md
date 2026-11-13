# Euonia.Concurrency.ZooKeeper 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

基于 ZooKeeper 临时顺序节点的分布式锁与信号量：崩溃自动释放，配置只覆盖连接与超时。

| 项 | 值 |
|---|---|
| NuGet 包 | `Euonia.Concurrency.ZooKeeper` |
| 目标框架 | `net10.0`、`net11.0` |
| 根命名空间 | `Nerosoft.Euonia.Concurrency.ZooKeeper` |
| 依赖 | `Euonia.Concurrency`（同步抽象）+ ZooKeeperNetEx `3.4.12.4` |
| 完整参考 | [`Source/Euonia.Concurrency.ZooKeeper/README.md`](../../../Source/Euonia.Concurrency.ZooKeeper/README.md) |

## 教程

### 场景

三个实例同时部署的夜间对账任务：同一时刻只允许一个实例执行（分布式锁）；导出任务最多 3 个并发（分布式信号量）。任务持锁期间若会话过期，要立刻感知并放弃本轮。

### 前置条件

- 可访问的 ZooKeeper（本地可用 `docker run --rm -p 2181:2181 zookeeper` 起一个）。
- 应用使用 `Euonia.Modularity` 模块系统；不使用模块时按“手工接线”一节直接注册单例。

### 1. 安装

```bash
dotnet add package Euonia.Concurrency.ZooKeeper
```

### 2. 配置

`appsettings.json`：

```json
{
  "ZooKeeperSynchronization": {
    "ConnectionString": "localhost:2181",
    "SessionTimeout": 20000,
    "ConnectTimeout": 15000
  }
}
```

```bash
# .NET 原生：双下划线表示层级
ZooKeeperSynchronization__ConnectionString=localhost:2181
ZooKeeperSynchronization__SessionTimeout=20000

# 仓库 alias：大写单下划线（Euonia.Modularity）
ZOOKEEPERSYNCHRONIZATION_CONNECTIONSTRING=localhost:2181
ZOOKEEPERSYNCHRONIZATION_SESSIONTIMEOUT=20000
```

### 3. 接线：模块方式

```csharp
using Nerosoft.Euonia.Concurrency.ZooKeeper;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(ZooKeeperLockModule))]          // 需要信号量再加 ZooKeeperSemaphoreModule
public sealed class ClusterModule : ModuleContextBase
{
}
```

模块会从 `ZooKeeperSynchronization:ConnectionString` 读连接串，并注册单例 `ILockFactory` / `ISemaphoreFactory`（两者是同一个 `ZooKeeperSynchronizationFactory` 实例）。

不使用模块系统时手工接线，同时把认证信息一起传进去：

```csharp
using System.Text;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.ZooKeeper;

services.AddSingleton<ILockFactory>(_ => new ZooKeeperSynchronizationFactory(
    new ZooKeeperPath("/euonia/locks"),
    builder.Configuration["ZooKeeperSynchronization:ConnectionString"],
    builder => builder
        .SessionTimeout(TimeSpan.FromSeconds(30))
        .ConnectTimeout(TimeSpan.FromSeconds(10))
        .AddAuthInfo("digest", Encoding.UTF8.GetBytes("user:password"))));
```

### 4. 互斥执行 + 丢锁感知

```csharp
using Nerosoft.Euonia.Concurrency;

public sealed class NightlyReconciliationService(ILockFactory locks, ILogger<NightlyReconciliationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 扩展方法：Create + TryAcquire 的简写；超时返回 null。
            await using var handle = await locks.TryAcquireAsync(
                "euonia:nightly:reconcile", TimeSpan.FromSeconds(5), stoppingToken);

            if (handle is null)
            {
                logger.LogInformation("另一个实例持有对账锁，稍后重试。");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            // 会话过期后锁会消失：用它感知“持锁期间丢锁”。
            await using var registration = handle.HandleCancellationToken.Register(
                () => logger.LogWarning("对账锁已丢失，本轮结果作废。"));

            await ReconcileAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private static Task ReconcileAsync(CancellationToken cancellationToken) => Task.Delay(10, cancellationToken);
}
```

### 5. 限流：信号量

```csharp
public sealed class ExportWorker(ISemaphoreFactory semaphores)
{
    public async Task ExportAsync(string id, CancellationToken cancellationToken)
    {
        await using var slot = await semaphores.TryAcquireAsync(
            "euonia:export", 3, TimeSpan.FromSeconds(30), cancellationToken);

        if (slot is null)
        {
            throw new TimeoutException($"导出 {id} 等待并发槽位超时。");
        }

        await Task.Delay(100, cancellationToken);
    }
}
```

### 6. 验证

```bash
# 两个实例同时启动，只有一个进入临界区
dotnet run --urls http://localhost:5001 &
dotnet run --urls http://localhost:5002 &
sleep 5

# 观察节点：锁节点形如 lock-0000000001
docker exec -it <zookeeper-container> zkCli.sh ls /euonia/locks
```

预期：只有一个实例打印“开始对账”；另一个打印“另一个实例持有对账锁”。`Kill` 掉持锁进程后，临时节点随会话消失，另一个实例在下一轮（或会话超时后）拿到锁。

### 常见失败点

- **首次取锁抛 `ArgumentNullException (connectionString)`**：配置节缺失，模块把 `null` 传给工厂（`ZooKeeperSynchronizationModule.cs:19,23-27`）。
- **`TimeoutException: Timed out connecting ...`**：地址不可达或超过 `ConnectTimeout`（`ZooKeeperConnection.cs:137-141`），先用 `zkCli` 验证连通。
- **等待中抛 `Lost connection to ZooKeeper`**：会话过期（`ZooKeeperSynchronizationHelper.cs:129-131`），把临界区当可重试。
- **`TryAcquire` 返回 `null` 不是异常**：就是等待超时（`ZooKeeperSynchronizationHelper.cs:138-141`），按业务决定重试或降级。
- **节点建在根 `/` 下与其他应用混用**：改用 `new ZooKeeperPath("/euonia/locks")` 建子目录。

## 相关文档

- [API 参考](API.md)
- [`Euonia.Concurrency.ZooKeeper` README](../../../Source/Euonia.Concurrency.ZooKeeper/README.md) —— 完整限制与节点语义
- [`Source/Euonia.Concurrency/README.md`](../../../Source/Euonia.Concurrency/README.md) —— `ILockProvider` / `ISynchronizationHandle` 抽象
- [`Source/Euonia.Modularity/README.md`](../../../Source/Euonia.Modularity/README.md) —— 模块与 `[DependsOn]`
- [Tenancy 手册](../Euonia.Tenancy/TUTORIAL.md) —— 多实例部署时的租户上下文注意事项
- [根 README](../../../README.md)
