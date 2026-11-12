# Euonia.Pipeline

受 ASP.NET Core 中间件启发的类型化管道框架：用 `IPipeline<TRequest, TResponse>` 串联可组合的 `IPipelineBehavior<TRequest, TResponse>`，支持委托、类型化行为、优先级与依赖注入。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Pipeline`
- **NuGet 包**：`Euonia.Pipeline`

## 安装

```bash
dotnet add package Euonia.Pipeline
```

## 主要功能

- **单一管道契约**：`IPipeline<TRequest, TResponse>` 覆盖类型化请求/响应场景。
- **多种组装方式**：`Use` 支持委托、类型、`IPipelineBehavior<,>` 与 `[PipelineBehavior]` 自动发现。
- **优先级**：数字越小越先执行；未指定时按注册顺序。
- **依赖注入**：`DefaultPipelineProvider<,>` 从 `IServiceProvider` 解析行为及其构造参数。
- **反向链构建**：最内层行为先执行，符合中间件语义。

## 快速开始

定义请求、响应与行为：

```csharp
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Pipeline;

public sealed record OrderContext(long OrderId);

public sealed record OrderResult(bool Success);

public sealed class LoggingBehavior : IPipelineBehavior<OrderContext, OrderResult>
{
    private readonly ILogger<LoggingBehavior> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior> logger) => _logger = logger;

    public async Task<OrderResult> HandleAsync(OrderContext context, PipelineDelegate<OrderContext, OrderResult> next)
    {
        _logger.LogInformation("Handling order {OrderId}", context.OrderId);
        var result = await next(context);
        _logger.LogInformation("Handled order {OrderId}: {Success}", context.OrderId, result.Success);
        return result;
    }
}
```

注册并运行管道：

```csharp
services.AddPipeline();

var pipeline = new DefaultPipelineProvider<OrderContext, OrderResult>(serviceProvider)
    .Use<LoggingBehavior>()
    .Use(async (context, next) =>
    {
        // 前置逻辑
        var result = await next(context);
        // 后置逻辑
        return result;
    });

var result = await pipeline.RunAsync(
    new OrderContext(42),
    context => Task.FromResult(new OrderResult(true)));
```

在请求类型上声明行为：

```csharp
[PipelineBehavior(typeof(LoggingBehavior), priority: 0)]
public sealed record SubmitOrderContext(long OrderId);
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `IPipeline<TRequest, TResponse>` | 管道契约：`Use`、`Build`、`RunAsync` |
| `IPipelineBehavior<TRequest, TResponse>` | 环绕内层处理器的行为契约 |
| `PipelineBase<TRequest, TResponse>` | 反向链构建的基础实现 |
| `DefaultPipelineProvider<TRequest, TResponse>` | 默认提供者，支持 DI 解析与表达式编译 |
| `PipelineDelegate<TRequest, TResponse>` | 类型化处理委托 |
| `PipelineDelegateComponent<TRequest, TResponse>` | 带优先级的委托组件 |
| `PipelineBehaviorAttribute` | 声明类型应使用的行为与优先级 |
| `AddPipeline()` | 注册管道基础服务 |

## 选项（代码配置）

`AddPipeline()` 只把开放的 `IPipeline<,>` 映射到 `DefaultPipelineProvider<,>`（Transient），没有配置节或 `*Options`。执行顺序与行为来源全部由代码控制：

| 配置点 | 说明 |
|--------|------|
| `Use(handler, priority)` | 添加委托组件；数字越小越先执行 |
| `Use<TBehavior>(priority)` | 添加 `IPipelineBehavior<TRequest, TResponse>`；未传优先级时读取类型上的 `[PipelineBehavior]` |
| `UseOf<TContext>(useAheadOfOthers)` | 按上下文类型上的 `[PipelineBehavior]` 批量添加行为；`true` 时置于最外层 |
| `[PipelineBehavior(typeof(Behavior), priority)]` | 在请求/上下文类型上声明行为与优先级 |
| `RunAsync(context, accumulate)` | 指定最内层终结点；不传时终结点返回 `default(TResponse)` |

## 注意事项 / 限制

- 不传 `accumulate` 调用 `RunAsync(context)` 时，管道最内层是 `default(TResponse)`；如果没有行为负责产生结果，调用会静默返回默认值而不是执行处理器。需要真实处理逻辑时使用 `RunAsync(context, accumulate)`。
- `Use<TBehavior>` 要求行为实现与请求/响应完全匹配的 `IPipelineBehavior<TRequest, TResponse>`；构造函数依赖由 `ActivatorUtilities` 解析，缺少注册时在首次执行而非注册时失败。
- 非 `IPipelineBehavior` 的行为类型走“处理方法”路径：必须有且只有一个公共 `Handle` 或 `HandleAsync` 方法、返回 `Task`、至少一个参数；按引用参数与 `CancellationToken` 参数不受支持，会抛出 `NotSupportedException`。
- 优先级数字越小越先执行（越外层），相同优先级按注册顺序；`Build()` 是非破坏性的，同一个管道实例可以反复构建并得到等价委托。
- `PipelineBehaviorAttribute` 只能标在类上，`Inherited = false` 且允许重复；`UseOf<TContext>()` 只读取该上下文类型自身声明的特性。
- 管道以 Transient 注册；如果从根容器解析并长期持有，构造依赖或行为中解析 Scoped 服务会触发“Cannot resolve scoped service from root provider”。应在请求/消息作用域内解析管道。

## 常见问题与故障排查

### 行为执行了，但管道返回 `null`/`default`
**原因**：`RunAsync(context)` 没有终结点，最内层是 `default(TResponse)`；如果行为只调用 `next` 而没有真正的处理器，返回值就是默认值。
**解决**：使用 `RunAsync(context, accumulate)` 传入最终处理委托；或确保最后一个组件负责产生结果。回归测试 `RunAsync_WithAccumulate_ShouldStillExecutePositivePriorityComponents` 覆盖了“累积委托不能被优先级组件包住”的边界。

### `Use(Type)` 的行为在首次执行时报“找不到处理方法”
**原因**：非 `IPipelineBehavior` 类型走方法级路径，必须有且只有一个公共 `Handle`/`HandleAsync` 方法、返回 `Task`、至少一个参数；多个/缺失方法、返回非 `Task` 或按引用/`CancellationToken` 参数会抛 `InvalidOperationException`/`NotSupportedException`。
**解决**：优先实现 `IPipelineBehavior<TRequest, TResponse>`；必须用方法级行为时，保留一个 `HandleAsync(TRequest, ...)` 并返回 `Task<TResponse>`，其余依赖通过构造函数或服务提供程序注入。

### `UseOf<TContext>()` 没有注册任何行为
**原因**：`PipelineBehaviorAttribute` 的 `Inherited = false`，且 `UseOf` 读取的是**运行时上下文类型**上声明的特性；把特性标在基类或另一个请求类型上不会生效。
**解决**：把 `[PipelineBehavior(...)]` 标在实际传入 `RunAsync` 的请求类型上；需要跨类型复用时改用 `Use<TBehavior>(priority)` 显式注册。

### 从根容器解析管道后报“Cannot resolve scoped service from root provider”
**原因**：管道以 Transient 注册，但若从根容器解析并长期持有，行为构造函数或处理器中解析 Scoped 服务会失败；`DefaultPipelineProvider` 持有的是解析时的 `IServiceProvider`。
**解决**：在请求/消息作用域内解析 `IPipeline<,>`；后台任务为每次执行创建 `IServiceScope`，不要缓存根容器解析出的管道实例。

## 最佳实践

- 始终给 `RunAsync` 传终结点；需要重复执行时用 `Build()` 缓存委托，`Build` 是非破坏性的。
- 行为保持无状态、Transient；依赖通过构造函数注入，避免在行为里捕获请求级状态。
- 用优先级表达顺序（数字小的在外层），同优先级依赖注册顺序；复杂组合优先用 `UseOf<TContext>` 声明式配置。
- 为管道写顺序测试：用记录列表的假行为断言“外层 → 内层 → 终结点”的调用顺序，并覆盖异常路径。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：`Microsoft.Extensions.DependencyInjection.Abstractions` 11.0.0；没有配置节或第三方运行时依赖。
- 已知边界：方法级行为的 `CancellationToken` 参数不受支持；`PipelineBehaviorAttribute` 不可继承；`RunAsync` 不传终结点时返回默认值。
- 升级提示：升级后先跑管道顺序与“累积委托不泄漏到组件表”的回归测试，再调整行为优先级。

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层管道行为
- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 消息总线中的管道集成
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
