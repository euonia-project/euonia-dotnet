# Euonia.Concurrency.FileSystem

Euonia 分布式同步的文件系统提供程序：以目录中的锁文件实现跨进程互斥，适用于单机多进程或已挂载共享卷的场景，无需额外基础设施。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.FileSystem`
- **NuGet 包**：`Euonia.Concurrency.FileSystem`
- **依赖**：`Euonia.Concurrency`、`Euonia.Modularity`
- **配置节**：`FileSynchronization`

## 安装

```bash
dotnet add package Euonia.Concurrency.FileSystem
```

## 快速开始

配置锁目录（`FileSystemLockModule` 会自动绑定该配置节）：

```json
{
  "FileSynchronization": {
    "Directory": "/var/lib/myapp/locks"
  }
}
```

环境变量（两种写法等价，仓库 alias 见 `Euonia.Modularity`）：

```bash
export FileSynchronization__Directory=/var/lib/myapp/locks   # .NET 原生：双下划线表示层级
export FILESYNCHRONIZATION_DIRECTORY=/var/lib/myapp/locks    # 仓库 alias：大写单下划线
```

也可在代码中覆盖（显式 `Configure` 优先于配置节）：

```csharp
using Nerosoft.Euonia.Concurrency.FileSystem;

services.Configure<FileSynchronizationOptions>(options =>
{
    options.Directory = "/var/lib/myapp/locks";
});
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

## 主要功能

- **文件锁**：`FileSystemLockModule` 注册 `ILockFactory`，由 `FileSynchronizationFactory` 创建 `FileLockProvider`。
- **自动建目录**：获取锁时若目录不存在会自动创建；创建目录失败时抛出带目录信息的 `InvalidOperationException`。
- **跨进程互斥**：以 `FileShare.None` 独占打开锁文件；进程退出时操作系统关闭句柄并删除文件。
- **可移植文件名**：锁名称被转换为“安全前缀 + Base32 哈希 + `.lock`”，避免特殊字符与大小写差异。
- **轻量无依赖**：不需要 Redis、ZooKeeper 或数据库。

## 核心类型

| 类型 | 作用 |
|------|------|
| `FileSystemLockModule` | 注册 `ILockFactory` 的模块 |
| `FileSynchronizationFactory` | 基于 `DirectoryInfo` 创建锁提供程序 |
| `FileLockProvider` / `FileSynchronizationHandle` | 文件锁实现与句柄 |
| `FileSynchronizationOptions` | 同步目录配置（`Directory`） |

## 配置项

| 配置键 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `FileSynchronization:Directory` | `string` | 无（必填） | 锁文件目录；不存在时在获取锁时创建 |

```json
{
  "FileSynchronization": {
    "Directory": "/var/lib/myapp/locks"
  }
}
```

```bash
FileSynchronization__Directory=/var/lib/myapp/locks    # .NET 原生
FILESYNCHRONIZATION_DIRECTORY=/var/lib/myapp/locks     # 仓库 alias
```

## 注意事项 / 限制

- **未配置目录会失败**：`Directory` 为空时，创建 `ILockFactory` 会抛 `InvalidOperationException`，错误信息中给出配置键与环境变量名。
- **仅提供锁**：本包不实现 `ISemaphoreFactory`；需要信号量请使用 Redis 或 ZooKeeper 提供程序。
- **跨主机依赖文件系统语义**：`FileShare.None` 在本地文件系统可靠，但在 NFS/SMB 等网络卷上是否具备原子互斥取决于服务端实现，跨主机强互斥应先验证。
- **忙等待参数不可配置**：竞争时固定以 50ms–1s 的随机间隔重试；锁文件被长期占用时等待方会持续轮询。
- **锁文件是临时的**：释放（或进程退出）时文件被删除；不要依赖锁文件中的内容，它只是互斥标记。

## 相关包

- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 同步抽象
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块、配置绑定与环境变量 alias
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[DistributedLock]` / `[SemaphoreLock]` 拦截器

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
