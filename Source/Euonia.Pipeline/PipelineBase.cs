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
	/// <remarks>返回的是排序后的快照：构造委托与对外读取都不共享同一条 <see cref="List{T}"/>。</remarks>
	public IReadOnlyList<PipelineDelegateComponent<TRequest, TResponse>> Components =>
	[
		.. Snapshot()
			.OrderBy(t => t.Priority)
			.ThenBy(t => t.Sequence)
			.Select(c => c.Component)
	];

	/// <summary>
	/// 管道组件存储，每个组件携带执行优先级和注册序号（用于同优先级时的顺序保持）。
	/// </summary>
	private readonly List<ComponentEntry> _components = new();

	/// <summary>
	/// 保护 <see cref="_components"/> 与 <see cref="_sequence"/>：注册与构建可能来自不同线程
	/// （管道实例在容器里是瞬时的，但同一个实例完全可能被并发的两次 <c>RunAsync</c> 用到）。
	/// 裸 <see cref="List{T}"/> 在枚举期间被另一线程写入会直接损坏内部状态。
	/// </summary>
	private readonly object _sync = new();

	/// <summary>
	/// 注册序号计数器，每次添加组件时自增，用于同优先级时保持注册顺序。
	/// </summary>
	private long _sequence;

	/// <summary>
	/// 组件表项：优先级 + 注册序号 + 包装函数。
	/// </summary>
	private readonly struct ComponentEntry(int priority, long sequence, PipelineDelegateComponent<TRequest, TResponse> component)
	{
		public int Priority { get; } = priority;

		public long Sequence { get; } = sequence;

		public PipelineDelegateComponent<TRequest, TResponse> Component { get; } = component;
	}

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
		lock (_sync)
		{
			_components.Add(new ComponentEntry(priority ?? 0, _sequence++, component));
		}

		return this;
	}

	/// <summary>
	/// 取当前组件表的快照（复制），调用方在锁外排序与组装，避免在锁内做反射/表达式编译等重活。
	/// </summary>
	private List<ComponentEntry> Snapshot()
	{
		lock (_sync)
		{
			return [.. _components];
		}
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
		return AddComponent(next => GetNext(next, type, args), priority);
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
		var behaviors = ResolveBehaviors(contextType, useAheadOfOthers);

		lock (_sync)
		{
			foreach (var (behaviorType, priority) in behaviors)
			{
				_components.Add(new ComponentEntry(priority, _sequence++, next => GetNext(next, behaviorType)));
			}
		}

		return this;
	}

	/// <summary>
	/// 解析上下文类型上标记的 <see cref="PipelineBehaviorAttribute"/>，得到（行为类型, 优先级）列表。
	/// </summary>
	/// <remarks>只做反射、不碰共享状态，因此可以完全在锁外调用。</remarks>
	private static List<(Type BehaviorType, int Priority)> ResolveBehaviors(Type contextType, bool useAheadOfOthers)
	{
		List<(Type BehaviorType, int Priority)> behaviors = [];

		foreach (var attribute in contextType.GetCustomAttributes<PipelineBehaviorAttribute>(true))
		{
			// 置于最前：使用最小优先级，保证最先执行；否则使用特性声明的优先级。
			behaviors.Add((attribute.BehaviorType, useAheadOfOthers ? int.MinValue : attribute.Priority));
		}

		return behaviors;
	}

	/// <summary>
	/// 构建类型化管道委托。
	/// 按优先级组合所有组件（数字越小越先执行，同优先级按注册顺序），最终形成完整的类型化管道委托。
	/// </summary>
	/// <remarks>
	/// 构建是<strong>非破坏性</strong>的：组件表在构建后保持不变，因此同一个管道实例可以反复 <see cref="Build"/>，
	/// 每次得到等价的委托。曾经的「构建即清空」会让 <c>ActuatorBuilder&lt;TTarget&gt;</c> 之类的持有方
	/// 在第二次执行时静默丢失全部已注册行为。
	/// </remarks>
	/// <returns>构建完成的类型化管道委托。</returns>
	public virtual PipelineDelegate<TRequest, TResponse> Build()
	{
		return Compose(Snapshot());
	}

	/// <summary>
	/// 按 (优先级, 注册序号) 降序把组件包装到终结点上，得到完整的类型化管道委托。
	/// </summary>
	/// <remarks>
	/// 每个组件都是包装函数：接收“下一个委托”，返回包装后的委托。
	/// 因此只能从最内层的终结点开始、按执行顺序的逆序逐层向外包装：
	/// 优先级最高（最后执行）的组件最先被包装，成为最内层；优先级最低（最先执行）的最后包装，成为最外层。
	/// 必须在锁外调用：<c>Component</c> 的执行会触发反射与表达式编译，不适合持锁。
	/// </remarks>
	private static PipelineDelegate<TRequest, TResponse> Compose(IReadOnlyList<ComponentEntry> entries)
	{
		// ReSharper disable once ConvertToLocalFunction
		PipelineDelegate<TRequest, TResponse> app = _ => Task.FromResult(default(TResponse));

		return entries
			.OrderByDescending(c => c.Priority)
			.ThenByDescending(c => c.Sequence)
			.Aggregate(app, (current, c) => c.Component(current));
	}

	/// <summary>
	/// 运行类型化管道委托。
	/// 根据请求的运行时类型自动注册关联的管道行为并执行。
	/// </summary>
	/// <param name="context">管道请求上下文。</param>
	/// <returns>表示异步运行操作的任务，包含响应结果。</returns>
	public virtual async Task<TResponse> RunAsync(TRequest context)
	{
		return await RunCoreAsync(context, null);
	}

	/// <summary>
	/// 运行类型化管道委托，并指定累积（最终处理）委托。
	/// </summary>
	/// <param name="context">管道请求上下文。</param>
	/// <param name="accumulate">执行最终处理的累积委托。</param>
	/// <returns>表示异步运行操作的任务，包含响应结果。</returns>
	public virtual async Task<TResponse> RunAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate)
	{
		// 直接用累积委托本身作为终结点。此前用 Task.Run 包裹会为每条消息多引入一次线程池调度，
		// 且 Task.Run 内部对 AsyncLocal 的修改不会回流到调用方（影响关联 ID / 工作单元的传播）。
		return await RunCoreAsync(context, accumulate);
	}

	/// <summary>
	/// 取组件快照、按需附加本次运行专属的累积委托与请求类型行为，组装后执行。
	/// </summary>
	/// <remarks>
	/// <para>本次运行的附加项<strong>不写入共享组件表</strong>：累计委托与按请求类型解析的行为都是“这一次调用”的输入，
	/// 混入共享表会在复用同一实例时留下跨次的残留。</para>
	/// <para>顺序与旧实现一致：请求类型行为优先级为 <see cref="int.MinValue"/>，故排在最外层（最先执行）；
	/// 累积委托优先级 0、序号最大，故排在优先级 0 组件的最内层。</para>
	/// </remarks>
	private async Task<TResponse> RunCoreAsync(TRequest context, Func<TRequest, Task<TResponse>> accumulate)
	{
		var behaviors = ResolveBehaviors(context.GetType(), useAheadOfOthers: true);

		List<ComponentEntry> entries;
		lock (_sync)
		{
			entries = [.. _components];

			if (accumulate != null)
			{
				entries.Add(new ComponentEntry(0, _sequence++, _ => request => accumulate(request)));
			}

			foreach (var (behaviorType, priority) in behaviors)
			{
				entries.Add(new ComponentEntry(priority, _sequence++, next => GetNext(next, behaviorType)));
			}
		}

		return await Compose(entries)(context);
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