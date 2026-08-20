using System.Reflection;

namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// <see cref="IPipeline{TRequest, TResponse}"/> 的抽象实现。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public abstract class PipelineBase<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
	/// <summary>
	/// 管道组件列表（含优先级），数字越小越先执行，同优先级按注册顺序执行。
	/// </summary>
	public IReadOnlyList<PipelineDelegateComponent<TRequest, TResponse>> Components =>
	[
		.. _components.OrderBy(t => t.Priority)
		              .ThenBy(t => t.Sequence)
		              .Select(c => c.Component)
	];

	/// <summary>
	/// 管道组件存储，每个组件携带执行优先级和注册序号（用于同优先级时的顺序保持）。
	/// </summary>
	private readonly List<(int Priority, long Sequence, PipelineDelegateComponent<TRequest, TResponse> Component)> _components = new();

	/// <summary>
	/// 注册序号计数器，每次添加组件时自增，用于同优先级时保持注册顺序。
	/// </summary>
	private long _sequence;

	#region Implements

	/// <summary>
	/// 向管道中添加一个类型化组件。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component)
	{
		return AddComponent(component, null);
	}

	/// <summary>
	/// 向管道中添加一个指定优先级的类型化组件，数字越小越先执行。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <param name="priority">执行优先级，数字越小越先执行。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(PipelineDelegateComponent<TRequest, TResponse> component, int priority)
	{
		return AddComponent(component, priority);
	}

	/// <summary>
	/// 向管道中添加一个类型化组件，优先级未指定时按注册顺序推导。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <param name="priority">执行优先级，为 null 时按注册顺序推导。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	private IPipeline<TRequest, TResponse> AddComponent(PipelineDelegateComponent<TRequest, TResponse> component, int? priority)
	{
		_components.Add((priority ?? 0, _sequence++, component));
		return this;
	}

	/// <summary>
	/// 向管道中添加一个基于委托（handler）的类型化组件。
	/// </summary>
	/// <param name="handler">接收请求和下一个类型化委托的处理函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler)
	{
		return Use(next =>
		{
			return context => handler(context, next);
		});
	}

	/// <summary>
	/// 向管道中添加一个指定优先级的基于委托（handler）的类型化组件，数字越小越先执行。
	/// </summary>
	/// <param name="handler">接收请求和下一个类型化委托的处理函数。</param>
	/// <param name="priority">执行优先级，数字越小越先执行。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler, int priority)
	{
		return Use(next =>
		{
			return context => handler(context, next);
		}, priority);
	}

	/// <summary>
	/// 向管道中添加一个指定类型的组件。
	/// </summary>
	/// <param name="type">组件类型。</param>
	/// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Type type, params object[] args)
	{
		return AddComponent(next => GetNext(next, type, args), null);
	}

	/// <summary>
	/// 向管道中添加一个指定优先级和类型的组件，数字越小越先执行。
	/// </summary>
	/// <param name="type">组件类型。</param>
	/// <param name="priority">执行优先级，数字越小越先执行。</param>
	/// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Type type, int priority, params object[] args)
	{
		return Use(next => GetNext(next, type, args), priority);
	}

	/// <summary>
	/// 向管道中添加一个类型化管道行为。
	/// 未指定优先级时，从类型上标记的 <see cref="PipelineBehaviorAttribute"/> 获取，否则按注册顺序推导。
	/// </summary>
	/// <typeparam name="TBehavior">实现 <see cref="IPipelineBehavior{TRequest, TResponse}"/> 的行为类型。</typeparam>
	/// <param name="priority">执行优先级，为 null 时从 <see cref="PipelineBehaviorAttribute"/> 获取或按注册顺序推导。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use<TBehavior>(int? priority = null)
		where TBehavior : IPipelineBehavior<TRequest, TResponse>
	{
		return Use(next => GetNext(next, typeof(TBehavior)), priority ?? ResolvePriority(typeof(TBehavior)));
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的类型化组件（泛型形式）。
	/// </summary>
	/// <typeparam name="TContext">上下文类型。</typeparam>
	/// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> UseOf<TContext>(bool useAheadOfOthers = false)
	{
		return UseOf(typeof(TContext), useAheadOfOthers);
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的类型化组件。
	/// 从上下文类型上标记的 <see cref="PipelineBehaviorAttribute"/> 特性解析管道行为并注册到管道中，
	/// 行为优先级取自特性上声明的优先级。
	/// </summary>
	/// <param name="contextType">上下文类型。</param>
	/// <param name="useAheadOfOthers">指示这些行为是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> UseOf(Type contextType, bool useAheadOfOthers = false)
	{
		IPipeline<TRequest, TResponse> pipeline = this;
		var attributes = contextType.GetCustomAttributes<PipelineBehaviorAttribute>(true).ToList();
		foreach (var attribute in attributes)
		{
			// 置于最前：使用最小优先级，保证最先执行；否则使用特性声明的优先级。
			var priority = useAheadOfOthers ? int.MinValue : attribute.Priority;
			pipeline = Use(next => GetNext(next, attribute.BehaviorType), priority);
		}

		return pipeline;
	}

	/// <summary>
	/// 构建类型化管道委托。
	/// 按优先级组合所有组件（数字越小越先执行，同优先级按注册顺序），最终形成完整的类型化管道委托，并在完成后清空组件列表。
	/// </summary>
	/// <returns>构建完成的类型化管道委托。</returns>
	public virtual PipelineDelegate<TRequest, TResponse> Build()
	{
		try
		{
			// ReSharper disable once ConvertToLocalFunction
			PipelineDelegate<TRequest, TResponse> app = _ => Task.FromResult(default(TResponse));

			// 执行顺序：优先级小的先执行，同优先级按注册顺序（Sequence）执行。
			// 但每个组件都是包装函数：接收“下一个委托”，返回包装后的委托，
			// 因此只能从最内层的终结点（app）开始、按执行顺序的逆序逐层向外包装：
			// 优先级最高（最后执行）的组件最先被包装，成为最内层；
			// 优先级最低（最先执行）的组件最后被包装，成为最外层。
			return Components.Reverse()
			                 .Aggregate(app, (current, component) => component(current));
		}
		finally
		{
			_components.Clear();
		}
	}

	/// <summary>
	/// 运行类型化管道委托。
	/// 根据请求的运行时类型自动注册关联的管道行为并执行。
	/// </summary>
	/// <param name="context">管道请求上下文。</param>
	/// <returns>表示异步运行操作的任务，包含响应结果。</returns>
	public virtual async Task<TResponse> RunAsync(TRequest context)
	{
		var type = context.GetType();
		var pipeline = UseOf(type, true);
		var @delegate = pipeline.Build();
		return await @delegate(context);
	}

	/// <summary>
	/// 运行类型化管道委托，并指定累积（最终处理）委托。
	/// </summary>
	/// <param name="context">管道请求上下文。</param>
	/// <param name="accumulate">执行最终处理的累积委托。</param>
	/// <returns>表示异步运行操作的任务，包含响应结果。</returns>
	public virtual async Task<TResponse> RunAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate)
	{
		Use((request, _) =>
		{
			return Task.Run(() => accumulate(request));
		});
		return await RunAsync(context);
	}

	#endregion

	#region Abstract Methods

	/// <summary>
	/// 为指定的行为类型构建类型化管道委托。
	/// </summary>
	/// <param name="next">管道中的下一个委托。</param>
	/// <param name="type">要调用的行为类型。</param>
	/// <param name="constructorArguments">传递给行为构造函数（Constructor）的可选参数。</param>
	/// <returns>组合后的类型化管道委托。</returns>
	protected abstract PipelineDelegate<TRequest, TResponse> GetNext(PipelineDelegate<TRequest, TResponse> next, Type type, params object[] constructorArguments);

	#endregion

	/// <summary>
	/// 解析组件的执行优先级：未显式指定时，从类型上标记的 <see cref="PipelineBehaviorAttribute"/> 获取，否则返回 0（按注册顺序推导）。
	/// </summary>
	/// <param name="type">组件类型。</param>
	/// <returns>解析后的优先级。</returns>
	private static int ResolvePriority(Type type)
	{
		return type.GetCustomAttribute<PipelineBehaviorAttribute>(true)?.Priority ?? 0;
	}
}