namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 定义管道行为。
/// </summary>
public interface IPipelineBehavior
{
	/// <summary>
	/// 处理管道上下文。
	/// </summary>
	/// <param name="context">管道上下文。</param>
	/// <param name="next">管道中下一个处理阶段的委托。</param>
	/// <returns>表示异步处理操作的任务。</returns>
	Task HandleAsync(object context, PipelineDelegate next);
}

/// <summary>
/// 用于环绕内部处理程序的管道行为。
/// 实现类可添加额外的处理逻辑，并在必要时等待 <c>next</c> 委托。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
{
	/// <summary>
	/// 管道处理方法。执行任意额外的处理逻辑，并在必要时等待 <paramref name="next"/> 委托。
	/// </summary>
	/// <param name="context">传入的请求。</param>
	/// <param name="next">管道中下一个操作的可等待委托，最终该委托代表处理函数（handler）。</param>
	/// <returns>返回 <typeparamref name="TResponse"/> 的可等待任务。</returns>
	Task<TResponse> HandleAsync(TRequest context, PipelineDelegate<TRequest, TResponse> next);
}

/// <summary>
/// 用于环绕内部处理程序的管道行为。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
public interface IPipelineBehavior<TRequest>
{
	/// <summary>
	/// 管道处理方法。执行任意额外的处理逻辑，并在必要时等待 <paramref name="next"/> 委托。
	/// </summary>
	/// <param name="context">传入的请求。</param>
	/// <param name="next">管道中下一个操作的可等待委托，最终该委托代表处理函数（handler）。</param>
	/// <returns>表示异步处理操作的可等待任务。</returns>
	Task HandleAsync(TRequest context, PipelineDelegate<TRequest> next);
}