# Euonia.Concurrency.FileSystem

Euonia 分布式同步的文件系统提供程序：以本地/共享目录中的锁文件实现跨进程互斥。适用于单机多进程或已挂载共享卷的场景，无需额外基础设施。

File system provider for Euonia distributed synchronization: cross-process mutual exclusion using lock files in a local or shared directory. Suitable for multi-process single-host deployments or mounted shared volumes, with no extra infrastructure.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.FileSystem`
- **NuGet 包**：`Euonia.Concurrency.FileSystem`

## 安装

```bash
dotnet add package Euonia.Concurrency.FileSystem
```

## 主要功能

- **文件锁**：`FileSystemLockModule` 注册 `ILockFactory`。
- **目录可配置**：通过 `FileSynchronizationOptions.Directory` 指定锁文件目录。
- **跨进程互斥**：锁由文件句柄持有，进程退出后自动释放。
- **轻量无依赖**：不需要 Redis、ZooKeeper 或数据库。

## 快速开始

配置目录（模块本身不绑定配置节，需要显式注册选项）：

```csharp
using Nerosoft.Euonia.Concurrency.FileSystem;

services.Configure<FileSynchronizationOptions>(
    configuration.GetSection("FileSynchronization"));
```

```json
{
  "FileSynchronization": {
    "Directory": "/var/lib/myapp/locks"
  }
}
```

声明模块依赖：

```csharp
using Nerosoft.Euonia.Concurrency.FileSystem;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(FileSystemLockModule))]
public sealed class WorkerModule : ModuleContextBase
{
}
```

使用锁：

```csharp
public sealed class SchedulerService(ILockFactory lockFactory)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("nightly-job");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);
        if (handle is null)
        {
            return; // 其他进程正在执行
        }

        // 独占执行
    }
}
```

## 配置项

| 选项 | 说明 |
|------|------|
| `FileSynchronizationOptions.Directory` | 锁文件所在目录，必须存在且可写 |

## 核心类型

| 类型 | 作用 |
|------|------|
| `FileSystemLockModule` | 注册 `ILockFactory` |
| `FileSynchronizationFactory` | 基于 `DirectoryInfo` 创建锁提供程序 |
| `FileLockProvider` / `FileSynchronizationHandle` | 文件锁实现与句柄 |
| `FileSynchronizationOptions` | 同步目录配置 |

> 注意：文件系统实现仅提供锁（`ILockFactory`），不提供信号量；且共享目录的原子性取决于底层文件系统，跨主机使用时请确认网络文件系统语义。

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[Lock]` / `[DistributedLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
