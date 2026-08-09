namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 提供用于运行管道的静态方法。
/// </summary>
public class Pipeline
{
    /// <summary>
    /// 异步运行管道，依次执行指定的行为，最终由处理函数（handler）产生响应。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <typeparam name="TResponse">响应的类型。</typeparam>
    /// <param name="context">请求上下文。</param>
    /// <param name="handler">处理请求并产生响应的最终处理函数。</param>
    /// <param name="behaviors">要在管道中执行的管道行为集合。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务，包含管道运行产生的响应。</returns>
    public static async Task<TResponse> RunAsync<TRequest, TResponse>(TRequest context, Func<TRequest, Task<TResponse>> handler, IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors, CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            Task<TResponse> Accumulate(TRequest _) => handler(context);
            var response = behaviors.Aggregate((PipelineDelegate<TRequest, TResponse>)Accumulate, (@delegate, behavior) => request => behavior.HandleAsync(request, @delegate));
            return await response(context);
        }, cancellationToken);
    }

    /// <summary>
    /// 异步运行管道，依次执行指定的行为，最终由处理函数（handler）完成处理。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <param name="context">请求上下文。</param>
    /// <param name="handler">处理请求的最终处理函数。</param>
    /// <param name="behaviors">要在管道中执行的管道行为集合。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public static async Task RunAsync<TRequest>(TRequest context, Func<TRequest, Task> handler, IEnumerable<IPipelineBehavior<TRequest>> behaviors, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            Task Accumulate(TRequest _) => handler(context);
            var response = behaviors.Aggregate((PipelineDelegate<TRequest>)Accumulate, (@delegate, behavior) => request => behavior.HandleAsync(request, @delegate));
            await response(context);
        }, cancellationToken);
    }

    /// <summary>
    /// 同步运行管道，依次执行指定的委托行为，最终由累积委托（accumulate）完成处理。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <typeparam name="TResponse">响应的类型。</typeparam>
    /// <param name="context">请求上下文。</param>
    /// <param name="accumulate">执行最终处理的累积委托。</param>
    /// <param name="behaviors">要在管道中执行的委托行为集合。</param>
    /// <returns>管道运行产生的响应。</returns>
    public static TResponse Run<TRequest, TResponse>(TRequest context, Action<TRequest> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
    {
        var response = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => () => behavior.HandleAsync(context, @delegate, default));
        return (TResponse)response.DynamicInvoke(context);
    }

    /// <summary>
    /// 同步运行管道，依次执行指定的委托行为，最终由累积委托（accumulate）完成处理。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <param name="context">请求上下文。</param>
    /// <param name="accumulate">执行最终处理的累积委托。</param>
    /// <param name="behaviors">要在管道中执行的委托行为集合。</param>
    public static void Run<TRequest>(TRequest context, Action<TRequest> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
    {
        var response = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => () => behavior.HandleAsync(context, @delegate, default));
        response.DynamicInvoke(context);
    }
}