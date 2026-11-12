# Euonia.Pipeline 用户手册

`Euonia.Pipeline` 提供受 ASP.NET Core 中间件启发的类型化管道：用 `IPipelineBehavior<TRequest, TResponse>` 环绕最终处理器，按优先级组合并支持依赖注入。

- **包名**：`Euonia.Pipeline`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Pipeline`（`AddPipeline` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Pipeline/README.md](../../Source/Euonia.Pipeline/README.md)

## 教程：为下单请求组合日志与指标行为

这个场景创建一个控制台应用，注册两个环绕行为，最后用 `RunAsync` 的终结点执行真正的下单逻辑。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 不依赖 ASP.NET Core，只需要 DI 与日志抽象。

### 1. 创建项目并安装

```bash
dotnet new console -n OrderPipeline
cd OrderPipeline
dotnet add package Euonia.Pipeline
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Logging.Console
```

### 2. 定义请求、响应与行为

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

public sealed record SubmitOrder(long OrderId);

public sealed record SubmitResult(bool Accepted);

public sealed class AuditBehavior(ILogger<AuditBehavior> logger)
    : IPipelineBehavior<SubmitOrder, SubmitResult>
{
    public async Task<SubmitResult> HandleAsync(
        SubmitOrder context,
        PipelineDelegate<SubmitOrder, SubmitResult> next)
    {
        logger.LogInformation("before order {OrderId}", context.OrderId);
        var result = await next(context);
        logger.LogInformation("after order {OrderId}: {Accepted}", context.OrderId, result.Accepted);
        return result;
    }
}

public sealed class MetricsBehavior : IPipelineBehavior<SubmitOrder, SubmitResult>
{
    public async Task<SubmitResult> HandleAsync(
        SubmitOrder context,
        PipelineDelegate<SubmitOrder, SubmitResult> next)
    {
        var started = DateTimeOffset.UtcNow;
        var result = await next(context);
        Console.WriteLine($"elapsed={(DateTimeOffset.UtcNow - started).TotalMilliseconds:F1}ms");
        return result;
    }
}
```

### 3. 注册并运行管道

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddPipeline();

using var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<IPipeline<SubmitOrder, SubmitResult>>();

var result = await pipeline
    .Use<AuditBehavior>(priority: 0)
    .Use<MetricsBehavior>(priority: 10)
    .RunAsync(
        new SubmitOrder(42),
        order => Task.FromResult(new SubmitResult(order.OrderId > 0)));

Console.WriteLine($"accepted={result.Accepted}");
```

### 4. 运行与验证

```bash
dotnet run
```

预期输出（顺序体现“外层先进入、内层后返回”）：

```text
info: AuditBehavior[0]
      before order 42
elapsed=0.2ms
info: AuditBehavior[0]
      after order 42: True
accepted=True
```

把 `AuditBehavior` 的优先级改为 `10`、`MetricsBehavior` 改为 `0`，会看到 `elapsed` 与日志的包裹顺序互换；数字越小越靠外层。

### 常见失败点

- **返回 `null`/`default`**：调用 `RunAsync(context)` 没有传终结点；必须用 `RunAsync(context, accumulate)` 或确保某个组件产生结果。
- **`Use(Type)` 首次执行报错**：方法级行为必须恰好有一个 `Handle`/`HandleAsync`、返回 `Task`、至少一个参数；优先实现 `IPipelineBehavior<,>`。
- **`UseOf<TContext>()` 不生效**：`PipelineBehaviorAttribute` 的 `Inherited = false`，必须标在运行时请求类型上。
- **根容器解析后报 Scoped 错误**：管道是 Transient，但不要从根容器长期持有；在请求/消息作用域内解析。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `IPipeline<TRequest, TResponse>` | 管道契约 | `Use`、`Build`、`RunAsync`、`Components` |
| `IPipelineBehavior<TRequest, TResponse>` | 环绕内层处理器的行为 | `HandleAsync(context, next)` |
| `PipelineDelegate<TRequest, TResponse>` | 类型化处理委托 | `Task<TResponse> (TRequest)` |
| `PipelineDelegateComponent<TRequest, TResponse>` | 带优先级的委托组件 | 由 `Use` 重载创建 |
| `PipelineBase<TRequest, TResponse>` | 反向链构建的抽象基类 | 需实现 `GetNext` |
| `DefaultPipelineProvider<TRequest, TResponse>` | 默认提供者，支持 DI 与表达式编译 | 构造函数接收 `IServiceProvider` |
| `AddPipeline()` | 注册开放的 `IPipeline<,>` 到默认提供者 | Transient |
| `Use(Func<...> handler, int priority)` | 添加委托组件 | 数字越小越先执行 |
| `Use<TBehavior>(int? priority)` | 添加 `IPipelineBehavior` | 未传时读取行为类型上的 `[PipelineBehavior]` |
| `UseOf<TContext>(bool useAheadOfOthers)` | 按上下文类型特性批量添加 | `true` 时置于最外层 |
| `Use(Type type, params object[] args)` | 添加方法级处理器 | 需唯一 `Handle`/`HandleAsync` |
| `Build()` | 构建委托 | 非破坏性，可重复调用 |
| `RunAsync(context)` | 运行管道 | 无终结点时返回 `default(TResponse)` |
| `RunAsync(context, accumulate)` | 指定最内层终结点 | `Func<TRequest, Task<TResponse>>` |
| `[PipelineBehavior(typeof(TBehavior), int priority = 0)]` | 在请求类型上声明行为 | `Inherited = false`，可重复 |
| `IDelegateBehavior<TRequest>` | 静态 `Pipeline.Run` 使用的委托行为 | `HandleAsync(request, Delegate next, CancellationToken)` |
| `Pipeline.Run/RunAsync` | 不依赖 DI 的静态门面 | 显式传入行为序列 |

### 配置说明

`Euonia.Pipeline` 没有配置节或 `*Options`；行为顺序、优先级和终结点全部通过 `Use`/`RunAsync` 代码控制。需要集中配置时，把管道构建封装到模块的 `ConfigureServices` 或应用服务中。

## 相关文档

- [Source README：完整 API、优先级与生命周期边界](../../Source/Euonia.Pipeline/README.md)
- [Euonia.Application 用户手册](./Euonia.Application.md) —— 把管道行为挂到应用层
- [Euonia.Modularity 用户手册](./Euonia.Modularity.md) —— 在模块中注册管道
- [Euonia.Hosting 用户手册](./Euonia.Hosting.md) —— Web 请求中的应用服务
- [仓库根 README](../../README.md)
