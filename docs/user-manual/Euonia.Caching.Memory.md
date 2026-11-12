# Euonia.Caching.Memory 用户手册

用 `Microsoft.Extensions.Caching.Memory` 提供进程内缓存，可作为多级缓存的 L1，也可通过 `ICacheService` 直接注入业务服务。本页用“用户资料读穿缓存 + 更新失效”演示完整接线。

- **NuGet 包**：`Euonia.Caching.Memory`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Caching.Memory`
- **完整安装与选项**：[项目 README](../../Source/Euonia.Caching.Memory/README.md)

## 教程

> 以下代码块按 `Program.cs`（接线与端点）与 `Services.cs`（服务类型）拆分；放在同一项目中即可编译，业务占位方法按项目替换。

### 场景

资料服务按用户 ID 读取资料，首次从数据库加载并缓存 5 分钟；更新资料后立即删除对应键。服务部署 3 个实例，接受“各实例本地缓存短暂不一致”，但要求单实例内不重复回源。

### 前置条件

- .NET 10 或 11 SDK。
- 无需外部服务；缓存随进程生命周期存在。
- 安装 `Euonia.Caching.Memory`。

### 1. 安装与注册

```bash
dotnet add package Euonia.Caching.Memory
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Caching;
using Nerosoft.Euonia.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MemoryCacheOptions>(options =>
{
    options.InstanceName = "profiles";
    options.KeyPrefix = "profiles";
    options.Expires = TimeSpan.FromMinutes(10);
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
});

builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<ProfileService>();

var app = builder.Build();
```

### 2. 读穿与失效

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Caching;

public sealed record Profile(long Id, string Name);

public sealed class ProfileService(ICacheService cache, ILogger<ProfileService> logger)
{
    public Profile Get(long id)
        => cache.GetOrAdd(
            $"user:{id}",
            () =>
            {
                logger.LogInformation("Loading profile {Id} from database.", id);
                return new Profile(id, $"user-{id}");
            },
            TimeSpan.FromMinutes(5));

    public void Update(long id, string name)
    {
        SaveToDatabase(id, name);
        cache.Remove<Profile>($"user:{id}");
    }

    // 数据库实现占位。
    private static void SaveToDatabase(long id, string name) { }
}
```

### 3. 暴露端点

```csharp
app.MapGet("/profiles/{id:long}", (long id, ProfileService service)
    => Results.Ok(service.Get(id)));

app.MapPut("/profiles/{id:long}", (long id, string name, ProfileService service) =>
{
    service.Update(id, name);
    return Results.NoContent();
});
```

### 运行与验证

```bash
dotnet run --urls http://localhost:5080

curl -s http://localhost:5080/profiles/42
curl -s http://localhost:5080/profiles/42
```

第一次请求应打印一次 `Loading profile 42`，第二次不再打印。更新后再次读取会重新加载：

```bash
curl -X PUT "http://localhost:5080/profiles/42?name=alice"
curl -s http://localhost:5080/profiles/42
```

停止进程后缓存清空，重启后的第一次读取会重新回源。

### 常见失败点

- **多实例读到不同资料**：进程内缓存不共享，这是预期行为；需要跨节点一致性时改用 `Euonia.Caching.Redis`。
- **`Expires` 或 `UpdateMode` 改了没效果**：这些选项只由 `MemoryCacheService` 内部管理器读取；直接用 `WithMemoryCacheHandle` 时要在构建器上设置。
- **`TryGet<object>` 读不到**：条目按写入时的 `TValue` 存储，读取必须使用同一类型；`Remove<TValue>` 才与类型无关。
- **前缀删除删不掉外部数据**：只有通过缓存服务/管理器写入并登记过的键才能按前缀删除。

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

- [项目 README：安装、完整选项与限制](../../Source/Euonia.Caching.Memory/README.md)
- [Euonia.Caching 用户手册](./Euonia.Caching.md)
- [Euonia.Caching.Redis 用户手册](./Euonia.Caching.Redis.md)
- [根 README](../../README.md)
