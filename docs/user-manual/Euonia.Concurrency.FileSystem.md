# Euonia.Concurrency.FileSystem 用户手册

用目录中的锁文件实现跨进程互斥，适合单机多进程、容器共享卷或已确认语义的共享文件系统。本页用“夜间订单汇总只跑一次”演示配置、模块与双进程验证。

- **NuGet 包**：`Euonia.Concurrency.FileSystem`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.FileSystem`
- **完整安装与选项**：[项目 README](../../Source/Euonia.Concurrency.FileSystem/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

同一台机器上运行两个 Worker 副本，都会在收到触发时执行夜间汇总。任何时刻只允许一个副本执行，另一个立即返回忙碌，而不是排队。锁文件放在固定目录，进程退出后由操作系统清理。

### 前置条件

- .NET 10 或 11 SDK。
- 一个运行账号可写的目录（示例 `/var/lib/orders/locks`）。
- 安装 `Euonia.Concurrency.FileSystem` 与 `Euonia.Modularity`。

### 1. 安装

```bash
dotnet add package Euonia.Concurrency.FileSystem
dotnet add package Euonia.Modularity
```

### 2. 配置锁目录

`appsettings.json`：

```json
{
  "FileSynchronization": {
    "Directory": "/var/lib/orders/locks"
  }
}
```

两种等价的环境变量写法：

```bash
export FileSynchronization__Directory=/var/lib/orders/locks   # .NET 原生
export FILESYNCHRONIZATION_DIRECTORY=/var/lib/orders/locks    # 仓库 alias
```

### 3. 模块与服务

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Concurrency;
using Nerosoft.Euonia.Concurrency.FileSystem;
using Nerosoft.Euonia.Modularity;

var builder = WebApplication.CreateBuilder(args);
var application = builder.Services.AddModularityApplication<WorkerModule>(builder.Configuration);
builder.Services.AddSingleton<NightlyJobService>();

var app = builder.Build();
application.Initialize(app.Services);

app.MapPost("/nightly/run", async (NightlyJobService service, CancellationToken ct)
    => await service.RunAsync(ct)
        ? Results.Accepted()
        : Results.Conflict(new { message = "Another process is running." }));

app.Run();

[DependsOn(typeof(FileSystemLockModule))]
public sealed class WorkerModule : ModuleContextBase
{
}

public sealed class NightlyJobService(ILockFactory lockFactory, ILogger<NightlyJobService> logger)
{
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var locker = lockFactory.Create("nightly-orders");
        await using var handle = await locker.TryAcquireAsync(TimeSpan.Zero, cancellationToken);
        if (handle is null)
        {
            logger.LogInformation("Nightly job is already running.");
            return false;
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        return true;
    }
}
```

### 4. 运行两个进程并验证

```bash
mkdir -p /var/lib/orders/locks

dotnet build
dotnet run --no-build --urls http://localhost:5081 &
dotnet run --no-build --urls http://localhost:5082 &
```

同时触发两个实例：

```bash
curl -s -o /dev/null -w "5081:%{http_code}\n" -X POST http://localhost:5081/nightly/run &
curl -s -o /dev/null -w "5082:%{http_code}\n" -X POST http://localhost:5082/nightly/run &
wait
```

应一个返回 `202`、另一个返回 `409`。执行期间查看目录，会看到临时锁文件；释放后文件消失：

```bash
ls -la /var/lib/orders/locks
```

### 常见失败点

- **`InvalidOperationException` 提示目录未配置**：设置 `FileSynchronization:Directory` 或 `FileSynchronization__Directory`；错误信息会同时给出两者。
- **`UnauthorizedAccessException`**：运行账号对目录没有写权限，或目录只读；容器中请挂载可写卷。
- **提示锁文件已是目录**：锁文件路径被同名目录占用；换专用空目录，不要手工创建同名目录。
- **跨主机互斥不稳定**：NFS/SMB 的 `FileShare.None` 语义不一定等价本地磁盘；跨主机请改 Redis/ZooKeeper/Azure。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `FileSystemLockModule` | 注册 `ILockFactory` | 依赖绑定 `FileSynchronization` 的内部模块 |
| `FileSynchronizationFactory(DirectoryInfo)` | 按目录创建锁 | 目录对象必填 |
| `FileLockProvider(FileInfo)` | 对指定锁文件加锁 | 文件路径必填 |
| `FileLockProvider(DirectoryInfo, string)` | 按名称生成锁文件 | 名称会转成可移植文件名 |
| `FileLockProvider.Name` | 锁文件完整路径 | 只读 |
| `FileLockProvider.Acquire/TryAcquire` | 同步获取 | 竞争时返回 `null`/超时 |
| `FileLockProvider.AcquireAsync/TryAcquireAsync` | 异步获取 | 支持取消与 `TimeoutValue` |
| `FileSynchronizationHandle.Dispose/DisposeAsync` | 关闭文件句柄并删除锁文件 | `await using` 自动调用 |
| `FileSynchronizationOptions.Directory` | 锁文件目录 | 无默认，必填 |
| `FileSynchronizationOptions.ConfigurationSection` | 配置节名常量 | `FileSynchronization` |
| `ISynchronizationHandle.HandleCancellationToken` | 句柄丢失监视 | 文件实现返回 `None` |
| `ILockFactory.Create(string)` | 按名称创建锁 | 名称必填 |
| `Extensions.Acquire/TryAcquire(name, ...)` | 工厂级快捷调用 | 内部 `Create` |
| `FileSystemLockModule` 的依赖绑定 | 自动读取配置节 | 显式 `Configure` 优先 |

### 配置项

| 配置键 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `FileSynchronization:Directory` | `string` | 无（必填） | 锁文件目录；获取锁时自动创建 |

```json
{
  "FileSynchronization": {
    "Directory": "/var/lib/orders/locks"
  }
}
```

```bash
FileSynchronization__Directory=/var/lib/orders/locks   # .NET 原生
FILESYNCHRONIZATION_DIRECTORY=/var/lib/orders/locks    # 仓库 alias
```

## 相关文档

- [项目 README：安装、完整选项与限制](../../Source/Euonia.Concurrency.FileSystem/README.md)
- [Euonia.Concurrency 用户手册](./Euonia.Concurrency.md)
- [Euonia.Concurrency.Redis 用户手册](./Euonia.Concurrency.Redis.md)
- [根 README](../../README.md)
