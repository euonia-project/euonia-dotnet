# Euonia.Auditing

Euonia 的审计内核：作用域/动作模型、记录契约、异步写入队列、脱敏序列化与 InMemory 存储；持久化由 EF Core / Mongo 适配包提供。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Auditing`
- **NuGet 包**：`Euonia.Auditing`

## 安装

```bash
dotnet add package Euonia.Auditing
```

持久化适配另装 [`Euonia.Auditing.EfCore`](../Euonia.Auditing.EfCore/README.md) 或 [`Euonia.Auditing.Mongo`](../Euonia.Auditing.Mongo/README.md)；HTTP 中间件在 `Euonia.Hosting`。

## 快速开始

```csharp
using Nerosoft.Euonia.Auditing;

builder.Services.AddAuditing(options =>
{
    options.Enabled = true;
    options.ApplicationName = "Orders";
})
.AddInMemoryAuditingStore();   // 生产环境替换为 EF / Mongo 适配
```

HTTP 管道（审计必须在租户之前）：

```csharp
using Nerosoft.Euonia.Hosting;

app.UseAuditing();
app.UseTenancy();
```

## 核心类型

审计内核的核心契约与类型：

| 类型 | 说明 |
|------|------|
| `IAuditingRecorder` | `EnsureScope` / `CreateScope` / `SubmitAsync` / `FlushAsync` |
| `IAuditingScope` | 一个执行作用域；`BeginAction` / `CompleteAsync` / `AbortAsync` |
| `IAuditingAction` | 设置参数 / 扩展属性，`Complete` / `Fail` |
| `IAuditingWriter` | 写入队列；失败开放 |
| `IAuditingStore` | 持久化契约；同 `ScopeId` 幂等 |
| `IAuditingStoreQuery` | 查询、按 `ScopeId` 查找、按时间清理 |
| `IAuditingStoreRouter` | 按记录选择存储（每租户审计库） |
| `AuditingRecord` / `AuditingAction` | 作用域记录与动作 |
| `AuditingScopeOptions` | 作用域的操作键、幂等键与 HTTP 元数据 |
| `AuditedAttribute` | 标记必须审计的方法或类型 |
| `AuditingOptions` | 全局选项（见「配置项」） |

## 使用示例

### 1. EF Core 存储 + 文件暂存

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.EfCore;

services.AddAuditing(options =>
{
    options.Enabled = true;
    options.ApplicationName = "Orders";
    options.SuccessDelivery = AuditingDelivery.Batched;
})
.AddFileAuditingSpool(Path.Combine(AppContext.BaseDirectory, "audit-spool"))
.AddAuditingEfCore<AuditingDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("Audit")));
```

### 2. 标注应用服务

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class OrderAppService
{
    [Audited(ActionName = "创建订单")]
    public Task<long> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 方法体
        return Task.FromResult(0L);
    }
}
```

> `[Audited]` 由 `Euonia.Application` 的 `AuditingInterceptor` 拦截；即使全局 `Enabled` 为 `false`，标注的方法仍会记录。

### 3. 手动作用域

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class ImportJob(IAuditingRecorder recorder)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scope = recorder.CreateScope(new AuditingScopeOptions
        {
            OperationKey = $"job:{Guid.NewGuid():N}",
            AutoComplete = false
        });

        var action = scope.BeginAction("OrderImport", nameof(RunAsync), "导入订单");
        try
        {
            // 业务逻辑
            action.Complete(result: null);
            await scope.CompleteAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            action.Fail(exception);
            await scope.AbortAsync(exception, cancellationToken);
            throw;
        }
    }
}
```

### 4. 查询失败记录

```csharp
using Nerosoft.Euonia.Auditing;

public sealed class AuditQueryService(IAuditingStoreQuery store)
{
    public Task<IReadOnlyList<AuditingRecord>> QueryFailedAsync(CancellationToken cancellationToken)
        => store.QueryAsync(new AuditingQuery { IsSuccess = false, Take = 100 }, cancellationToken);
}
```

## 配置项

`AddAuditing` 在 `IConfiguration` 已注册时绑定 `Euonia:Auditing`；传入的 `configure` 委托在绑定之后执行，优先级更高。

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `Enabled` | `bool` | `false` | 是否启用审计 |
| `ApplicationName` | `string?` | `null` | 写入记录的应用名称 |
| `CrossTenantQueryAuthorizer` | `Func<AuditingQuery, bool>?` | `null` | 跨租户查询授权；仅代码配置，`null` 表示拒绝所有跨租户查询 |
| `RecordAnonymous` | `bool` | `false` | 是否记录匿名成功调用 |
| `RecordParameters` | `bool` | `true` | 是否记录动作参数（写入前脱敏） |
| `RecordStackTrace` | `bool` | `false` | 是否记录异常堆栈 |
| `RecordResult` | `bool` | `true` | 是否记录返回值（写入前脱敏） |
| `Mode` | `AuditingTriggerMode` | `All` | `All` 记录全部；`WritesOnly` 跳过读前缀方法 |
| `MaxDepth` | `int` | `8` | 脱敏递归深度上限 |
| `SuccessDelivery` | `AuditingDelivery` | `Batched` | 成功记录投递方式 |
| `FailureDelivery` | `AuditingDelivery` | `Immediate` | 失败记录投递方式 |
| `BatchSize` | `int` | `50` | 批大小（最小按 1 处理） |
| `BatchInterval` | `TimeSpan` | `00:00:02` | 批刷写间隔；非正值回退为 1 秒 |
| `WriteRetryCount` | `int` | `1` | 写入失败重试次数（不含首次） |
| `QueueCapacity` | `int` | `0` | 写入队列容量；`0` 表示无界 |

```json
{
  "Euonia": {
    "Auditing": {
      "Enabled": true,
      "ApplicationName": "Orders",
      "Mode": "WritesOnly",
      "SuccessDelivery": "Batched",
      "FailureDelivery": "Immediate"
    }
  }
}
```

```bash
Euonia__Auditing__Enabled=true          # .NET 原生：双下划线表示层级
EUONIA_AUDITING_ENABLED=true            # 仓库 alias：大写单下划线
```

## 语义与保证

- `ScopeId` = `{actorTenantId ?? "-"}:{opKey}`；HTTP 使用 `req:{Guid}`，消息使用 `msg:{MessageId}`。超过 256 字符时截断并追加 SHA-256 前缀，保持唯一性。
- 审计失败开放：写入异常只记录日志，不改变业务结果。
- 参数与结果在序列化前经 `SensitiveDataMasker` 脱敏；异常消息也会截断并遮蔽凭据片段。
- `IsSuccess` 为 `null` 表示未知（未封口动作）；`IAuditingStore` 实现必须按 `ScopeId` 幂等。
- 写入队列满时丢弃当前记录并计入 `euonia.auditing.records.dropped`；写入器已停止时计入 `euonia.auditing.records.rejected`。
- 注册 `IAuditingSpool` 后，记录先落暂存再入队；落库成功后删除，进程崩溃后启动时重放。
- `AddAuditing` 幂等：重复调用不会重复注册 `IHostedService`，但选项配置仍会生效。
- 未安装租户能力时租户字段为空，不影响审计可用性。

## 注意事项 / 限制

1. `AddAuditing` 不注册 `IAuditingStore`；必须再注册一个 `IAuditingStore`（`AddInMemoryAuditingStore`、`AddAuditingEfCore` 或 `AddAuditingMongo`），否则记录会被丢弃并输出警告。
2. `AddInMemoryAuditingStore` 是进程内存储，重启即丢失，只适合测试与本地开发；生产环境使用 EF Core / Mongo 适配。
3. `UseAuditing()` 来自 `Euonia.Hosting`，必须位于 `UseTenancy()` 之前（外层）；否则租户中间件释放后无法回填租户字段。
4. 写入器、记录器在构造时读取 `AuditingOptions`；运行期修改选项不会重新配置队列容量、批大小或投递方式，需重启宿主。
5. `AddFileAuditingSpool` 会在调用方线程执行一次文件写入 I/O；对延迟敏感的路径应关闭 spool 或换低延迟后端。
6. `IAuditingStore` 实现不得依赖调用方的请求作用域；写入器每批创建自己的 DI 作用域，后台写入没有 HTTP 上下文。
7. `AddAuditing` 只有在 `IServiceCollection` 中已存在 `IConfiguration` 时才绑定配置节；纯手工构造的容器需要自行 `Configure<AuditingOptions>`。
8. `CrossTenantQueryAuthorizer` 无法通过 JSON 配置，只能代码赋值；未设置时跨租户查询一律拒绝。

## 相关包

- [`Euonia.Auditing.EfCore`](../Euonia.Auditing.EfCore/README.md) —— EF Core 存储
- [`Euonia.Auditing.Mongo`](../Euonia.Auditing.Mongo/README.md) —— MongoDB 存储
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— `UseAuditing()` 中间件
- [`Euonia.Application`](../Euonia.Application/README.md) —— `[Audited]` 拦截器
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 租户字段来源
- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础类型

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
