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
    /// 同步运行管道，依次执行指定的委托行为，最终由累积委托（accumulate）产生响应。
    /// 每个委托行为的 <see cref="Task"/> 都会在当前线程同步等待（阻塞），然后调用下一个委托；
    /// <paramref name="accumulate"/> 作为终结点产生响应值。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <typeparam name="TResponse">响应的类型。</typeparam>
    /// <param name="context">请求上下文。</param>
    /// <param name="accumulate">执行最终处理并产生响应的累积委托。</param>
    /// <param name="behaviors">要在管道中执行的委托行为集合。</param>
    /// <returns>管道运行产生的响应。</returns>
    public static TResponse Run<TRequest, TResponse>(TRequest context, Func<TRequest, TResponse> accumulate, IEnumerable<IDelegateBehavior<TRequest>> behaviors)
    {
        var @delegate = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => (Func<TRequest, TResponse>)(request =>
        {
            behavior.HandleAsync(request, @delegate, default).GetAwaiter().GetResult();
            return ((Func<TRequest, TResponse>)@delegate)(request);
        }));

        return ((Func<TRequest, TResponse>)@delegate)(context);
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
        var @delegate = behaviors.Aggregate((Delegate)accumulate, (@delegate, behavior) => (Action<TRequest>)(request =>
        {
            behavior.HandleAsync(request, @delegate, default).GetAwaiter().GetResult();
            ((Action<TRequest>)@delegate)(request);
        }));

        ((Action<TRequest>)@delegate)(context);
    }
}