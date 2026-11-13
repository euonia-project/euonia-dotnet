# Euonia.Pipeline API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Pipeline` 提供受 ASP.NET Core 中间件启发的类型化管道：用 `IPipelineBehavior<TRequest, TResponse>` 环绕最终处理器，按优先级组合并支持依赖注入。

- **包名**：`Euonia.Pipeline`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Pipeline`（`AddPipeline` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Pipeline/README.md](../../../Source/Euonia.Pipeline/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整 API、优先级与生命周期边界](../../../Source/Euonia.Pipeline/README.md)
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 把管道行为挂到应用层
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 在模块中注册管道
- [Euonia.Hosting 用户手册](../Euonia.Hosting/API.md) —— Web 请求中的应用服务
- [仓库根 README](../../../README.md)
