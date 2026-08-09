namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 定义管道处理委托，接收管道上下文并返回异步任务。
/// </summary>
/// <param name="context">管道上下文。</param>
/// <returns>表示异步处理操作的任务。</returns>
public delegate Task PipelineDelegate(object context);

/// <summary>
/// 定义类型化管道处理委托，接收请求并返回异步任务。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <param name="request">请求实例。</param>
/// <returns>表示异步处理操作的任务。</returns>
public delegate Task PipelineDelegate<in TRequest>(TRequest request);

/// <summary>
/// 定义类型化管道处理委托，接收请求并返回包含响应的异步任务。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
/// <param name="request">请求实例。</param>
/// <returns>表示异步处理操作的任务，包含响应结果。</returns>
public delegate Task<TResponse> PipelineDelegate<in TRequest, TResponse>(TRequest request);