// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

using System.Reflection;

namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// <see cref="IPipeline"/> 的抽象实现。
/// </summary>
public abstract class PipelineBase : IPipeline
{
	/// <summary>
	/// 管道组件列表，每个组件用于包装管道委托。
	/// </summary>
	public IList<Func<PipelineDelegate, PipelineDelegate>> Components { get; } = new List<Func<PipelineDelegate, PipelineDelegate>>();

	#region Implements

	/// <summary>
	/// 向管道中添加一个组件。
	/// </summary>
	/// <param name="component">用于包装管道委托的组件函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component)
	{
		Components.Add(component);
		return this;
	}

	/// <summary>
	/// 在指定的索引位置向管道中插入一个组件。
	/// </summary>
	/// <param name="component">用于包装管道委托的组件函数。</param>
	/// <param name="index">要插入的索引位置。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component, int index)
	{
		Components.Insert(index, component);
		return this;
	}

	/// <summary>
	/// 向管道中添加一个基于委托（handler）的组件。
	/// </summary>
	/// <param name="handler">接收管道上下文和下一个委托的处理函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline Use(Func<object, PipelineDelegate, Task> handler)
	{
		return Use(next =>
		{
			return context => handler(context, next);
		});
	}

	/// <summary>
	/// 向管道中添加一个指定类型的组件。
	/// </summary>
	/// <param name="type">组件类型。</param>
	/// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline Use(Type type, params object[] args)
	{
		return Use(next => GetNext(next, type, args));
	}

	/// <summary>
	/// 向管道中添加指定类型的组件（泛型形式）。
	/// </summary>
	/// <typeparam name="TBehavior">组件类型。</typeparam>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline Use<TBehavior>()
	{
		return Use(typeof(TBehavior));
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的组件（泛型形式）。
	/// </summary>
	/// <typeparam name="TContext">上下文类型。</typeparam>
	/// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline UseOf<TContext>(bool useAheadOfOthers = false)
	{
		return UseOf(typeof(TContext));
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的组件。
	/// 从上下文类型上标记的 <see cref="PipelineBehaviorAttribute"/> 特性解析管道行为并注册到管道中。
	/// </summary>
	/// <param name="contextType">上下文类型。</param>
	/// <param name="useAheadOfOthers">指示这些行为是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline UseOf(Type contextType, bool useAheadOfOthers = false)
	{
		IPipeline pipeline = this;
		var attributes = contextType.GetCustomAttributes<PipelineBehaviorAttribute>(true).ToList();
		if (useAheadOfOthers)
		{
			for (var index = 0; index < attributes.Count; index++)
			{
				var attribute = attributes[index];
				pipeline = Use(next => GetNext(next, attribute.BehaviorType), index);
			}
		}
		else
		{
			foreach (var attribute in attributes)
			{
				pipeline = Use(attribute.BehaviorType);
			}
		}

		return pipeline;
	}

	/// <summary>
	/// 构建管道委托。
	/// 按逆序组合所有组件，最终形成完整的管道委托，并在完成后清空组件列表。
	/// </summary>
	/// <returns>构建完成的管道委托。</returns>
	public virtual PipelineDelegate Build()
	{
		try
		{
			// ReSharper disable once ConvertToLocalFunction
			PipelineDelegate app = _ => Task.CompletedTask;

			return Components.Reverse().Aggregate(app, (current, component) => component(current));
		}
		finally
		{
			Components.Clear();
		}
	}

	/// <summary>
	/// 运行管道委托。
	/// 根据上下文的运行时类型自动注册关联的管道行为并执行。
	/// </summary>
	/// <param name="context">管道上下文。</param>
	/// <returns>表示异步运行操作的任务。</returns>
	public virtual async Task RunAsync(object context)
	{
		var type = context.GetType();
		var pipeline = UseOf(type, true);
		var @delegate = pipeline.Build();
		await @delegate(context);
	}

	/// <summary>
	/// 运行管道委托，并指定累积（最终处理）委托。
	/// </summary>
	/// <param name="context">管道上下文。</param>
	/// <param name="accumulate">执行最终处理的累积委托。</param>
	/// <returns>表示异步运行操作的任务。</returns>
	public virtual async Task RunAsync(object context, Func<object, Task> accumulate)
	{
		Use((request, _) =>
		{
			return Task.Run(() => accumulate(request));
		});
		await RunAsync(context);
	}

	#endregion

	#region Abstract Methods

	/// <summary>
	/// 为指定的行为类型构建管道委托。
	/// </summary>
	/// <param name="next">管道中的下一个委托。</param>
	/// <param name="type">要调用的行为类型。</param>
	/// <param name="constructorArguments">传递给行为构造函数（Constructor）的可选参数。</param>
	/// <returns>组合后的管道委托。</returns>
	protected abstract PipelineDelegate GetNext(PipelineDelegate next, Type type, params object[] constructorArguments);

	#endregion
}

/// <summary>
/// <see cref="IPipeline{TRequest, TResponse}"/> 的抽象实现。
/// </summary>
/// <typeparam name="TRequest">请求的类型。</typeparam>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public abstract class PipelineBase<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
	private readonly List<Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>>> _components = new();

	/// <summary>
	/// 管道组件列表，每个组件用于包装类型化管道委托。
	/// </summary>
	public IReadOnlyList<Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>>> Components => _components;

	#region Implements

	/// <summary>
	/// 向管道中添加一个类型化组件。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component)
	{
		_components.Add(component);
		return this;
	}

	/// <summary>
	/// 在指定的索引位置向管道中插入一个类型化组件。
	/// </summary>
	/// <param name="component">用于包装类型化管道委托的组件函数。</param>
	/// <param name="index">要插入的索引位置。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component, int index)
	{
		_components.Insert(index, component);
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
	/// 向管道中添加一个指定类型的组件。
	/// </summary>
	/// <param name="type">组件类型。</param>
	/// <param name="args">传递给组件构造函数（Constructor）的可选参数。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use(Type type, params object[] args)
	{
		return Use(next => GetNext(next, type, args));
	}

	/// <summary>
	/// 向管道中添加一个类型化管道行为。
	/// </summary>
	/// <typeparam name="TBehavior">实现 <see cref="IPipelineBehavior{TRequest, TResponse}"/> 的行为类型。</typeparam>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> Use<TBehavior>()
		where TBehavior : IPipelineBehavior<TRequest, TResponse>
	{
		return Use(typeof(TBehavior));
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的类型化组件（泛型形式）。
	/// </summary>
	/// <typeparam name="TContext">上下文类型。</typeparam>
	/// <param name="useAheadOfOthers">指示该组件是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> UseOf<TContext>(bool useAheadOfOthers = false)
	{
		return UseOf(typeof(TContext));
	}

	/// <summary>
	/// 添加一个基于指定上下文类型的类型化组件。
	/// 从上下文类型上标记的 <see cref="PipelineBehaviorAttribute"/> 特性解析管道行为并注册到管道中。
	/// </summary>
	/// <param name="contextType">上下文类型。</param>
	/// <param name="useAheadOfOthers">指示这些行为是否应置于其他组件之前。</param>
	/// <returns>返回当前的 <see cref="IPipeline{TRequest, TResponse}"/> 实例，以便进行链式调用。</returns>
	public virtual IPipeline<TRequest, TResponse> UseOf(Type contextType, bool useAheadOfOthers = false)
	{
		IPipeline<TRequest, TResponse> pipeline = this;
		var attributes = contextType.GetCustomAttributes<PipelineBehaviorAttribute>(true).ToList();
		if (useAheadOfOthers)
		{
			for (var index = 0; index < attributes.Count; index++)
			{
				var attribute = attributes[index];
				pipeline = Use(next => GetNext(next, attribute.BehaviorType), index);
			}
		}
		else
		{
			foreach (var attribute in attributes)
			{
				pipeline = Use(attribute.BehaviorType);
			}
		}

		return pipeline;
	}

	/// <summary>
	/// 构建类型化管道委托。
	/// 按逆序组合所有组件，最终形成完整的类型化管道委托，并在完成后清空组件列表。
	/// </summary>
	/// <returns>构建完成的类型化管道委托。</returns>
	public virtual PipelineDelegate<TRequest, TResponse> Build()
	{
		try
		{
			// ReSharper disable once ConvertToLocalFunction
			PipelineDelegate<TRequest, TResponse> app = _ => Task.FromResult(default(TResponse));

			return Components.Reverse().Aggregate(app, (current, component) => component(current));
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
}
