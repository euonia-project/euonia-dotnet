using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class PipelineMessage<TMessage, TResponse>
	where TMessage : class
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	public PipelineMessage(TMessage message)
	{
		Message = message;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	/// <param name="pipeline"></param>
	public PipelineMessage(TMessage message, IPipeline<TMessage, TResponse> pipeline)
	{
		Message = message;
		Pipeline = pipeline;
	}

	/// <summary>
	/// Gets the message.
	/// </summary>
	public TMessage Message { get; }

	/// <summary>
	/// 
	/// </summary>
	public IPipeline<TMessage, TResponse> Pipeline { get; private set; }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public PipelineMessage<TMessage, TResponse> Use(Type type, params object[] args)
	{
		Pipeline = Pipeline.Use(type, args);
		return this;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBehavior"></typeparam>
	/// <returns></returns>
	public PipelineMessage<TMessage, TResponse> Use<TBehavior>()
		where TBehavior : IPipelineBehavior<TMessage, TResponse>
	{
		Pipeline = Pipeline.Use<TBehavior>();
		return this;
	}

	/// <summary>
	/// Executes the pipeline asynchronously.
	/// </summary>
	/// <returns></returns>
	public Task<TResponse> ExecuteAsync()
	{
		return Pipeline.RunAsync(Message);
	}
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class PipelineMessage<TMessage>
	where TMessage : class
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	public PipelineMessage(TMessage message)
	{
		Message = message;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	/// <param name="pipeline"></param>
	public PipelineMessage(TMessage message, IPipeline pipeline)
	{
		Message = message;
		Pipeline = pipeline;
	}

	/// <summary>
	/// 
	/// </summary>
	public TMessage Message { get; }

	/// <summary>
	/// 
	/// </summary>
	public IPipeline Pipeline { get; private set; }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="type"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public PipelineMessage<TMessage> Use(Type type, params object[] args)
	{
		Pipeline = Pipeline.Use(type, args);
		return this;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TBehavior"></typeparam>
	/// <returns></returns>
	public PipelineMessage<TMessage> Use<TBehavior>()
		where TBehavior : IPipelineBehavior<TMessage>
	{
		Pipeline = Pipeline.Use<TBehavior>();
		return this;
	}

	/// <summary>
	/// Executes the pipeline asynchronously.
	/// </summary>
	/// <returns></returns>
	public Task ExecuteAsync()
	{
		return Pipeline.RunAsync(Message);
	}
}