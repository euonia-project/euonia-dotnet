namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// 定义管道的契约。
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// 向管道中添加一个组件。
    /// </summary>
    /// <param name="component">用于包装管道委托的组件函数。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component);

    /// <summary>
    /// 在指定的索引位置向管道中插入一个组件。
    /// </summary>
    /// <param name="component">用于包装管道委托的组件函数。</param>
    /// <param name="index">要插入的索引位置。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component, int index);

    /// <summary>
    /// 向管道中添加一个基于委托（handler）的组件。
    /// </summary>
    /// <param name="handler">接收管道上下文和下一个委托的处理函数。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline Use(Func<object, PipelineDelegate, Task> handler);

    /// <summary>
    /// 向管道中添加一个指定类型的组件。
    /// </summary>
    /// <param name="type">组件类型。</param>
    /// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline Use(Type type, params object[] args);

    /// <summary>
    /// 向管道中添加指定类型的组件（泛型形式）。
    /// </summary>
    /// <typeparam name="TBehavior">组件类型。</typeparam>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline Use<TBehavior>();

    /// <summary>
    /// 添加一个基于指定上下文类型的组件（泛型形式）。
    /// </summary>
    /// <typeparam name="TContext">上下文类型。</typeparam>
    /// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline UseOf<TContext>(bool useAheadOfOthers = false);

    /// <summary>
    /// 添加一个基于指定上下文类型的组件。
    /// </summary>
    /// <param name="contextType">上下文类型。</param>
    /// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
    /// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
    IPipeline UseOf(Type contextType, bool useAheadOfOthers = false);

    /// <summary>
    /// 构建管道委托。
    /// </summary>
    /// <returns>构建完成的管道委托。</returns>
    PipelineDelegate Build();

    /// <summary>
    /// 运行管道委托。
    /// </summary>
    /// <param name="context">管道上下文。</param>
    /// <returns>表示异步运行操作的任务。</returns>
    Task RunAsync(object context);

    /// <summary>
    /// 运行管道委托，并指定累积（最终处理）委托。
    /// </summary>
    /// <param name="context">管道上下文。</param>
    /// <param name="accumulate">执行最终处理的累积委托。</param>
    /// <returns>表示异步运行操作的任务。</returns>
    Task RunAsync(object context, Func<object, Task> accumulate);
}

/// <summary>
/// 定义类型化管道的契约。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public interface IPipeline<TRequest, TResponse>
{
	/// <summary>
	/// 向管道中添加一个类型化组件。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component);

	/// <summary>
	/// 在指定的索引位置向管道中插入一个类型化组件。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <param name="index">要插入的索引位置。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component, int index);

    //IPipeline<TRequest, TResponse> Use(Func<TRequest, Func<Task<TResponse>>, Task<TResponse>> handler);

    /// <summary>
    /// 向管道中添加一个基于委托（handler）的类型化组件。
    /// </summary>
    /// <param name="handler">接收请求和下一个类型化委托的处理函数。</param>
    /// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler);

    /// <summary>
    /// 向管道中添加一个指定类型的组件。
    /// </summary>
    /// <param name="type">组件类型。</param>
    /// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
    /// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> Use(Type type, params object[] args);

    /// <summary>
    /// 向管道中添加一个类型化管道行为。
    /// </summary>
    /// <typeparam name="TBehavior">实现 <see cref="IPipelineBehavior{TRequest, TResponse}"/> 的行为类型。</typeparam>
    /// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> Use<TBehavior>() where TBehavior : IPipelineBehavior<TRequest, TResponse>;

    /// <summary>
    /// 添加一个基于指定上下文类型的类型化组件（泛型形式）。
    /// </summary>
    /// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
    /// <typeparam name="TContext">上下文类型。</typeparam>
    /// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> UseOf<TContext>(bool useAheadOfOthers = false);

    /// <summary>
    /// 添加一个基于指定上下文类型的类型化组件。
    /// </summary>
    /// <param name="contextType">上下文类型。</param>
    /// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
    /// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
    IPipeline<TRequest, TResponse> UseOf(Type contextType, bool useAheadOfOthers = false);

    /// <summary>
    /// 构建类型化管道委托。
    /// </summary>
    /// <returns>构建完成的类型化管道委托。</returns>
    PipelineDelegate<TRequest, TResponse> Build();

    /// <summary>
    /// 运行类型化管道委托。
    /// </summary>
    /// <param name="context">管道请求上下文。</param>
    /// <returns>表示异步运行操作的任务，包含响应结果。</returns>
    Task<TResponse> RunAsync(TRequest context);

    /// <summary>
    /// 运行类型化管道委托，并指定累积（最终处理）委托。
    /// </summary>
    /// <param name="context">管道请求上下文。</param>
    /// <param name="accumulate">执行最终处理的累积委托。</param>
    /// <returns>表示异步运行操作的任务，包含响应结果。</returns>
    Task<TResponse> RunAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate);
}