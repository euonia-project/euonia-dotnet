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

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层管道行为
- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 消息总线中的管道集成
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
