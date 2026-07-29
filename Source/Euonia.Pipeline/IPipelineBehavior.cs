namespace Nerosoft.Euonia.Pipeline;

/// <summary>
/// Defines a pipeline behavior.
/// </summary>
public interface IPipelineBehavior
{
	/// <summary>
	/// Handle context
	/// </summary>
	/// <param name="context"></param>
	/// <param name="next"></param>
	/// <returns></returns>
	Task HandleAsync(object context, PipelineDelegate next);
}

/// <summary>
/// Pipeline behavior to surround the inner handler.
/// Implementations add additional behavior and await the next delegate.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
{
	/// <summary>
	/// Pipeline handler. Perform any additional behavior and await the <paramref name="next"/> delegate as necessary
	/// </summary>
	/// <param name="context">Incoming request</param>
	/// <param name="next">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
	/// <returns>Awaitable task returning the <typeparamref name="TResponse"/></returns>
	Task<TResponse> HandleAsync(TRequest context, PipelineDelegate<TRequest, TResponse> next);
}

/// <summary>
/// Pipeline behavior to surround the inner handler.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IPipelineBehavior<TRequest>
{
	/// <summary>
	/// Pipeline handler. Perform any additional behavior and await the <paramref name="next"/> delegate as necessary
	/// </summary>
	/// <param name="context">Incoming request</param>
	/// <param name="next">Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.</param>
	/// <returns>Awaitable task</returns>
	Task HandleAsync(TRequest context, PipelineDelegate<TRequest> next);
}