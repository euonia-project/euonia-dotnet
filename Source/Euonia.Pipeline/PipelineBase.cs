// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

using System.Reflection;

namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// Specified an abstract implement of <see cref="IPipeline"/>.
/// </summary>
public abstract class PipelineBase : IPipeline
{
	/// <summary>
	/// 
	/// </summary>
	public IList<Func<PipelineDelegate, PipelineDelegate>> Components { get; } = new List<Func<PipelineDelegate, PipelineDelegate>>();

	#region Implements

	/// <summary>
	/// 
	/// </summary>
	/// <param name="component"></param>
	/// <returns></returns>
	public virtual IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component)
	{
		Components.Add(component);
		return this;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="component"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public virtual IPipeline Use(Func<PipelineDelegate, PipelineDelegate> component, int index)
	{
		Components.Insert(index, component);
		return this;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="handler"></param>
	/// <returns></returns>
	public virtual IPipeline Use(Func<object, PipelineDelegate, Task> handler)
	{
		return Use(next =>
		{
			return context => handler(context, next);
		});
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public virtual IPipeline Use(Type type, params object[] args)
	{
		return Use(next => GetNext(next, type, args));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBehavior"></typeparam>
	/// <returns></returns>
	public virtual IPipeline Use<TBehavior>()
	{
		return Use(typeof(TBehavior));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	/// <param name="useAheadOfOthers"></param>
	/// <returns></returns>
	public virtual IPipeline UseOf<TContext>(bool useAheadOfOthers = false)
	{
		return UseOf(typeof(TContext));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="contextType"></param>
	/// <param name="useAheadOfOthers"></param>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public virtual async Task RunAsync(object context)
	{
		var type = context.GetType();
		var pipeline = UseOf(type, true);
		var @delegate = pipeline.Build();
		await @delegate(context);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <param name="accumulate"></param>
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
	/// 
	/// </summary>
	/// <param name="next"></param>
	/// <param name="type"></param>
	/// <param name="constructorArguments"></param>
	/// <returns></returns>
	protected abstract PipelineDelegate GetNext(PipelineDelegate next, Type type, params object[] constructorArguments);

	#endregion
}

/// <summary>
/// Specified a abstract implement of <see cref="IPipeline{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class PipelineBase<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
	private readonly List<Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>>> _components = new();

	/// <summary>
	/// 
	/// </summary>
	public IReadOnlyList<Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>>> Components => _components;

	#region Implements

	/// <summary>
	/// 
	/// </summary>
	/// <param name="component"></param>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component)
	{
		_components.Add(component);
		return this;
	}

	/// <summary>
	/// Use the specified pipeline component.
	/// </summary>
	/// <param name="component"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<PipelineDelegate<TRequest, TResponse>, PipelineDelegate<TRequest, TResponse>> component, int index)
	{
		_components.Insert(index, component);
		return this;
	}

	/// <summary>
	/// Use the specified pipeline handler.
	/// </summary>
	/// <param name="handler"></param>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> Use(Func<TRequest, PipelineDelegate<TRequest, TResponse>, Task<TResponse>> handler)
	{
		return Use(next =>
		{
			return context => handler(context, next);
		});
	}

	/// <summary>
	/// Use the specified pipeline handler.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> Use(Type type, params object[] args)
	{
		return Use(next => GetNext(next, type, args));
	}

	/// <summary>
	/// Use the specified pipeline behavior.
	/// </summary>
	/// <typeparam name="TBehavior"></typeparam>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> Use<TBehavior>()
		where TBehavior : IPipelineBehavior<TRequest, TResponse>
	{
		return Use(typeof(TBehavior));
	}

	/// <summary>
	/// Use the specified pipeline behavior.
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	/// <param name="useAheadOfOthers"></param>
	/// <returns></returns>
	public virtual IPipeline<TRequest, TResponse> UseOf<TContext>(bool useAheadOfOthers = false)
	{
		return UseOf(typeof(TContext));
	}

	/// <summary>
	/// Use the specified pipeline behavior.
	/// </summary>
	/// <param name="contextType"></param>
	/// <param name="useAheadOfOthers"></param>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public virtual async Task<TResponse> RunAsync(TRequest context)
	{
		var type = context.GetType();
		var pipeline = UseOf(type, true);
		var @delegate = pipeline.Build();
		return await @delegate(context);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <param name="accumulate"></param>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <param name="next"></param>
	/// <param name="type"></param>
	/// <param name="constructorArguments"></param>
	/// <returns></returns>
	protected abstract PipelineDelegate<TRequest, TResponse> GetNext(PipelineDelegate<TRequest, TResponse> next, Type type, params object[] constructorArguments);

	#endregion
}