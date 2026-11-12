# Euonia.Concurrency.FileSystem API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

用目录中的锁文件实现跨进程互斥，适合单机多进程、容器共享卷或已确认语义的共享文件系统。本页用“夜间订单汇总只跑一次”演示配置、模块与双进程验证。

- **NuGet 包**：`Euonia.Concurrency.FileSystem`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Concurrency.FileSystem`
- **完整安装与选项**：[项目 README](../../../Source/Euonia.Concurrency.FileSystem/README.md)

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

- [教程](TUTORIAL.md)
- [项目 README：安装、完整选项与限制](../../../Source/Euonia.Concurrency.FileSystem/README.md)
- [Euonia.Concurrency 用户手册](../Euonia.Concurrency/API.md)
- [Euonia.Concurrency.Redis 用户手册](../Euonia.Concurrency.Redis/API.md)
- [根 README](../../../README.md)
