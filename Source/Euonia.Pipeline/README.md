# Euonia.Pipeline

受 ASP.NET Core 中间件启发的类型化管道框架：用 `IPipeline<TRequest, TResponse>` 串联可组合的 `IPipelineBehavior<TRequest, TResponse>`，支持委托、类型化行为、优先级与依赖注入。

ASP.NET Core-inspired typed middleware pipeline for Euonia: compose `IPipelineBehavior<TRequest, TResponse>` behaviors with delegates, typed behaviors, priorities and dependency injection.

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

var result = await pipeline.RunAsync(new OrderContext(42));
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

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层管道行为
- [`Euonia.Bus`](../Euonia.Bus/README.md) —— 消息总线中的管道集成
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
