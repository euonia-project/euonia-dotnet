namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 定义管道组件委托，接收下一个类型化管道委托并返回包装后的新委托，用于链式组合管道处理步骤。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
/// <param name="handle">管道中的下一个处理委托。</param>
/// <returns>包装 <paramref name="handle"/> 后得到的类型化管道处理委托。</returns>
public delegate PipelineDelegate<TRequest, TResponse> PipelineDelegateComponent<TRequest, TResponse>(PipelineDelegate<TRequest, TResponse> handle);

/// <summary>
/// 定义类型化管道组件委托，接收下一个类型化管道委托并返回包装后的新委托，用于链式组合管道处理步骤。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <param name="handle">管道中的下一个处理委托。</param>
/// <returns>包装 <paramref name="handle"/> 后得到的类型化管道处理委托。</returns>
public delegate PipelineDelegate<TRequest> PipelineDelegateComponent<TRequest>(PipelineDelegate<TRequest> handle);

/// <summary>
/// 定义管道组件委托，接收下一个管道委托并返回包装后的新委托，用于链式组合管道处理步骤。
/// </summary>
/// <param name="handle">管道中的下一个处理委托。</param>
/// <returns>包装 <paramref name="handle"/> 后得到的管道处理委托。</returns>
public delegate PipelineDelegate PipelineDelegateComponent(PipelineDelegate handle);