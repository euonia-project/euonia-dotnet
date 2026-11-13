# Euonia.Pipeline 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Pipeline |
| 公共类型 | 10 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddPipeline(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Pipeline

### DefaultPipelineProvider<TRequest, TResponse>

**类型**：class
基类 PipelineBase<TRequest, TResponse>；接口 IPipeline<TRequest, TResponse>

- new DefaultPipelineProvider<TRequest, TResponse>(System.IServiceProvider provider)

### IDelegateBehavior<TRequest>

**类型**：interface

- System.Threading.Tasks.Task HandleAsync(TRequest request, System.Delegate next, System.Threading.CancellationToken cancellationToken)

### IPipeline<TRequest, TResponse>

**类型**：interface

- IReadOnlyList<PipelineDelegateComponent<TRequest, TResponse>> Components { get; }
- PipelineDelegate<TRequest, TResponse> Build()
- Task<TResponse> RunAsync(TRequest context)
- Task<TResponse> RunAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate)
- IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component)
- IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component, System.Int32 priority)
- IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler)
- IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler, System.Int32 priority)
- IPipeline<TRequest, TResponse> Use(System.Type type, System.Object[] args)
- IPipeline<TRequest, TResponse> Use(System.Type type, System.Int32 priority, System.Object[] args)
- IPipeline<TRequest, TResponse> Use<TBehavior>(Nullable<System.Int32> priority = default)
- IPipeline<TRequest, TResponse> UseOf<TContext>(System.Boolean useAheadOfOthers = false)
- IPipeline<TRequest, TResponse> UseOf(System.Type contextType, System.Boolean useAheadOfOthers = false)

### IPipelineBehavior<TRequest, TResponse>

**类型**：interface

- Task<TResponse> HandleAsync(TRequest context, PipelineDelegate<TRequest, TResponse> next)

### Nerosoft.Euonia.Pipeline.Pipeline

**类型**：class

- new Nerosoft.Euonia.Pipeline.Pipeline()
- static TResponse Run<TRequest, TResponse>(TRequest context, Func<TRequest, TResponse> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
- static void Run<TRequest>(TRequest context, Action<TRequest> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
- static Task<TResponse> RunAsync<TRequest, TResponse>(TRequest context, Func<TRequest, Task<TResponse>> handler, IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors, System.Threading.CancellationToken cancellationToken = default)

### PipelineBase<TRequest, TResponse>

**类型**：class
接口 IPipeline<TRequest, TResponse>

- IReadOnlyList<PipelineDelegateComponent<TRequest, TResponse>> Components { get; }
- PipelineDelegate<TRequest, TResponse> Build()
- Task<TResponse> RunAsync(TRequest context)
- Task<TResponse> RunAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate)
- IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component)
- IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component, System.Int32 priority)
- IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler)
- IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler, System.Int32 priority)
- IPipeline<TRequest, TResponse> Use(System.Type type, System.Object[] args)
- IPipeline<TRequest, TResponse> Use(System.Type type, System.Int32 priority, System.Object[] args)
- IPipeline<TRequest, TResponse> Use<TBehavior>(Nullable<System.Int32> priority = default)
- IPipeline<TRequest, TResponse> UseOf<TContext>(System.Boolean useAheadOfOthers = false)
- IPipeline<TRequest, TResponse> UseOf(System.Type contextType, System.Boolean useAheadOfOthers = false)

### Nerosoft.Euonia.Pipeline.PipelineBehaviorAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Pipeline.PipelineBehaviorAttribute(System.Type behaviorType, System.Int32 priority = 0)
- System.Type BehaviorType { get; }
- System.Int32 Priority { get; }

### PipelineDelegate<TRequest, TResponse>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new PipelineDelegate<TRequest, TResponse>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(TRequest request, System.AsyncCallback callback, System.Object object)
- Task<TResponse> EndInvoke(System.IAsyncResult result)
- Task<TResponse> Invoke(TRequest request)

### PipelineDelegateComponent<TRequest, TResponse>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new PipelineDelegateComponent<TRequest, TResponse>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(PipelineDelegate<TRequest, TResponse> handle, System.AsyncCallback callback, System.Object object)
- PipelineDelegate<TRequest, TResponse> EndInvoke(System.IAsyncResult result)
- PipelineDelegate<TRequest, TResponse> Invoke(PipelineDelegate<TRequest, TResponse> handle)
